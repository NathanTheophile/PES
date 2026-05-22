#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class UgsCloudCodeGameServerAllocator : MonoBehaviour, IGameServerAllocator
    {
        #region _____________________________/ VALUES

        [Tooltip("Cloud Code endpoint. It must be idempotent by matchId: create the EdgeGap deployment once, then return the same endpoint to both players.")]
        [SerializeField] private string _AllocateEndpointName = "AllocateEdgeGapServer";
        [Tooltip("Optional Cloud Code endpoint used when leaving a dedicated match session.")]
        [SerializeField] private string _ReleaseEndpointName = "ReleaseEdgeGapServer";
        [SerializeField] private bool _ReleaseOnCleanup = true;
        [SerializeField] private bool _LogEvents = true;

        #endregion

        #region _____________________________| ALLOCATE

        public async Task<MatchServerEndpoint> AllocateServerAsync(MatchAllocationRequest pRequest, CancellationToken pCancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_AllocateEndpointName))
                throw new InvalidOperationException("Cloud Code allocator requires an allocate endpoint name.");

            pCancellationToken.ThrowIfCancellationRequested();

            CloudCodeServerEndpointResponse lResponse = await CloudCodeService.Instance.CallEndpointAsync<CloudCodeServerEndpointResponse>(
                _AllocateEndpointName,
                BuildAllocateArgs(pRequest));

            pCancellationToken.ThrowIfCancellationRequested();

            MatchServerEndpoint lEndpoint = ToEndpoint(lResponse);
            if (lEndpoint == null || !lEndpoint.IsValid)
                throw new InvalidOperationException("Cloud Code allocation response did not contain a valid server endpoint.");

            if (_LogEvents)
                Debug.Log($"[UGS Cloud Code Allocator] Server endpoint ready. MatchId={pRequest?.MatchId}, Endpoint={lEndpoint.IpAddress}:{lEndpoint.Port}, AllocationId={lEndpoint.AllocationId}", this);

            return lEndpoint;
        }

        public async Task ReleaseServerAsync(MatchServerEndpoint pEndpoint, CancellationToken pCancellationToken)
        {
            if (!_ReleaseOnCleanup || pEndpoint == null || string.IsNullOrWhiteSpace(pEndpoint.AllocationId) || string.IsNullOrWhiteSpace(_ReleaseEndpointName))
                return;

            pCancellationToken.ThrowIfCancellationRequested();
            await CloudCodeService.Instance.CallEndpointAsync(_ReleaseEndpointName, BuildReleaseArgs(pEndpoint));
            pCancellationToken.ThrowIfCancellationRequested();

            if (_LogEvents)
                Debug.Log($"[UGS Cloud Code Allocator] Server allocation released. AllocationId={pEndpoint.AllocationId}", this);
        }

        private static Dictionary<string, object> BuildAllocateArgs(MatchAllocationRequest pRequest) =>
            new Dictionary<string, object>
            {
                { "ticketId", pRequest?.TicketId ?? string.Empty },
                { "matchId", pRequest?.MatchId ?? string.Empty },
                { "mapId", pRequest?.MapId ?? string.Empty },
                { "queueName", pRequest?.QueueName ?? string.Empty },
                { "localPlayerId", pRequest?.LocalPlayerId ?? string.Empty },
                { "manifestJson", pRequest?.Manifest != null ? JsonUtility.ToJson(pRequest.Manifest) : string.Empty }
            };

        private static Dictionary<string, object> BuildReleaseArgs(MatchServerEndpoint pEndpoint) =>
            new Dictionary<string, object>
            {
                { "allocationId", pEndpoint.AllocationId ?? string.Empty },
                { "ipAddress", pEndpoint.IpAddress ?? string.Empty },
                { "port", pEndpoint.Port }
            };

        private static MatchServerEndpoint ToEndpoint(CloudCodeServerEndpointResponse pResponse)
        {
            if (pResponse == null)
                return null;

            int lPort = pResponse.Port > 0 ? pResponse.Port : pResponse.port;
            return new MatchServerEndpoint
            {
                IpAddress = FirstNonEmpty(pResponse.IpAddress, pResponse.ipAddress, pResponse.Ip, pResponse.ip, pResponse.Host, pResponse.host),
                Port = (ushort)Mathf.Clamp(lPort, 0, ushort.MaxValue),
                AllocationId = FirstNonEmpty(pResponse.AllocationId, pResponse.allocationId, pResponse.DeploymentId, pResponse.deploymentId, pResponse.RequestId, pResponse.requestId)
            };
        }

        private static string FirstNonEmpty(params string[] pValues)
        {
            for (int lIndex = 0; lIndex < pValues.Length; lIndex++)
            {
                if (!string.IsNullOrWhiteSpace(pValues[lIndex]))
                    return pValues[lIndex];
            }

            return string.Empty;
        }

        #endregion

        [Serializable]
#pragma warning disable 0649
        private sealed class CloudCodeServerEndpointResponse
        {
            public string IpAddress;
            public string ipAddress;
            public string Ip;
            public string ip;
            public string Host;
            public string host;
            public int Port;
            public int port;
            public string AllocationId;
            public string allocationId;
            public string DeploymentId;
            public string deploymentId;
            public string RequestId;
            public string requestId;
        }
#pragma warning restore 0649
    }
}
