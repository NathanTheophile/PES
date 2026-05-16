using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Apis.Matchmaker;
using Unity.Services.CloudCode.Core;
using Unity.Services.Matchmaker.Model;
using IExecutionContext = Unity.Services.CloudCode.Core.IExecutionContext;

namespace EdgegapAllocatorModule;

public class ModuleConfig : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
#pragma warning disable CS0618
        config.Dependencies.AddSingleton(GameApiClient.Create());
#pragma warning restore CS0618
    }
}

public class EdgegapAllocator(IGameApiClient gameApiClient, ILogger<EdgegapAllocator> logger) : IMatchmakerAllocator
{
    private const string ApplicationName = "tortugarena";
    private const string VersionName = "alpha-0.1.3";
    private const string PortName = "gameport";

    private const string EdgegapApiUrl = "https://api.edgegap.com";
    private const bool ForceCachedLocation = false;
    private const string EdgegapApiTokenSecretName = "EDGEGAP_API_TOKEN";
    private const string EdgegapRequestIdAssignmentKey = "edgegapRequestId";
    private const string EdgegapPortNameAssignmentKey = "edgegapPortName";

    [CloudCodeFunction("Matchmaker_AllocateServer")]
    public async Task<AllocateResponse> Allocate(IExecutionContext context, AllocateRequest request)
    {
        try
        {
            Secret edgegapApiToken = await gameApiClient.SecretManager.GetSecret(context, EdgegapApiTokenSecretName);
            using HttpClient client = CreateEdgeGapClient(edgegapApiToken.Value);

            var users = ExtractValidPlayerIps(request.MatchmakingResults.MatchProperties)
                .Select(ip => new DeploymentUser
                {
                    UserType = "ip_address",
                    UserData = new UserData { IpAddress = ip.ToString() },
                })
                .ToList();

            if (users.Count == 0)
            {
                logger.LogWarning("No valid player IPs found in match properties. Add player_ip to player custom data for better EdgeGap location selection.");
                users.Add(new DeploymentUser
                {
                    UserType = "geo_coordinates",
                    UserData = new UserData
                    {
                        Latitude = 48.8566,
                        Longitude = 2.3522,
                    },
                });
            }

            var deploymentRequest = new DeploymentRequest
            {
                Application = ApplicationName,
                Version = VersionName,
                RequireCachedLocations = ForceCachedLocation,
                Users = users,
                Tags = ["ugs-matchmaker"],
            };

            var content = new StringContent(JsonConvert.SerializeObject(deploymentRequest), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"{EdgegapApiUrl}/v2/deployments", content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("EdgeGap deployment failed with status code {ResponseStatusCode}: {ResponseContent}", response.StatusCode, responseContent);
                return new AllocateResponse(AllocateStatus.Error) { Message = responseContent };
            }

            var edgegapDeployment = JsonConvert.DeserializeObject<DeploymentResponse>(responseContent);
            return new AllocateResponse(AllocateStatus.Created)
            {
                AllocationData = new Dictionary<string, object>
                {
                    { "requestId", edgegapDeployment?.RequestId ?? string.Empty },
                },
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "EdgeGap deployment failed");
            return new AllocateResponse(AllocateStatus.Error)
            {
                Message = exception.Message,
            };
        }
    }

    [CloudCodeFunction("Matchmaker_PollAllocation")]
    public async Task<PollResponse> Poll(IExecutionContext context, PollRequest request)
    {
        string requestId = request.AllocationData.TryGetValue("requestId", out object? requestIdValue)
            ? requestIdValue?.ToString() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrWhiteSpace(requestId))
            return new PollResponse(PollStatus.Error) { Message = "Missing EdgeGap requestId in allocation data." };

        try
        {
            Secret edgegapApiToken = await gameApiClient.SecretManager.GetSecret(context, EdgegapApiTokenSecretName);
            using HttpClient client = CreateEdgeGapClient(edgegapApiToken.Value);
            HttpResponseMessage response = await client.GetAsync($"{EdgegapApiUrl}/v1/status/{requestId}");
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new PollResponse(PollStatus.Error) { Message = responseContent };

            var deploymentStatus = JsonConvert.DeserializeObject<DeploymentStatusResponse>(responseContent);
            if (deploymentStatus == null)
                return new PollResponse(PollStatus.Error) { Message = "Deployment status response is null." };

            string normalizedStatus = NormalizeEdgeGapStatus(deploymentStatus.CurrentStatus);
            switch (normalizedStatus)
            {
                case "ready":
                    return BuildAllocatedResponse(requestId, deploymentStatus, responseContent);

                case "error":
                    return new PollResponse(PollStatus.Error)
                    {
                        Message = $"Deployment failed with the current error: {deploymentStatus.ErrorDetail}",
                    };

                case "terminating":
                case "terminated":
                    return new PollResponse(PollStatus.Error)
                    {
                        Message = "Deployment is terminated and cannot receive connections.",
                    };

                default:
                    return new PollResponse(PollStatus.Pending);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error polling EdgeGap");
            return new PollResponse(PollStatus.Error)
            {
                Message = exception.Message,
            };
        }
    }

    [CloudCodeFunction("ReleaseEdgeGapServer")]
    public async Task<ReleaseDeploymentResponse> Release(IExecutionContext context, string allocationId)
    {
        if (string.IsNullOrWhiteSpace(allocationId))
        {
            return new ReleaseDeploymentResponse
            {
                Released = false,
                Message = "Missing EdgeGap allocation id.",
            };
        }

        try
        {
            Secret edgegapApiToken = await gameApiClient.SecretManager.GetSecret(context, EdgegapApiTokenSecretName);
            using HttpClient client = CreateEdgeGapClient(edgegapApiToken.Value);
            HttpResponseMessage response = await client.DeleteAsync($"{EdgegapApiUrl}/v1/stop/{allocationId}");
            string responseContent = await response.Content.ReadAsStringAsync();

            bool success = response.StatusCode is HttpStatusCode.OK
                or HttpStatusCode.Accepted
                or HttpStatusCode.NoContent
                or HttpStatusCode.NotFound
                or HttpStatusCode.Gone;

            if (!success)
                logger.LogWarning("EdgeGap deployment release failed for {RequestId} with status code {StatusCode}: {ResponseContent}", allocationId, response.StatusCode, responseContent);

            return new ReleaseDeploymentResponse
            {
                Released = success,
                RequestId = allocationId,
                StatusCode = (int)response.StatusCode,
                Message = responseContent,
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "EdgeGap deployment release failed");
            return new ReleaseDeploymentResponse
            {
                Released = false,
                RequestId = allocationId,
                Message = exception.Message,
            };
        }
    }

    private static PollResponse BuildAllocatedResponse(string requestId, DeploymentStatusResponse deploymentStatus, string responseContent)
    {
        if (deploymentStatus.Ports == null || deploymentStatus.Ports.Count == 0)
        {
            return new PollResponse(PollStatus.Error)
            {
                Message = $"Deployment has no exposed ports. Response: {responseContent}",
            };
        }

        if (!deploymentStatus.Ports.TryGetValue(PortName, out var port))
        {
            return new PollResponse(PollStatus.Error)
            {
                Message = $"Requested port {PortName} is not exposed by the deployment. Verify the EdgeGap dashboard port name.",
            };
        }

        if (deploymentStatus.PublicIp == null)
        {
            return new PollResponse(PollStatus.Error)
            {
                Message = $"Deployment is ready but public_ip is missing. Response: {responseContent}",
            };
        }

        return new PollResponse(PollStatus.Allocated)
        {
            AssignmentData = AssignmentData.IpPort(
                deploymentStatus.PublicIp,
                port.External,
                new Dictionary<string, object>
                {
                    { EdgegapRequestIdAssignmentKey, requestId },
                    { EdgegapPortNameAssignmentKey, PortName },
                }),
        };
    }

    private static string NormalizeEdgeGapStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return string.Empty;

        string normalized = status.Trim().ToLowerInvariant();
        int separatorIndex = normalized.LastIndexOf('.');
        return separatorIndex >= 0 && separatorIndex < normalized.Length - 1
            ? normalized[(separatorIndex + 1)..]
            : normalized;
    }

    private static HttpClient CreateEdgeGapClient(string apiToken)
    {
        var client = new HttpClient();
        string cleanedToken = (apiToken ?? string.Empty).Replace("token ", string.Empty);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", cleanedToken);
        return client;
    }

    private IReadOnlyList<IPAddress> ExtractValidPlayerIps(Dictionary<string, object> matchProperties)
    {
        var result = new List<IPAddress>();

        if (!matchProperties.TryGetValue("Players", out var playersObj))
        {
            logger.LogDebug("No Players key found in MatchProperties");
            return result;
        }

        if (playersObj is not JArray playersArray)
        {
            logger.LogWarning("Players is not a JArray. Actual type: {Type}", playersObj.GetType());
            return result;
        }

        List<Player>? players;
        try
        {
            players = playersArray.ToObject<List<Player>>();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to deserialize Players array");
            return result;
        }

        if (players == null)
            return result;

        foreach (var player in players)
        {
            if (player.CustomData is not JObject customData || !customData.TryGetValue("player_ip", out var ipToken))
                continue;

            string? ipString = ipToken.Type == JTokenType.String ? ipToken.Value<string>() : null;
            if (string.IsNullOrWhiteSpace(ipString))
                continue;

            if (IPAddress.TryParse(ipString, out var ip))
                result.Add(ip);
            else
                logger.LogDebug("Invalid IP format ignored: {Ip}", ipString);
        }

        return result;
    }

    private sealed class DeploymentRequest
    {
        [JsonProperty("application")]
        public string Application { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("require_cached_locations")]
        public bool RequireCachedLocations { get; set; }

        [JsonProperty("users")]
        public List<DeploymentUser> Users { get; set; } = [];

        [JsonProperty("tags")]
        public string[] Tags { get; set; } = [];
    }

    private sealed class DeploymentUser
    {
        [JsonProperty("user_type")]
        public string UserType { get; set; } = string.Empty;

        [JsonProperty("user_data")]
        public UserData UserData { get; set; } = new();
    }

    private sealed class UserData
    {
        [JsonProperty("ip_address", NullValueHandling = NullValueHandling.Ignore)]
        public string? IpAddress { get; set; }

        [JsonProperty("latitude", NullValueHandling = NullValueHandling.Ignore)]
        public double? Latitude { get; set; }

        [JsonProperty("longitude", NullValueHandling = NullValueHandling.Ignore)]
        public double? Longitude { get; set; }
    }

    private sealed class DeploymentResponse
    {
        [JsonProperty("request_id")]
        public string RequestId { get; set; } = string.Empty;
    }

    public sealed class ReleaseDeploymentResponse
    {
        public bool Released { get; set; }
        public string RequestId { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    private sealed class DeploymentStatusResponse
    {
        [JsonProperty("current_status")]
        public string CurrentStatus { get; set; } = string.Empty;

        [JsonProperty("error_detail")]
        public string ErrorDetail { get; set; } = string.Empty;

        [JsonProperty("public_ip")]
        public string? PublicIp { get; set; }

        [JsonProperty("ports")]
        public Dictionary<string, DeploymentPort> Ports { get; set; } = new();
    }

    private sealed class DeploymentPort
    {
        [JsonProperty("external")]
        public int External { get; set; }
    }
}
