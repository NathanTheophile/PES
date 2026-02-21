using System;
using UnityEngine;

namespace PES.Presentation.Configuration
{
    [CreateAssetMenu(
        fileName = "VerticalSliceTestProfile",
        menuName = "PES/Combat/Vertical Slice Test Profile",
        order = 22)]
    public sealed class VerticalSliceTestProfileAsset : ScriptableObject
    {
        [SerializeField] private string _profileName = "Default";
        [SerializeField] private CombatRuntimeConfigAsset _runtimeConfig;
        [SerializeField] private EntityArchetypeAsset _unitAArchetype;
        [SerializeField] private EntityArchetypeAsset _unitBArchetype;

        public string ProfileName => string.IsNullOrWhiteSpace(_profileName) ? name : _profileName;

        public CombatRuntimeConfigAsset RuntimeConfig => _runtimeConfig;

        public EntityArchetypeAsset UnitAArchetype => _unitAArchetype;

        public EntityArchetypeAsset UnitBArchetype => _unitBArchetype;
    }
}
