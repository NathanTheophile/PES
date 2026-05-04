#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Matchmaking
#endregion

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class LocalGameServerAllocator : MonoBehaviour, IGameServerAllocator
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _IpAddress = "127.0.0.1";
        [SerializeField, Min(1)] private int _Port = 7777;

        #endregion

        #region _____________________________| ALLOCATE

        public Task<MatchServerEndpoint> AllocateServerAsync(MatchAllocationRequest pRequest, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new MatchServerEndpoint
            {
                IpAddress = _IpAddress,
                Port = (ushort)Mathf.Clamp(_Port, 1, ushort.MaxValue),
                AllocationId = string.IsNullOrWhiteSpace(pRequest?.MatchId) ? "local" : pRequest.MatchId
            });
        }

        #endregion
    }
}
