using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Grid.Pathfinding;
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

        [Header("Authoring (optional)")]
        [SerializeField] private EntityArchetypeAsset _unitAArchetype;
        [SerializeField] private EntityArchetypeAsset _unitBArchetype;

        [Header("Camera (Ankama-like Isometric)")]
        [SerializeField] private bool _autoSetupIsometricCamera = true;
        [SerializeField] private float _cameraTiltX = 35f;
        [SerializeField] private float _cameraYawY = 45f;
        [SerializeField] private float _cameraDistance = 18f;
        [SerializeField] private float _cameraHeightOffset = 10f;

        private const int MapWidth = 12;
        private const int MapDepth = 12;

        private readonly HashSet<Position3> _mapTiles = new();
        private readonly List<GameObject> _reachableOverlayTiles = new();
        private readonly HashSet<Position3> _currentReachableTiles = new();

        private MoveActionPolicy _effectiveMovePolicy;
        private Material _reachableTileMaterial;
        private LineRenderer _pathLineRenderer;

        private EntityId _lastPreviewActor;
        private int _lastPreviewMovementPoints = int.MinValue;
        private bool _lastPreviewMoveMode;
        private bool _lastPreviewHasSelection;

        private ActionResolution _lastResult;
        private MouseIntentMode _mouseIntentMode = MouseIntentMode.Move;
        private int _selectedSkillSlot;

        private void Start()
        {
            var runtimePolicies = CombatRuntimePolicyProvider.FromAsset(_runtimeConfig);
            _effectiveMovePolicy = runtimePolicies.MovePolicyOverride
                ?? new MoveActionPolicy(maxMovementCostPerAction: 6, maxVerticalStepPerTile: 1);

            var actorDefinitions = BuildActorDefinitionsFromArchetypes();
            var skillLoadoutMap = EntityArchetypeRuntimeAdapter.BuildSkillLoadoutMap(
                VerticalSliceBattleLoop.UnitA,
                _unitAArchetype,
                VerticalSliceBattleLoop.UnitB,
                _unitBArchetype);

            _battleLoop = new VerticalSliceBattleLoop(
                movePolicyOverride: _effectiveMovePolicy,
                basicAttackPolicyOverride: runtimePolicies.BasicAttackPolicyOverride,
                actorDefinitions: actorDefinitions);

            EntityArchetypeRuntimeAdapter.ApplyRuntimeResources(_battleLoop.State, VerticalSliceBattleLoop.UnitA, _unitAArchetype);
            EntityArchetypeRuntimeAdapter.ApplyRuntimeResources(_battleLoop.State, VerticalSliceBattleLoop.UnitB, _unitBArchetype);

            _planner = new VerticalSliceCommandPlanner(
                _battleLoop.State,
                _effectiveMovePolicy,
                runtimePolicies.BasicAttackPolicyOverride,
                runtimePolicies.SkillPolicyOverride,
                skillLoadoutMap);

            BuildSteppedMap();
            EnsureAnkamaLikeCamera();
            SetupMovementPreviewVisuals();

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
            UpdateMovementPreviewVisuals();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_planner.TryBuildCommand(out var actorId, out var command))
                {
                    _battleLoop.TryExecutePlannedCommand(actorId, command, out _lastResult);
                    _planner.ClearPlannedAction();
                }
                else
                {
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

            var panel = new Rect(12f, 12f, 760f, 250f);
            GUI.Box(panel, "Vertical Slice");
            GUI.Label(new Rect(24f, 38f, 740f, 20f), $"Tick: {_battleLoop.State.Tick} | Round: {_battleLoop.CurrentRound}");
            GUI.Label(new Rect(24f, 58f, 740f, 20f), $"Actor: {_battleLoop.PeekCurrentActorLabel()} | Next: {_battleLoop.PeekNextStepLabel()} | AP:{_battleLoop.RemainingActions} | PM:{_battleLoop.CurrentActorMovementPoints} | Timer:{_battleLoop.RemainingTurnSeconds:0.0}s");
            GUI.Label(new Rect(24f, 78f, 740f, 20f), $"HP UnitA: {hpA} | HP UnitB: {hpB}");
            GUI.Label(new Rect(24f, 98f, 740f, 20f), $"Selected: {selected} | Planned: {planned} | MouseMode: {_mouseIntentMode} | SkillSlot:{_selectedSkillSlot}");
            GUI.Label(new Rect(24f, 118f, 740f, 20f), $"Last: {_lastResult.Code} / {_lastResult.FailureReason}");
            GUI.Label(new Rect(24f, 138f, 740f, 20f), _battleLoop.IsBattleOver ? $"Winner Team: {_battleLoop.WinnerTeamId}" : "Mouse: left click world/unit. Keys: 1/2 select, M/A/S mode, Q/E skill slot, P pass, SPACE execute.");

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

            if (GUI.Button(new Rect(422f, 166f, 90f, 28f), "Skill"))
            {
                _mouseIntentMode = MouseIntentMode.Skill;
            }

            if (GUI.Button(new Rect(518f, 166f, 90f, 28f), "Execute"))
            {
                TryExecutePlanned();
            }

            if (GUI.Button(new Rect(614f, 166f, 90f, 28f), "Pass Turn"))
            {
                TryPassTurn();
            }

            GUI.Label(new Rect(24f, 204f, 740f, 30f), "Bleu = déplacements possibles. Survol d'une case bleue en mode Move => aperçu du chemin blanc.");
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
                _planner.PlanSkill(target, _selectedSkillSlot);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                _selectedSkillSlot = _selectedSkillSlot > 0 ? _selectedSkillSlot - 1 : 0;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                _selectedSkillSlot++;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                TryPassTurn();
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
                var destinationPosition = new Position3(destination.X, destination.Y, destination.Z);
                if (_battleLoop.State.IsPositionOccupied(destinationPosition, _planner.SelectedActorId))
                {
                    _lastResult = new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination occupied ({destination})", ActionFailureReason.DestinationOccupied);
                    return;
                }

                if (!_currentReachableTiles.Contains(destinationPosition))
                {
                    _lastResult = new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination unreachable ({destination})", ActionFailureReason.MovementBudgetExceeded);
                    return;
                }

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
                _planner.PlanSkill(clickedActor, _selectedSkillSlot);
                TryExecutePlanned();
            }
        }

        private void TryPassTurn()
        {
            if (!_planner.HasActorSelection)
            {
                return;
            }

            _battleLoop.TryPassTurn(_planner.SelectedActorId, out _lastResult);
            _planner.ClearPlannedAction();
            SyncUnitViews();
            Debug.Log($"[VerticalSlice] {_lastResult.Description}");
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

        private void SetupMovementPreviewVisuals()
        {
            _reachableTileMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(0.18f, 0.62f, 1f, 0.35f)
            };

            var pathObject = new GameObject("Preview_PathLine");
            _pathLineRenderer = pathObject.AddComponent<LineRenderer>();
            _pathLineRenderer.material = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            _pathLineRenderer.widthMultiplier = 0.08f;
            _pathLineRenderer.positionCount = 0;
            _pathLineRenderer.useWorldSpace = true;
            _pathLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pathLineRenderer.receiveShadows = false;
            _pathLineRenderer.alignment = LineAlignment.View;
            _pathLineRenderer.numCapVertices = 4;
        }

        private void UpdateMovementPreviewVisuals()
        {
            if (_battleLoop == null || !_planner.HasActorSelection)
            {
                ClearMovementPreviewTiles();
                HidePathPreview();
                return;
            }

            var previewActor = _battleLoop.CurrentActorId;
            var inMoveMode = _mouseIntentMode == MouseIntentMode.Move;
            var hasSelection = _planner.HasActorSelection;
            var currentPm = _battleLoop.CurrentActorMovementPoints;

            if (!previewActor.Equals(_lastPreviewActor) ||
                currentPm != _lastPreviewMovementPoints ||
                inMoveMode != _lastPreviewMoveMode ||
                hasSelection != _lastPreviewHasSelection)
            {
                RebuildReachableTiles(previewActor);
                _lastPreviewActor = previewActor;
                _lastPreviewMovementPoints = currentPm;
                _lastPreviewMoveMode = inMoveMode;
                _lastPreviewHasSelection = hasSelection;
            }

            if (inMoveMode)
            {
                UpdateHoveredPathPreview(previewActor);
            }
            else
            {
                HidePathPreview();
            }
        }

        private void RebuildReachableTiles(EntityId actorId)
        {
            ClearMovementPreviewTiles();
            _currentReachableTiles.Clear();

            if (!_battleLoop.State.TryGetEntityPosition(actorId, out var originPosition))
            {
                return;
            }

            var currentPm = _battleLoop.State.TryGetEntityMovementPoints(actorId, out var movementPoints)
                ? movementPoints
                : _effectiveMovePolicy.MaxMovementCostPerAction;

            var validator = new MoveValidationService();
            var origin = new GridCoord3(originPosition.X, originPosition.Y, originPosition.Z);

            foreach (var tile in _mapTiles)
            {
                var destination = new GridCoord3(tile.X, tile.Y, tile.Z);
                if (destination.Equals(origin))
                {
                    continue;
                }

                var validation = validator.Validate(_battleLoop.State, actorId, origin, destination, _effectiveMovePolicy);
                if (!validation.Success)
                {
                    continue;
                }

                if (validation.MovementCost > currentPm)
                {
                    continue;
                }

                AddReachableOverlayTile(tile);
            }

            // On affiche aussi la case courante de l'acteur.
            AddReachableOverlayTile(originPosition);
        }

        private void AddReachableOverlayTile(Position3 tile)
        {
            if (!_currentReachableTiles.Add(tile))
            {
                return;
            }

            var overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
            overlay.name = $"MovePreview_{tile.X}_{tile.Y}_{tile.Z}";
            overlay.transform.position = new Vector3(tile.X, tile.Z + 0.51f, tile.Y);
            overlay.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            overlay.transform.localScale = new Vector3(0.95f, 0.95f, 0.95f);

            var renderer = overlay.GetComponent<Renderer>();
            renderer.material = _reachableTileMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var collider = overlay.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _reachableOverlayTiles.Add(overlay);
        }

        private void ClearMovementPreviewTiles()
        {
            for (var i = 0; i < _reachableOverlayTiles.Count; i++)
            {
                if (_reachableOverlayTiles[i] != null)
                {
                    Destroy(_reachableOverlayTiles[i]);
                }
            }

            _reachableOverlayTiles.Clear();
            _currentReachableTiles.Clear();
        }

        private void UpdateHoveredPathPreview(EntityId actorId)
        {
            if (!_battleLoop.State.TryGetEntityPosition(actorId, out var originPosition))
            {
                HidePathPreview();
                return;
            }

            if (Camera.main == null)
            {
                HidePathPreview();
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 250f))
            {
                HidePathPreview();
                return;
            }

            var destination = ToGrid(hit.point);
            var destinationPosition = new Position3(destination.X, destination.Y, destination.Z);
            if (!_currentReachableTiles.Contains(destinationPosition) || destinationPosition.Equals(originPosition))
            {
                HidePathPreview();
                return;
            }

            var blocked = BuildBlockedCellsForPathPreview(actorId, originPosition, destinationPosition);
            var pathService = new PathfindingService();
            if (!pathService.TryComputePath(
                    new GridCoord3(originPosition.X, originPosition.Y, originPosition.Z),
                    destination,
                    blocked,
                    out var path))
            {
                HidePathPreview();
                return;
            }

            _pathLineRenderer.positionCount = path.Count;
            for (var i = 0; i < path.Count; i++)
            {
                _pathLineRenderer.SetPosition(i, new Vector3(path[i].X, path[i].Z + 0.62f, path[i].Y));
            }
        }

        private HashSet<GridCoord3> BuildBlockedCellsForPathPreview(EntityId actorId, Position3 origin, Position3 destination)
        {
            var blocked = new HashSet<GridCoord3>();
            foreach (var blockedPosition in _battleLoop.State.GetBlockedPositions())
            {
                blocked.Add(new GridCoord3(blockedPosition.X, blockedPosition.Y, blockedPosition.Z));
            }

            foreach (var pair in _battleLoop.State.GetEntityPositions())
            {
                if (pair.Key.Equals(actorId))
                {
                    continue;
                }

                if (pair.Value.Equals(origin) || pair.Value.Equals(destination))
                {
                    continue;
                }

                blocked.Add(new GridCoord3(pair.Value.X, pair.Value.Y, pair.Value.Z));
            }

            return blocked;
        }

        private void HidePathPreview()
        {
            if (_pathLineRenderer != null)
            {
                _pathLineRenderer.positionCount = 0;
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

        private bool TryFindAdjacentMoveDestination(EntityId actorId, out GridCoord3 destination)
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
            for (var x = 0; x < MapWidth; x++)
            {
                for (var y = 0; y < MapDepth; y++)
                {
                    var checker = (x + y) % 2 == 0;
                    var color = checker
                        ? new Color(0.27f, 0.27f, 0.27f)
                        : new Color(0.33f, 0.33f, 0.33f);
                    CreateTileFromGrid(x, y, 0, color);
                    _mapTiles.Add(new Position3(x, y, 0));
                }
            }

            for (var x = 4; x <= 7; x++)
            {
                for (var y = 4; y <= 7; y++)
                {
                    CreateTileFromGrid(x, y, 1, new Color(0.42f, 0.42f, 0.42f));
                    _mapTiles.Add(new Position3(x, y, 1));
                }
            }

            AddBlockingColumn(6, 2, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(6, 3, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(6, 4, 1, new Color(0.2f, 0.2f, 0.2f));
            AddBlockingColumn(5, 6, 2, new Color(0.18f, 0.18f, 0.18f));
            AddBlockingColumn(7, 6, 2, new Color(0.18f, 0.18f, 0.18f));
        }

        private void EnsureAnkamaLikeCamera()
        {
            if (!_autoSetupIsometricCamera)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            var centerX = (MapWidth - 1) * 0.5f;
            var centerY = (MapDepth - 1) * 0.5f;
            var mapCenter = new Vector3(centerX, 0f, centerY);

            var rotation = Quaternion.Euler(_cameraTiltX, _cameraYawY, 0f);
            var backward = rotation * Vector3.back;
            var cameraPosition = mapCenter + (backward * _cameraDistance) + Vector3.up * _cameraHeightOffset;

            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.rotation = rotation;
            mainCamera.orthographic = false;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 250f;

            if (!mainCamera.TryGetComponent<AudioListener>(out _))
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
            }
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

        private IReadOnlyList<PES.Core.TurnSystem.BattleActorDefinition> BuildActorDefinitionsFromArchetypes()
        {
            var actorDefinitions = new[]
            {
                EntityArchetypeRuntimeAdapter.BuildActorDefinition(
                    VerticalSliceBattleLoop.UnitA,
                    teamId: 1,
                    startPosition: new Position3(0, 0, 0),
                    archetype: _unitAArchetype),
                EntityArchetypeRuntimeAdapter.BuildActorDefinition(
                    VerticalSliceBattleLoop.UnitB,
                    teamId: 2,
                    startPosition: new Position3(2, 0, 1),
                    archetype: _unitBArchetype),
            };

            return actorDefinitions;
        }

        private enum MouseIntentMode
        {
            Move = 0,
            Attack = 1,
            Skill = 2,
        }
    }
}
