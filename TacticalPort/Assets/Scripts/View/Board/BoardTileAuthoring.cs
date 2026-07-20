using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TacticalPort.View
{
    public sealed class BoardTileAuthoring : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private bool _IsWalkable = true;
        [FormerlySerializedAs("_BlocksLineOfSight")]
        [SerializeField] private bool _BlocksVisibility;
        [SerializeField, Min(1)] private int _MovementCost = 1;
        [SerializeField] private bool _IsSpawner;
        [SerializeField] private MatchPlayerSlot _AssignedTeam;
        [SerializeField] private UnitDefinition _OccupantDefinition;
        [SerializeField] private int _HeightLevel;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool IsWalkable => _IsWalkable;
        public bool BlocksVisibility => _BlocksVisibility;
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
#if UNITY_EDITOR
                NotifyBoardAuthoringChanged();
#endif
                return;
            }

            if (_IsSpawner && _AssignedTeam == MatchPlayerSlot.None)
                _AssignedTeam = MatchPlayerSlot.TeamA;

#if UNITY_EDITOR
            NotifyBoardAuthoringChanged();
#endif
        }

        #endregion

#if UNITY_EDITOR
        private void NotifyBoardAuthoringChanged()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            BoardAuthoring3D lBoardAuthoring = GetComponentInParent<BoardAuthoring3D>();
            if (lBoardAuthoring != null)
                lBoardAuthoring.InvalidateRuntimeScenario();
        }
#endif
    }
}

