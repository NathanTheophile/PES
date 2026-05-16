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
        [Tooltip("Optional trusted backend or Cloud Code endpoint that releases an EdgeGap allocation.")]
        [SerializeField] private string _ReleaseEndpointUrl = string.Empty;
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

        public async Task ReleaseServerAsync(MatchServerEndpoint pEndpoint, CancellationToken pCancellationToken)
        {
            if (pEndpoint == null || string.IsNullOrWhiteSpace(pEndpoint.AllocationId) || string.IsNullOrWhiteSpace(_ReleaseEndpointUrl))
                return;

            string lPayload = JsonUtility.ToJson(pEndpoint);
            using UnityWebRequest lRequest = new UnityWebRequest(_ReleaseEndpointUrl, UnityWebRequest.kHttpVerbPOST)
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
                throw new InvalidOperationException($"EdgeGap release failed: {lRequest.error}");
        }

        #endregion
    }
}
