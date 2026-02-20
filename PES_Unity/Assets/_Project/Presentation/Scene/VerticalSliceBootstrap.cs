using PES.Core.Simulation;
using PES.Grid.Grid3D;
using UnityEngine;

namespace PES.Presentation.Scene
{
    /// <summary>
    /// Bootstrap MonoBehaviour pour visualiser une mini boucle tactique 3D.
    /// Appuyer sur Espace pour exécuter l'action suivante.
    /// </summary>
    public sealed class VerticalSliceBootstrap : MonoBehaviour
    {
        private VerticalSliceBattleLoop _battleLoop;
        private VerticalSliceCommandPlanner _planner;
        private GameObject _unitAView;
        private GameObject _unitBView;
        private ActionResolution _lastResult;

        private void Start()
        {
            _battleLoop = new VerticalSliceBattleLoop();
            _planner = new VerticalSliceCommandPlanner(_battleLoop.State);

            BuildSteppedMap();
            _unitAView = CreateUnitVisual("UnitA", Color.cyan);
            _unitBView = CreateUnitVisual("UnitB", Color.red);
            SyncUnitViews();
            _lastResult = new ActionResolution(true, ActionResolutionCode.Succeeded, "VerticalSlice ready");
        }

        private void Update()
        {
            if (_battleLoop == null)
            {
                return;
            }

            ProcessSelectionInputs();
            ProcessPlanningInputs();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_planner.TryBuildCommand(out var actorId, out var command))
                {
                    _battleLoop.TryExecutePlannedCommand(actorId, command, out _lastResult);
                    _planner.ClearPlannedAction();
                }
                else
                {
                    // Fallback : conserver la démo auto si aucune commande n'est planifiée.
                    _lastResult = _battleLoop.ExecuteNextStep();
                }

                SyncUnitViews();
                Debug.Log($"[VerticalSlice] {_lastResult.Description}");
            }
        }

        private void OnGUI()
        {
            if (_battleLoop == null)
            {
                return;
            }

            var hpA = _battleLoop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitA, out var valueA) ? valueA : -1;
            var hpB = _battleLoop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitB, out var valueB) ? valueB : -1;

            var selected = _planner.HasActorSelection ? _planner.SelectedActorId.ToString() : "None";
            var planned = _planner.PlannedLabel;

            var panel = new Rect(12f, 12f, 560f, 156f);
            GUI.Box(panel, "Vertical Slice");
            GUI.Label(new Rect(24f, 38f, 540f, 20f), $"Tick: {_battleLoop.State.Tick} | Round: {_battleLoop.CurrentRound}");
            GUI.Label(new Rect(24f, 58f, 540f, 20f), $"Actor: {_battleLoop.PeekCurrentActorLabel()} | Next: {_battleLoop.PeekNextStepLabel()} | AP:{_battleLoop.RemainingActions}");
            GUI.Label(new Rect(24f, 78f, 540f, 20f), $"HP UnitA: {hpA} | HP UnitB: {hpB}");
            GUI.Label(new Rect(24f, 98f, 540f, 20f), $"Selected: {selected} | Planned: {planned}");
            GUI.Label(new Rect(24f, 118f, 540f, 20f), $"Last: {_lastResult.Code} / {_lastResult.FailureReason}");
            GUI.Label(new Rect(24f, 138f, 540f, 20f), _battleLoop.IsBattleOver ? $"Winner Team: {_battleLoop.WinnerTeamId}" : "Keys: 1/2 select actor, M move, A attack, SPACE execute.");
        }

        private void ProcessSelectionInputs()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _planner.SelectActor(VerticalSliceBattleLoop.UnitB);
            }
        }

        private void ProcessPlanningInputs()
        {
            // IMPORTANT: aucune API GUI ici (OnGUI uniquement).
            if (!_planner.HasActorSelection)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                if (_planner.SelectedActorId.Equals(VerticalSliceBattleLoop.UnitA))
                {
                    _planner.PlanMove(new GridCoord3(1, 0, 1));
                }
                else
                {
                    _planner.PlanMove(new GridCoord3(2, 0, 1));
                }
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                var target = _planner.SelectedActorId.Equals(VerticalSliceBattleLoop.UnitA)
                    ? VerticalSliceBattleLoop.UnitB
                    : VerticalSliceBattleLoop.UnitA;
                _planner.PlanAttack(target);
            }
        }

        private void BuildSteppedMap()
        {
            CreateTileFromGrid(0, 0, 0, new Color(0.25f, 0.25f, 0.25f));
            CreateTileFromGrid(1, 0, 0, new Color(0.35f, 0.35f, 0.35f));
            CreateTileFromGrid(1, 0, 1, new Color(0.45f, 0.45f, 0.45f));
            CreateTileFromGrid(2, 0, 1, new Color(0.55f, 0.55f, 0.55f));
        }

        private static GameObject CreateTileFromGrid(int x, int y, int z, Color color)
        {
            return CreateTile(new Vector3(x, z, y), color);
        }

        private static GameObject CreateTile(Vector3 center, Color color)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"Tile_{center.x}_{center.y}_{center.z}";
            tile.transform.position = center;
            tile.transform.localScale = new Vector3(1f, 1f, 1f);

            var renderer = tile.GetComponent<Renderer>();
            renderer.material.color = color;
            return tile;
        }

        private static GameObject CreateUnitVisual(string objectName, Color color)
        {
            var unit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.name = objectName;

            var renderer = unit.GetComponent<Renderer>();
            renderer.material.color = color;
            return unit;
        }

        private void SyncUnitViews()
        {
            if (_battleLoop.State.TryGetEntityPosition(VerticalSliceBattleLoop.UnitA, out var unitAPosition))
            {
                _unitAView.transform.position = ToWorld(unitAPosition);
            }

            if (_battleLoop.State.TryGetEntityPosition(VerticalSliceBattleLoop.UnitB, out var unitBPosition))
            {
                _unitBView.transform.position = ToWorld(unitBPosition);
            }
        }

        private static Vector3 ToWorld(Position3 position)
        {
            return new Vector3(position.X, position.Z + 1.5f, position.Y);
        }
    }
}
