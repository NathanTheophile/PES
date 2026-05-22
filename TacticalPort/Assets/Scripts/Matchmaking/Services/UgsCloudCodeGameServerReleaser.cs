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
    public sealed class UgsCloudCodeGameServerReleaser : MonoBehaviour, IGameServerAllocationReleaser
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _ModuleName = "EdgegapAllocator";
        [SerializeField] private string _ReleaseEndpointName = "ReleaseEdgeGapServer";
        [SerializeField] private bool _ReleaseOnCleanup = true;
        [SerializeField] private bool _LogEvents = true;

        #endregion

        #region _____________________________| RELEASE

        public async Task ReleaseServerAsync(MatchServerEndpoint pEndpoint, CancellationToken pCancellationToken)
        {
            if (!_ReleaseOnCleanup || pEndpoint == null || string.IsNullOrWhiteSpace(pEndpoint.AllocationId))
                return;

            if (string.IsNullOrWhiteSpace(_ModuleName) || string.IsNullOrWhiteSpace(_ReleaseEndpointName))
                throw new InvalidOperationException("Cloud Code server releaser requires a module name and release endpoint name.");

            pCancellationToken.ThrowIfCancellationRequested();
            await CloudCodeService.Instance.CallModuleEndpointAsync(_ModuleName, _ReleaseEndpointName, BuildReleaseArgs(pEndpoint));
            pCancellationToken.ThrowIfCancellationRequested();

            if (_LogEvents)
                Debug.Log($"[UGS Cloud Code Releaser] Server allocation released. AllocationId={pEndpoint.AllocationId}", this);
        }

        private static Dictionary<string, object> BuildReleaseArgs(MatchServerEndpoint pEndpoint) =>
            new Dictionary<string, object>
            {
                { "allocationId", pEndpoint.AllocationId ?? string.Empty },
                { "ipAddress", pEndpoint.IpAddress ?? string.Empty },
                { "port", pEndpoint.Port }
            };

        #endregion
    }
}
