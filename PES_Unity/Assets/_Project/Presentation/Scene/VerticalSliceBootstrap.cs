using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Presentation.Adapters;
using PES.Presentation.Configuration;
using UnityEngine;

namespace PES.Presentation.Scene
{
    /// <summary>
    /// Bootstrap MonoBehaviour pour visualiser une mini boucle tactique 3D.
    /// Jouable clavier + souris avec UI mono-spell.
    /// </summary>
    public sealed class VerticalSliceBootstrap : MonoBehaviour
    {
        private VerticalSliceBattleLoop _battleLoop;
        private VerticalSliceCommandPlanner _planner;
        private GameObject _unitAView;
        private GameObject _unitBView;
        [SerializeField] private CombatRuntimeConfigAsset _runtimeConfig;

        private ActionResolution _lastResult;
        private MouseIntentMode _mouseIntentMode = MouseIntentMode.Move;

        private void Start()
        {
            var runtimePolicies = CombatRuntimePolicyProvider.FromAsset(_runtimeConfig);
            _battleLoop = new VerticalSliceBattleLoop(
                movePolicyOverride: runtimePolicies.MovePolicyOverride,
                basicAttackPolicyOverride: runtimePolicies.BasicAttackPolicyOverride);
            _planner = new VerticalSliceCommandPlanner(
                _battleLoop.State,
                runtimePolicies.MovePolicyOverride,
                runtimePolicies.BasicAttackPolicyOverride,
                runtimePolicies.SkillPolicyOverride);

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

            if (_battleLoop.TryAdvanceTurnTimer(Time.deltaTime, out var timeoutResult))
            {
                _lastResult = timeoutResult;
                _planner.ClearPlannedAction();
                Debug.Log($"[VerticalSlice] {_lastResult.Description}");
            }

            ProcessSelectionInputs();
            ProcessPlanningInputs();
            ProcessMouseInputs();

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

            var panel = new Rect(12f, 12f, 700f, 244f);
            GUI.Box(panel, "Vertical Slice");
            GUI.Label(new Rect(24f, 38f, 680f, 20f), $"Tick: {_battleLoop.State.Tick} | Round: {_battleLoop.CurrentRound}");
            GUI.Label(new Rect(24f, 58f, 680f, 20f), $"Actor: {_battleLoop.PeekCurrentActorLabel()} | Next: {_battleLoop.PeekNextStepLabel()} | AP:{_battleLoop.RemainingActions} | Timer:{_battleLoop.RemainingTurnSeconds:0.0}s");
            GUI.Label(new Rect(24f, 78f, 680f, 20f), $"HP UnitA: {hpA} | HP UnitB: {hpB}");
            GUI.Label(new Rect(24f, 98f, 680f, 20f), $"Selected: {selected} | Planned: {planned} | MouseMode: {_mouseIntentMode}");
            GUI.Label(new Rect(24f, 118f, 680f, 20f), $"Last: {_lastResult.Code} / {_lastResult.FailureReason}");
            GUI.Label(new Rect(24f, 138f, 680f, 20f), _battleLoop.IsBattleOver ? $"Winner Team: {_battleLoop.WinnerTeamId}" : "Mouse: left click world/unit. Keys: 1/2 select, M/A/S mode, SPACE execute.");

            if (GUI.Button(new Rect(24f, 166f, 90f, 28f), "Select A"))
            {
                _planner.SelectActor(VerticalSliceBattleLoop.UnitA);
            }

            if (GUI.Button(new Rect(120f, 166f, 90f, 28f), "Select B"))
            {
                _planner.SelectActor(VerticalSliceBattleLoop.UnitB);
            }

            if (GUI.Button(new Rect(230f, 166f, 90f, 28f), "Move"))
            {
                _mouseIntentMode = MouseIntentMode.Move;
            }

            if (GUI.Button(new Rect(326f, 166f, 90f, 28f), "Attack"))
            {
                _mouseIntentMode = MouseIntentMode.Attack;
            }

            if (GUI.Button(new Rect(422f, 166f, 90f, 28f), "MonoSpell"))
            {
                _mouseIntentMode = MouseIntentMode.Skill;
            }

            if (GUI.Button(new Rect(518f, 166f, 90f, 28f), "Execute"))
            {
                TryExecutePlanned();
            }

            GUI.Label(new Rect(24f, 204f, 680f, 30f), "Flow souris: Select A/B -> choisir mode (Move/Attack/MonoSpell) -> clic sur map/cible -> Execute (ou auto-exec sur clic). ");
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
                _mouseIntentMode = MouseIntentMode.Move;
                if (TryFindAdjacentMoveDestination(_planner.SelectedActorId, out var destination))
                {
                    _planner.PlanMove(destination);
                }
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                _mouseIntentMode = MouseIntentMode.Attack;
                var target = _planner.SelectedActorId.Equals(VerticalSliceBattleLoop.UnitA)
                    ? VerticalSliceBattleLoop.UnitB
                    : VerticalSliceBattleLoop.UnitA;
                _planner.PlanAttack(target);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                _mouseIntentMode = MouseIntentMode.Skill;
                var target = _planner.SelectedActorId.Equals(VerticalSliceBattleLoop.UnitA)
                    ? VerticalSliceBattleLoop.UnitB
                    : VerticalSliceBattleLoop.UnitA;
                _planner.PlanSkill(target);
            }
        }

        private void ProcessMouseInputs()
        {
            if (!_planner.HasActorSelection || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            var ray = Camera.main != null
                ? Camera.main.ScreenPointToRay(Input.mousePosition)
                : new Ray();

            if (Camera.main == null || !Physics.Raycast(ray, out var hit, 250f))
            {
                return;
            }

            if (_mouseIntentMode == MouseIntentMode.Move)
            {
                var destination = ToGrid(hit.point);
                _planner.PlanMove(destination);
                TryExecutePlanned();
                return;
            }

            if (!TryResolveActorFromHit(hit.collider.gameObject, out var clickedActor))
            {
                return;
            }

            var selectedActor = _planner.SelectedActorId;
            if (clickedActor.Equals(selectedActor))
            {
                return;
            }

            if (_mouseIntentMode == MouseIntentMode.Attack)
            {
                _planner.PlanAttack(clickedActor);
                TryExecutePlanned();
            }
            else if (_mouseIntentMode == MouseIntentMode.Skill)
            {
                _planner.PlanSkill(clickedActor);
                TryExecutePlanned();
            }
        }

        private void TryExecutePlanned()
        {
            if (_battleLoop.IsBattleOver)
            {
                return;
            }

            if (_planner.TryBuildCommand(out var actorId, out var command))
            {
                _battleLoop.TryExecutePlannedCommand(actorId, command, out _lastResult);
                _planner.ClearPlannedAction();
                SyncUnitViews();
                Debug.Log($"[VerticalSlice] {_lastResult.Description}");
            }
        }

        private static bool TryResolveActorFromHit(GameObject hitObject, out EntityId actorId)
        {
            actorId = default;

            if (hitObject.name == "UnitA")
            {
                actorId = VerticalSliceBattleLoop.UnitA;
                return true;
            }

            if (hitObject.name == "UnitB")
            {
                actorId = VerticalSliceBattleLoop.UnitB;
                return true;
            }

            return false;
        }

        private bool TryFindAdjacentMoveDestination(Core.Simulation.EntityId actorId, out GridCoord3 destination)
        {
            destination = default;

            if (!_battleLoop.State.TryGetEntityPosition(actorId, out var currentPosition))
            {
                return false;
            }

            var origin = new GridCoord3(currentPosition.X, currentPosition.Y, currentPosition.Z);
            var candidates = new List<GridCoord3>
            {
                new(origin.X + 1, origin.Y, origin.Z),
                new(origin.X - 1, origin.Y, origin.Z),
                new(origin.X, origin.Y + 1, origin.Z),
                new(origin.X, origin.Y - 1, origin.Z),
                new(origin.X, origin.Y, origin.Z + 1),
                new(origin.X, origin.Y, origin.Z - 1),
            };

            foreach (var candidate in candidates)
            {
                var asPosition = new Position3(candidate.X, candidate.Y, candidate.Z);
                if (_battleLoop.State.IsPositionBlocked(asPosition))
                {
                    continue;
                }

                if (_battleLoop.State.IsPositionOccupied(asPosition, actorId))
                {
                    continue;
                }

                destination = candidate;
                return true;
            }

            return false;
        }

        private void BuildSteppedMap()
        {
            const int width = 12;
            const int depth = 12;

            // Sol principal : grande zone jouable pour tests manuels in-engine.
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < depth; y++)
                {
                    var checker = (x + y) % 2 == 0;
                    var color = checker
                        ? new Color(0.27f, 0.27f, 0.27f)
                        : new Color(0.33f, 0.33f, 0.33f);
                    CreateTileFromGrid(x, y, 0, color);
                }
            }

            // Plateforme surélevée de test (élévation skills/LOS).
            for (var x = 4; x <= 7; x++)
            {
                for (var y = 4; y <= 7; y++)
                {
                    CreateTileFromGrid(x, y, 1, new Color(0.42f, 0.42f, 0.42f));
                }
            }

            // Quelques obstacles de ligne de vue au centre.
            AddBlockingColumn(6, 2, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(6, 3, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(6, 4, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(5, 6, 2, new Color(0.18f, 0.18f, 0.18f));
            AddBlockingColumn(7, 6, 2, new Color(0.18f, 0.18f, 0.18f));
        }


        private void AddBlockingColumn(int x, int y, int height, Color color)
        {
            if (height < 1)
            {
                height = 1;
            }

            for (var z = 1; z <= height; z++)
            {
                CreateTileFromGrid(x, y, z, color);
                _battleLoop.State.SetBlockedPosition(new Position3(x, y, z), blocked: true);
            }
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

        private static GridCoord3 ToGrid(Vector3 world)
        {
            var x = Mathf.RoundToInt(world.x);
            var y = Mathf.RoundToInt(world.z);
            var z = Mathf.RoundToInt(world.y);
            return new GridCoord3(x, y, z);
        }

        private enum MouseIntentMode
        {
            Move = 0,
            Attack = 1,
            Skill = 2,
        }
    }
}
