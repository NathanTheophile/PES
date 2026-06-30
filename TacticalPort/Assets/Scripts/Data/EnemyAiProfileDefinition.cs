#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using TacticalPort.Shared;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "EnemyAiProfile", menuName = "Project/Data/Enemy AI Profile")]
    public sealed class EnemyAiProfileDefinition : ScriptableObject
    {
        #region _____________________________/ VALUES

        [TabGroup("Behaviour")]
        [LabelText("Target Priority")]
        [SerializeField] private EnemyAiTargetPriority _TargetPriority = EnemyAiTargetPriority.WeakFirst;

        [TabGroup("Behaviour")]
        [LabelText("Movement Policy")]
        [SerializeField] private EnemyAiMovementPolicy _MovementPolicy = EnemyAiMovementPolicy.Auto;

        [TabGroup("Behaviour")]
        [LabelText("Preferred Distance")]
        [SerializeField, Min(0)] private int _PreferredDistance = 0;

        [TabGroup("Behaviour")]
        [LabelText("Threat Radius")]
        [SerializeField, Min(0)] private int _ThreatRadius = 0;

        [TabGroup("Behaviour")]
        [LabelText("Kite After Action")]
        [SerializeField] private bool _KiteAfterSuccessfulAction;

        [TabGroup("Scoring")]
        [ValidateInput(nameof(HasAnyScoringWeight), "At least one scoring weight should be above 0.", InfoMessageType.Warning)]
        [LabelText("Damage Weight")]
        [SerializeField, Min(0)] private int _DamageWeight = 100;

        [TabGroup("Scoring")]
        [ValidateInput(nameof(HasAnyScoringWeight), "At least one scoring weight should be above 0.", InfoMessageType.Warning)]
        [LabelText("Heal Weight")]
        [SerializeField, Min(0)] private int _HealWeight = 100;

        [TabGroup("Scoring")]
        [ValidateInput(nameof(HasAnyScoringWeight), "At least one scoring weight should be above 0.", InfoMessageType.Warning)]
        [LabelText("Kill Confirm Weight")]
        [SerializeField, Min(0)] private int _KillConfirmWeight = 100;

        [TabGroup("Scoring")]
        [ValidateInput(nameof(HasAnyScoringWeight), "At least one scoring weight should be above 0.", InfoMessageType.Warning)]
        [LabelText("AoE Weight")]
        [SerializeField, Min(0)] private int _AoeWeight = 100;

        [TabGroup("Scoring")]
        [ValidateInput(nameof(HasAnyScoringWeight), "At least one scoring weight should be above 0.", InfoMessageType.Warning)]
        [LabelText("Safety Weight")]
        [SerializeField, Min(0)] private int _SafetyWeight = 100;

        [TabGroup("Debug")]
        [LabelText("Debug Decisions")]
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

        #region _____________________________| ODIN

        private bool HasAnyScoringWeight() =>
            _DamageWeight > 0
            || _HealWeight > 0
            || _KillConfirmWeight > 0
            || _AoeWeight > 0
            || _SafetyWeight > 0;

        #endregion
    }
}
