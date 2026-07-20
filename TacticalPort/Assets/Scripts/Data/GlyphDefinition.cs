#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : Generic deterministic glyph data shared by skills and summons.
#endregion

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TacticalPort.Data
{
    public enum GlyphTriggerTiming
    {
        TurnStart = 0,
        TurnEnd = 1,
        PersistentPresence = 2
    }

    public enum GlyphTargetUnitType
    {
        AllUnits = 0,
        CharactersOnly = 1,
        SummonsOnly = 2
    }

    public enum GlyphEffectType
    {
        None = 0,
        Damage = 1,
        Heal = 2,
        ApplyState = 3
    }

    [Serializable]
    public sealed class GlyphEffectDefinition
    {
        [SerializeField] private GlyphEffectType _Type;
        [SerializeField, Min(0)] private int _Power;
        [SerializeField] private StateDefinition _State;
        [SerializeField, Min(1)] private int _StateStacks = 1;
        [SerializeField] private int _StateDurationTurns = -1;

        public GlyphEffectType Type => _Type;
        public int Power => Mathf.Max(0, _Power);
        public StateDefinition State => _State;
        public int StateStacks => Mathf.Max(1, _StateStacks);
        public int StateDurationTurns => _StateDurationTurns;
    }

    [CreateAssetMenu(fileName = "GlyphDefinition", menuName = "Project/Data/Glyph Definition")]
    public sealed class GlyphDefinition : ScriptableObject
    {
        [TabGroup("Metadata")]
        [SerializeField] private string _Id = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("English Display Name (Fallback)")]
        [SerializeField] private string _DisplayName = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("English Description (Fallback)")]
        [SerializeField, TextArea] private string _Description = string.Empty;

        [TabGroup("Area")]
        [SerializeField] private SkillAoeShape _Shape = SkillAoeShape.Circle;

        [TabGroup("Area")]
        [SerializeField, Min(0)] private int _Size = 1;

        [TabGroup("Rules")]
        [SerializeField] private GlyphTriggerTiming _TriggerTiming = GlyphTriggerTiming.TurnStart;

        [TabGroup("Rules")]
        [SerializeField] private GlyphTargetUnitType _TargetUnitType = GlyphTargetUnitType.AllUnits;

        [TabGroup("Effects")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField] private List<GlyphEffectDefinition> _AllyEffects = new List<GlyphEffectDefinition>();

        [TabGroup("Effects")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField] private List<GlyphEffectDefinition> _EnemyEffects = new List<GlyphEffectDefinition>();

        [TabGroup("Effects")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField] private List<GlyphEffectDefinition> _NeutralEffects = new List<GlyphEffectDefinition>();

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string EnglishDisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string EnglishDescription => _Description;
        public string DisplayName => GameLocalization.GetContentName(GameLocalization.StatesTable, Id, EnglishDisplayName);
        public string Description => GameLocalization.GetContentDescription(GameLocalization.StatesTable, Id, EnglishDescription);
        public SkillAoeShape Shape => _Shape;
        public int Size => Mathf.Max(0, _Shape == SkillAoeShape.Single ? 0 : _Size);
        public GlyphTriggerTiming TriggerTiming => _TriggerTiming;
        public GlyphTargetUnitType TargetUnitType => _TargetUnitType;
        public IReadOnlyList<GlyphEffectDefinition> AllyEffects => _AllyEffects != null ? _AllyEffects : Array.Empty<GlyphEffectDefinition>();
        public IReadOnlyList<GlyphEffectDefinition> EnemyEffects => _EnemyEffects != null ? _EnemyEffects : Array.Empty<GlyphEffectDefinition>();
        public IReadOnlyList<GlyphEffectDefinition> NeutralEffects => _NeutralEffects != null ? _NeutralEffects : Array.Empty<GlyphEffectDefinition>();
    }
}
