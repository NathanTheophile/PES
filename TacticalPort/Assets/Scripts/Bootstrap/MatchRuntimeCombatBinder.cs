#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Bootstrap
#endregion

using System.Collections;
using TacticalPort.Matchmaking;
using TacticalPort.Networking;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Bootstrap
{
    public sealed class MatchRuntimeCombatBinder : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private MatchRuntimeContext _MatchContext;
        [SerializeField] private CombatBootstrap _CombatBootstrap;
        [SerializeField] private PurrNetCombatBridge _PurrNetCombatBridge;
        [SerializeField] private bool _InitializePurrNetBridge = true;
        [SerializeField] private bool _LogContext = true;

        #endregion

        #region _____________________________| UNITY

        private IEnumerator Start()
        {
            CacheMissingReferences();
            yield return null;
            ApplyMatchContext();
        }

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| BIND

        private void ApplyMatchContext()
        {
            if (_MatchContext == null || !_MatchContext.HasMatch)
            {
                _CombatBootstrap?.HudManager?.SetStatus("Local combat: no online match context.");
                return;
            }

            if (_InitializePurrNetBridge && _PurrNetCombatBridge != null)
            {
                _PurrNetCombatBridge.ConfigureServerPlayerMode(
                    _MatchContext.ConnectionMode is MatchConnectionMode.CustomHost or MatchConnectionMode.CustomRelayHost,
                    MatchPlayerSlot.TeamA);
                _PurrNetCombatBridge.InitializeMatch(_MatchContext.Manifest, _MatchContext.LocalPlayer);
            }

            _CombatBootstrap?.HudManager?.SetStatus($"Online match context loaded. MatchId: {_MatchContext.MatchId}. Network connection pending.");

            if (_LogContext)
                Debug.Log($"[Match Runtime] Combat scene bound. PlayerId={_MatchContext.LocalPlayer.PlayerId}, MatchId={_MatchContext.MatchId}", this);
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_CombatBootstrap == null)
                _CombatBootstrap = GetComponent<CombatBootstrap>() ?? GetComponentInParent<CombatBootstrap>();

            if (_PurrNetCombatBridge == null)
                _PurrNetCombatBridge = GetComponent<PurrNetCombatBridge>() ?? GetComponentInParent<PurrNetCombatBridge>();

            if (_MatchContext == null)
                _MatchContext = FindAnyObjectByType<MatchRuntimeContext>();
        }

        #endregion
    }
}
