#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Data
#endregion

using TacticalPort.Shared;
using UnityEngine;
using TacticalPort.Core;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "EnemyAiProfile", menuName = "Project/Data/Enemy AI Profile")]
    public sealed class EnemyAiProfileDefinition : ScriptableObject
    {
        #region _____________________________/ VALUES

        [SerializeField] private EnemyAiTargetPriority _TargetPriority = EnemyAiTargetPriority.WeakFirst;
        [SerializeField] private EnemyAiMovementPolicy _MovementPolicy = EnemyAiMovementPolicy.Auto;
        [SerializeField, Min(0)] private int _PreferredDistance = 0;
        [SerializeField, Min(0)] private int _ThreatRadius = 0;
        [SerializeField] private bool _KiteAfterSuccessfulAction;
        [SerializeField, Min(0)] private int _DamageWeight = 100;
        [SerializeField, Min(0)] private int _HealWeight = 100;
        [SerializeField, Min(0)] private int _KillConfirmWeight = 100;
        [SerializeField, Min(0)] private int _AoeWeight = 100;
        [SerializeField, Min(0)] private int _SafetyWeight = 100;
        [SerializeField] private bool _DebugDecisions;

        #endregion

        #region _____________________________/ ACCESSORS

        public EnemyAiTargetPriority TargetPriority => _TargetPriority;
        public EnemyAiMovementPolicy MovementPolicy => _MovementPolicy;
        public int PreferredDistance => Mathf.Max(0, _PreferredDistance);
        public int ThreatRadius => Mathf.Max(0, _ThreatRadius);
        public bool KiteAfterSuccessfulAction => _KiteAfterSuccessfulAction;
        public int DamageWeight => Mathf.Max(0, _DamageWeight);
        public int HealWeight => Mathf.Max(0, _HealWeight);
        public int KillConfirmWeight => Mathf.Max(0, _KillConfirmWeight);
        public int AoeWeight => Mathf.Max(0, _AoeWeight);
        public int SafetyWeight => Mathf.Max(0, _SafetyWeight);
        public bool DebugDecisions => _DebugDecisions;

        #endregion
    }
}
