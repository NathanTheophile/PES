#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Matchmaking
#endregion

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TacticalPort.Matchmaking
{
    public sealed class EdgeGapGameServerAllocator : MonoBehaviour, IGameServerAllocator
    {
        #region _____________________________/ VALUES

        [Tooltip("Trusted backend or Cloud Code endpoint that performs the EdgeGap allocation. Do not call EdgeGap directly from clients with a private token.")]
        [SerializeField] private string _AllocationEndpointUrl = string.Empty;
        [SerializeField, Min(1f)] private float _TimeoutSeconds = 15f;

        #endregion

        #region _____________________________| ALLOCATE

        public async Task<MatchServerEndpoint> AllocateServerAsync(MatchAllocationRequest pRequest, CancellationToken pCancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_AllocationEndpointUrl))
                throw new InvalidOperationException("EdgeGap allocator requires an allocation endpoint URL.");

            string lPayload = JsonUtility.ToJson(pRequest ?? new MatchAllocationRequest());
            using UnityWebRequest lRequest = new UnityWebRequest(_AllocationEndpointUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(lPayload)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = Mathf.CeilToInt(_TimeoutSeconds)
            };
            lRequest.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation lOperation = lRequest.SendWebRequest();
            while (!lOperation.isDone)
            {
                pCancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (lRequest.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"EdgeGap allocation failed: {lRequest.error}");

            MatchServerEndpoint lEndpoint = JsonUtility.FromJson<MatchServerEndpoint>(lRequest.downloadHandler.text);
            if (lEndpoint == null || !lEndpoint.IsValid)
                throw new InvalidOperationException("EdgeGap allocation response did not contain a valid server endpoint.");

            return lEndpoint;
        }

        #endregion
    }
}
