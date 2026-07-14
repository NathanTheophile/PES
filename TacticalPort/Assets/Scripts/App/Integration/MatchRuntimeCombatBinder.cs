#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Bootstrap
#endregion

using System.Collections;
using TacticalPort.App;
using TacticalPort.Matchmaking;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Bootstrap
{
    public sealed class MatchRuntimeCombatBinder : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Tooltip("Injected by RuntimeServicesBootstrap when an online combat scene loads. May be assigned directly for isolated scene tests.")]
        [SerializeField] private MatchRuntimeContext _MatchContext;
        [SerializeField] private CombatBootstrap _CombatBootstrap;
        [Tooltip("Component implementing IMatchCombatNetworkBridge. Usually the PurrNet combat adapter in online scenes.")]
        [SerializeField] private MonoBehaviour _MatchCombatNetworkBridgeSource;
        [SerializeField] private bool _InitializeMatchCombatNetworkBridge = true;
        [SerializeField] private bool _LogContext = true;

        private IMatchCombatNetworkBridge _MatchCombatNetworkBridge;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            ConfigureVelocityTieBreaker();
        }

        private IEnumerator Start()
        {
            CacheMissingReferences();
            yield return null;
            ApplyMatchContext();
        }

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| BIND

        public void ConfigureMatchContext(MatchRuntimeContext pMatchContext)
        {
            _MatchContext = pMatchContext;
            CacheMissingReferences();
            ConfigureVelocityTieBreaker();
        }

        private void ApplyMatchContext()
        {
            ConfigureVelocityTieBreaker();

            if (_MatchContext == null || !_MatchContext.HasMatch)
            {
                _CombatBootstrap?.HudManager?.SetStatus("Local combat: no online match context.");
                return;
            }

            if (_InitializeMatchCombatNetworkBridge && ResolveMatchCombatNetworkBridge() is IMatchCombatNetworkBridge lNetworkBridge)
            {
                lNetworkBridge.ConfigureServerPlayerMode(
                    _MatchContext.ConnectionMode is MatchConnectionMode.CustomHost or MatchConnectionMode.CustomRelayHost,
                    MatchPlayerSlot.TeamA);
                lNetworkBridge.InitializeMatch(_MatchContext.Manifest, _MatchContext.LocalPlayer);
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

            if (_MatchCombatNetworkBridgeSource == null)
                _MatchCombatNetworkBridgeSource = FindMatchCombatNetworkBridgeSource();

            _MatchCombatNetworkBridge = _MatchCombatNetworkBridgeSource as IMatchCombatNetworkBridge;
        }

        private void ConfigureVelocityTieBreaker()
        {
            MatchPlayerSlot lStartingSlot = _MatchContext?.Manifest?.ResolvePerfectVelocityTieStartingSlot() ?? MatchPlayerSlot.TeamA;
            _CombatBootstrap?.ConfigurePerfectVelocityTieStartingTeam(
                lStartingSlot == MatchPlayerSlot.TeamB ? Team.TeamB : Team.TeamA);
        }

        private IMatchCombatNetworkBridge ResolveMatchCombatNetworkBridge()
        {
            _MatchCombatNetworkBridge ??= _MatchCombatNetworkBridgeSource as IMatchCombatNetworkBridge;
            if (_MatchCombatNetworkBridge != null)
                return _MatchCombatNetworkBridge;

            if (_MatchCombatNetworkBridgeSource != null)
                Debug.LogWarning($"Assigned match combat network bridge '{_MatchCombatNetworkBridgeSource.name}' does not implement {nameof(IMatchCombatNetworkBridge)}.", this);

            return null;
        }

        private MonoBehaviour FindMatchCombatNetworkBridgeSource()
        {
            MonoBehaviour[] lBehaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is IMatchCombatNetworkBridge)
                    return lBehaviours[lIndex];
            }

            return null;
        }

        #endregion
    }
}
