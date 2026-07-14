#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Persisted identifiers only. ScriptableObject references are resolved at runtime.
//  State
#endregion

using System;
using System.Collections.Generic;

namespace TacticalPort.State
{
    [Serializable]
    public sealed class TeamPresetRepositorySnapshot
    {
        public int SchemaVersion = TeamPreset.CurrentSchemaVersion;
        public long UpdatedAtUnixMilliseconds;
        public string ActivePresetId = string.Empty;
        public List<TeamPreset> Presets = new List<TeamPreset>();

        public TeamPresetRepositorySnapshot Clone()
        {
            TeamPresetRepositorySnapshot lClone = new TeamPresetRepositorySnapshot
            {
                SchemaVersion = SchemaVersion,
                UpdatedAtUnixMilliseconds = UpdatedAtUnixMilliseconds,
                ActivePresetId = ActivePresetId
            };

            if (Presets == null)
                return lClone;

            for (int lIndex = 0; lIndex < Presets.Count; lIndex++)
            {
                if (Presets[lIndex] != null)
                    lClone.Presets.Add(Presets[lIndex].Clone());
            }

            return lClone;
        }
    }

    [Serializable]
    public sealed class TeamPreset
    {
        public const int CurrentSchemaVersion = 2;
        public const int TeamSize = 3;
        public const int EquippedSkillCount = 6;

        public int SchemaVersion = CurrentSchemaVersion;
        public string PresetId = string.Empty;
        public string DisplayName = string.Empty;
        public List<UnitBuildPreset> Slots = new List<UnitBuildPreset>(TeamSize);

        public TeamPreset Clone()
        {
            TeamPreset lClone = new TeamPreset
            {
                SchemaVersion = SchemaVersion,
                PresetId = PresetId,
                DisplayName = DisplayName
            };

            if (Slots == null)
                return lClone;

            for (int lIndex = 0; lIndex < Slots.Count; lIndex++)
                lClone.Slots.Add(Slots[lIndex]?.Clone() ?? new UnitBuildPreset());

            return lClone;
        }
    }

    [Serializable]
    public sealed class UnitBuildPreset
    {
        public string UnitId = string.Empty;
        public string PassiveId = string.Empty;
        public List<string> SkillIds = new List<string>(TeamPreset.EquippedSkillCount);
        public UnitStatAllocationPreset StatAllocations = new UnitStatAllocationPreset();

        public UnitBuildPreset Clone() => new UnitBuildPreset
        {
            UnitId = UnitId,
            PassiveId = PassiveId,
            SkillIds = SkillIds != null ? new List<string>(SkillIds) : new List<string>(),
            StatAllocations = StatAllocations?.Clone() ?? new UnitStatAllocationPreset()
        };
    }

    [Serializable]
    public sealed class UnitStatAllocationPreset
    {
        public int Health;
        public int Energy;
        public int Mobility;
        public int MeleeDamage;
        public int MeleeResistance;
        public int RangedDamage;
        public int RangedResistance;
        public int Velocity;

        public UnitStatAllocationPreset Clone() => (UnitStatAllocationPreset)MemberwiseClone();
    }
}
