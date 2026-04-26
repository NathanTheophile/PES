using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.View;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.Editor
{
    public sealed class TilemapBoardCellEditorWindow : EditorWindow
    {
        [SerializeField] private TilemapBoardAuthoring _Board;
        [SerializeField] private bool _PickInScene;
        [SerializeField] private bool _HasSelectedCell;
        [SerializeField] private GridCoord _SelectedCell;

        [MenuItem("TacticalPort/Board/Tilemap Cell Editor")]
        private static void Open() => GetWindow<TilemapBoardCellEditorWindow>("Tilemap Cell");

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            TryAutoAssignBoard();
        }
        private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

        private void OnGUI()
        {
            TryAutoAssignBoard();
            _Board = (TilemapBoardAuthoring)EditorGUILayout.ObjectField("Board", _Board, typeof(TilemapBoardAuthoring), true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Selected") && Selection.activeGameObject != null)
                    _Board = Selection.activeGameObject.GetComponentInParent<TilemapBoardAuthoring>();

                _PickInScene = GUILayout.Toggle(_PickInScene, "Pick In Scene", "Button");
            }

            if (_Board == null)
            {
                EditorGUILayout.HelpBox("Assign a TilemapBoardAuthoring to edit cell metadata.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Trim Missing Metadata"))
            {
                Undo.RecordObject(_Board, "Trim Tilemap Metadata");
                _Board.RemoveMetadataForMissingTiles();
                EditorUtility.SetDirty(_Board);
            }

            if (!_HasSelectedCell)
            {
                EditorGUILayout.HelpBox("Pick a painted tile in the Scene view.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Cell", $"{_SelectedCell.X}, {_SelectedCell.Y}");

            if (!_Board.ContainsCell(_SelectedCell))
            {
                EditorGUILayout.HelpBox("This coordinate is not painted on the tilemap.", MessageType.Warning);
                return;
            }

            bool lHasMetadata = _Board.TryGetCellMetadata(_SelectedCell, out TilemapBoardCellMetadata lMetadata);
            bool lWalkable = lHasMetadata ? lMetadata.IsWalkable : true;
            bool lBlocksLineOfSight = lHasMetadata && lMetadata.BlocksLineOfSight;
            int lMovementCost = lHasMetadata ? lMetadata.MovementCost : 1;
            BattleUnitDefinition lOccupant = lHasMetadata ? lMetadata.OccupantDefinition : null;

            EditorGUI.BeginChangeCheck();
            lWalkable = EditorGUILayout.Toggle("Walkable", lWalkable);
            lBlocksLineOfSight = EditorGUILayout.Toggle("Blocks Line Of Sight", lBlocksLineOfSight);
            lMovementCost = EditorGUILayout.IntField("Movement Cost", lMovementCost);
            lOccupant = (BattleUnitDefinition)EditorGUILayout.ObjectField("Occupant Definition", lOccupant, typeof(BattleUnitDefinition), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (!_Board.TryGetAuthoredCell(_SelectedCell, out Vector3Int lAuthoredCell))
                    return;

                Undo.RecordObject(_Board, "Edit Tilemap Cell Metadata");
                lMetadata = new TilemapBoardCellMetadata
                {
                    Coordinate = new SerializableGridCoord(lAuthoredCell.x, lAuthoredCell.y),
                    IsWalkable = lWalkable,
                    BlocksLineOfSight = lBlocksLineOfSight,
                    MovementCost = Mathf.Max(1, lMovementCost),
                    OccupantDefinition = lOccupant
                };
                _Board.SetCellMetadata(lMetadata);
                EditorUtility.SetDirty(_Board);
            }

            using (new EditorGUI.DisabledScope(!lHasMetadata))
            {
                if (GUILayout.Button("Reset Cell Metadata") && _Board.TryGetAuthoredCell(_SelectedCell, out Vector3Int lCellPosition))
                {
                    Undo.RecordObject(_Board, "Reset Tilemap Cell Metadata");
                    _Board.RemoveMetadata(lCellPosition);
                    EditorUtility.SetDirty(_Board);
                    Repaint();
                }
            }
        }

        private void OnSceneGUI(SceneView pSceneView)
        {
            if (!_PickInScene || _Board == null)
                return;

            Event lEvent = Event.current;
            Plane lPlane = new Plane(Vector3.forward, _Board.transform.position);
            Ray lRay = HandleUtility.GUIPointToWorldRay(lEvent.mousePosition);
            if (!lPlane.Raycast(lRay, out float lDistance))
                return;

            Vector3 lWorldPoint = lRay.GetPoint(lDistance);
            if (!_Board.TryGetGridCoord(lWorldPoint, out GridCoord lCoord))
                return;

            Handles.color = Color.yellow;
            Handles.DrawWireCube(_Board.GetWorldPosition(lCoord), Vector3.one * 0.35f);

            if (lEvent.type == EventType.MouseDown && lEvent.button == 0 && !lEvent.alt)
            {
                _HasSelectedCell = true;
                _SelectedCell = lCoord;
                Repaint();
                lEvent.Use();
            }
        }

        private void TryAutoAssignBoard()
        {
            if (_Board != null || Application.isPlaying)
                return;

            if (Selection.activeGameObject != null)
            {
                _Board = Selection.activeGameObject.GetComponentInParent<TilemapBoardAuthoring>();
                if (_Board != null)
                    return;
            }

            _Board = Object.FindAnyObjectByType<TilemapBoardAuthoring>();
        }
    }
}
