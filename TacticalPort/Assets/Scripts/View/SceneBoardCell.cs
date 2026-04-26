using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class SceneBoardCell : MonoBehaviour
    {
        #region _____________________________| VALUES

        [SerializeField] private SerializableGridCoord _GridCoord = new SerializableGridCoord(0, 0);
        [SerializeField] private bool _IsWalkable = true;
        [SerializeField] private bool _BlocksLineOfSight;
        [SerializeField, Min(1)] private int _MovementCost = 1;
        [SerializeField] private BattleUnitDefinition _OccupantDefinition;

        #endregion

        #region _____________________________| ACCESSORS

        public GridCoord GridCoord => _GridCoord.ToRuntime();
        public bool IsWalkable => _IsWalkable;
        public bool BlocksLineOfSight => _BlocksLineOfSight || !_IsWalkable;
        public int MovementCost => Mathf.Max(1, _MovementCost);
        public BattleUnitDefinition OccupantDefinition => _OccupantDefinition;

        #endregion

        private void OnValidate()
        {
            _MovementCost = Mathf.Max(1, _MovementCost);
            gameObject.name = $"Cell_{_GridCoord.X}_{_GridCoord.Y}";

            BoardView lBoardView = GetComponentInParent<BoardView>();
            if (lBoardView != null)
                lBoardView.RebuildBoard();
        }
    }
}
