using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace TacticalPort.View
{
    public sealed class BoardTileAuthoring : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private bool _IsWalkable = true;
        [SerializeField] private bool _BlocksLineOfSight;
        [SerializeField, Min(1)] private int _MovementCost = 1;
        [SerializeField] private bool _IsSpawner;
        [FormerlySerializedAs("_SpawnZone")]
        [SerializeField] private MatchPlayerSlot _AssignedTeam;
        [SerializeField] private UnitDefinition _OccupantDefinition;
        [SerializeField] private int _HeightLevel;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool IsWalkable => _IsWalkable;
        public bool BlocksLineOfSight => _BlocksLineOfSight;
        public int MovementCost => Mathf.Max(1, _MovementCost);
        public bool IsSpawner => _IsSpawner;
        public MatchPlayerSlot AssignedTeam => _IsSpawner ? _AssignedTeam : MatchPlayerSlot.None;
        public UnitDefinition OccupantDefinition => _OccupantDefinition;
        public int HeightLevel => _HeightLevel;

        #endregion

        #region _____________________________| UNITY

        private void OnValidate()
        {
            _MovementCost = Mathf.Max(1, _MovementCost);

            if (!_IsSpawner)
            {
                _AssignedTeam = MatchPlayerSlot.None;
                return;
            }

            if (_IsSpawner && _AssignedTeam == MatchPlayerSlot.None)
                _AssignedTeam = MatchPlayerSlot.TeamA;
        }

        #endregion
    }
}

