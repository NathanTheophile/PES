using System;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class BoardTileAuthoring : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private bool _IsWalkable = true;
        [SerializeField] private bool _BlocksLineOfSight;
        [SerializeField, Min(1)] private int _MovementCost = 1;
        [SerializeField, HideInInspector] private bool _IsSpawner;
        [SerializeField] private MatchPlayerSlot _SpawnZone;
        [SerializeField] private UnitDefinition _OccupantDefinition;
        [SerializeField] private int _HeightLevel;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool IsWalkable => _IsWalkable;
        public bool BlocksLineOfSight => _BlocksLineOfSight;
        public int MovementCost => Mathf.Max(1, _MovementCost);
        [Obsolete("Use SpawnZone instead.")]
        public bool IsSpawner => SpawnZone != MatchPlayerSlot.None;
        public MatchPlayerSlot SpawnZone => _SpawnZone != MatchPlayerSlot.None || !_IsSpawner ? _SpawnZone : MatchPlayerSlot.TeamA;
        public UnitDefinition OccupantDefinition => _OccupantDefinition;
        public int HeightLevel => _HeightLevel;

        #endregion

        #region _____________________________| UNITY

        private void OnValidate()
        {
            _MovementCost = Mathf.Max(1, _MovementCost);
            if (_IsSpawner && _SpawnZone == MatchPlayerSlot.None)
                _SpawnZone = MatchPlayerSlot.TeamA;

            _IsSpawner = _SpawnZone != MatchPlayerSlot.None;
        }

        #endregion
    }
}

