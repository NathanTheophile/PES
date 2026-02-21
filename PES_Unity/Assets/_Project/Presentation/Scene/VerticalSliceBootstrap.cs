using System;
using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Grid.Pathfinding;
using PES.Presentation.Configuration;
using UnityEngine;
using EntityId = PES.Core.Simulation.EntityId;

namespace PES.Presentation.Scene
{
    public sealed class VerticalSliceBootstrap : MonoBehaviour
    {
        private VerticalSliceBattleLoop _battleLoop;
        private VerticalSliceCommandPlanner _planner;
        private VerticalSliceHudBinder _hudBinder;
        private VerticalSliceInputBinder _inputBinder;
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
        private VerticalSliceMouseIntentMode _mouseIntentMode = VerticalSliceMouseIntentMode.Move;
        private int _selectedSkillSlot;

        private void Start()
        {
            var setup = VerticalSliceBattleSetup.Create(_runtimeConfig, _unitAArchetype, _unitBArchetype);
            var composition = new VerticalSliceCompositionRoot().Compose(setup);

            _battleLoop = composition.BattleLoop;
            _planner = composition.Planner;
            _effectiveMovePolicy = setup.EffectiveMovePolicy;
            _hudBinder = new VerticalSliceHudBinder();
            _inputBinder = new VerticalSliceInputBinder();

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

            _inputBinder.ProcessSelectionInputs(_planner, SyncSelectedSkillSlot);
            _inputBinder.ProcessPlanningInputs(
                _planner,
                ref _mouseIntentMode,
                ref _selectedSkillSlot,
                TryFindAdjacentMoveDestinationAndPlan,
                PlanAttackToOtherActor,
                TryPlanSkill,
                TryPassTurn);

            var hasImmediateResult = _inputBinder.ProcessMouseInputs(
                _battleLoop,
                _planner,
                _mouseIntentMode,
                ToGrid,
                PlanAndTryMove,
                hit => (TryResolveActorFromHit(hit, out var actor), actor),
                TryPlanSkill,
                target => _planner.PlanAttack(target),
                TryExecutePlanned,
                out var immediateResult);

            if (hasImmediateResult)
            {
                _lastResult = immediateResult;
            }

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

            _hudBinder.Draw(
                _battleLoop,
                _planner,
                _mouseIntentMode,
                _selectedSkillSlot,
                () =>
                {
                    _planner.SelectActor(VerticalSliceBattleLoop.UnitA);
                    SyncSelectedSkillSlot();
                },
                () =>
                {
                    _planner.SelectActor(VerticalSliceBattleLoop.UnitB);
                    SyncSelectedSkillSlot();
                },
                () => _mouseIntentMode = VerticalSliceMouseIntentMode.Move,
                () => _mouseIntentMode = VerticalSliceMouseIntentMode.Attack,
                () => _mouseIntentMode = VerticalSliceMouseIntentMode.Skill,
                TryExecutePlanned,
                TryPassTurn,
                DrawSkillKitButtons,
                DrawLegendLabel,
                GetSelectedSkillLabel,
                GetSelectedSkillTooltip,
                GetActionFeedbackLabel);
        }

        private void DrawSkillKitButtons()
        {
            if (!_planner.HasActorSelection)
            {
                GUI.Label(new Rect(24f, 204f, 740f, 20f), "Skills: select an actor to inspect skill kit.");
                return;
            }

            var actorId = _planner.SelectedActorId;
            var skillCount = _planner.GetAvailableSkillCount(actorId);
            if (skillCount <= 0)
            {
                GUI.Label(new Rect(24f, 204f, 740f, 20f), $"Skills: {actorId} has no configured skills.");
                return;
            }

            GUI.Label(new Rect(24f, 204f, 740f, 20f), $"Skills for {actorId}: click to select active slot.");

            const float startX = 24f;
            const float startY = 224f;
            const float width = 170f;
            const float height = 24f;
            const float spacing = 8f;

            for (var slot = 0; slot < skillCount; slot++)
            {
                var x = startX + (slot * (width + spacing));
                var label = GetSkillButtonLabel(actorId, slot);
                if (_selectedSkillSlot == slot)
                {
                    label = $"> {label}";
                }

                if (GUI.Button(new Rect(x, startY, width, height), label))
                {
                    _selectedSkillSlot = slot;
                    _mouseIntentMode = VerticalSliceMouseIntentMode.Skill;
                }
            }
        }

        private void DrawLegendLabel()
        {
            GUI.Label(new Rect(24f, 302f, 740f, 20f), "Bleu = déplacements possibles. Survol d'une case bleue en mode Move => aperçu du chemin blanc.");
        }


        private string GetActionFeedbackLabel()
        {
            return ActionFeedbackFormatter.FormatResolutionSummary(_lastResult);
        }

        private string GetSelectedSkillTooltip()
        {
            if (!_planner.HasActorSelection)
            {
                return "Sélectionne une unité pour voir le détail de la skill.";
            }

            var actorId = _planner.SelectedActorId;
            if (!_planner.TryGetSkillPolicy(actorId, _selectedSkillSlot, out var policy))
            {
                return "Aucune skill configurée sur ce slot.";
            }

            var cooldown = _battleLoop.State.GetSkillCooldown(actorId, policy.SkillId);
            var resource = _battleLoop.State.TryGetEntitySkillResource(actorId, out var value) ? value : 0;
            return ActionFeedbackFormatter.BuildSkillTooltip(policy, cooldown, resource);
        }

        private string GetSkillButtonLabel(EntityId actorId, int slot)
        {
            if (!_planner.TryGetSkillPolicy(actorId, slot, out var policy))
            {
                return $"Skill {slot + 1}: n/a";
            }

            var cooldown = _battleLoop.State.GetSkillCooldown(actorId, policy.SkillId);
            var resource = _battleLoop.State.TryGetEntitySkillResource(actorId, out var value) ? value : 0;
            var ready = cooldown <= 0 && resource >= policy.ResourceCost;
            var readyTag = ready ? "Ready" : $"CD:{cooldown} RES:{resource}/{policy.ResourceCost}";
            return $"S{slot + 1} [Id:{policy.SkillId}] {readyTag}";
        }

        private ActionResolution PlanAndTryMove(EntityId actorId, GridCoord3 destination)
        {
            var destinationPosition = new Position3(destination.X, destination.Y, destination.Z);
            if (_battleLoop.State.IsPositionOccupied(destinationPosition, actorId))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination occupied ({destination})", ActionFailureReason.DestinationOccupied);
            }

            if (!_currentReachableTiles.Contains(destinationPosition))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination unreachable ({destination})", ActionFailureReason.MovementBudgetExceeded);
            }

            _planner.PlanMove(destination);
            TryExecutePlanned();
            return _lastResult;
        }


        private bool TryFindAdjacentMoveDestinationAndPlan(EntityId actorId)
        {
            if (!TryFindAdjacentMoveDestination(actorId, out var destination))
            {
                return false;
            }

            _planner.PlanMove(destination);
            return true;
        }

        private void PlanAttackToOtherActor(EntityId actorId)
        {
            var target = actorId.Equals(VerticalSliceBattleLoop.UnitA)
                ? VerticalSliceBattleLoop.UnitB
                : VerticalSliceBattleLoop.UnitA;
            _planner.PlanAttack(target);
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
            _pathLineRenderer.positionCount = 0;
            _pathLineRenderer.widthMultiplier = 0.08f;
            _pathLineRenderer.numCapVertices = 4;
            _pathLineRenderer.useWorldSpace = true;
        }

        private void UpdateMovementPreviewVisuals()
        {
            var previewActor = _planner.HasActorSelection ? _planner.SelectedActorId : _battleLoop.CurrentActorId;
            var inMoveMode = _mouseIntentMode == VerticalSliceMouseIntentMode.Move;
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
                if (!validation.Success || validation.MovementCost > currentPm)
                {
                    continue;
                }

                AddReachableOverlayTile(tile);
            }

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
            if (!_battleLoop.State.TryGetEntityPosition(actorId, out var originPosition) || Camera.main == null)
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
            if (!pathService.TryComputePath(new GridCoord3(originPosition.X, originPosition.Y, originPosition.Z), destination, blocked, out var path))
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
                if (pair.Key.Equals(actorId) || pair.Value.Equals(origin) || pair.Value.Equals(destination))
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
                if (_battleLoop.State.IsPositionBlocked(asPosition) || _battleLoop.State.IsPositionOccupied(asPosition, actorId))
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
                    var color = checker ? new Color(0.27f, 0.27f, 0.27f) : new Color(0.33f, 0.33f, 0.33f);
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
            return new GridCoord3(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z), Mathf.RoundToInt(world.y));
        }

        private bool TryPlanSkill(EntityId targetId)
        {
            if (!_planner.HasActorSelection)
            {
                return false;
            }

            if (!_planner.TryGetSkillPolicy(_planner.SelectedActorId, _selectedSkillSlot, out var policy))
            {
                _lastResult = new ActionResolution(false, ActionResolutionCode.Rejected, $"SkillSelectionRejected: no skill in slot {_selectedSkillSlot + 1} for {_planner.SelectedActorId}", ActionFailureReason.InvalidPolicy);
                return false;
            }

            _planner.PlanSkill(targetId, _selectedSkillSlot);
            _lastResult = new ActionResolution(true, ActionResolutionCode.Succeeded, $"SkillSelected: {_planner.SelectedActorId} slot:{_selectedSkillSlot + 1} skill:{policy.SkillId}");
            return true;
        }

        private string GetSelectedSkillLabel()
        {
            if (_planner == null || !_planner.HasActorSelection)
            {
                return "n/a";
            }

            if (!_planner.TryGetSkillPolicy(_planner.SelectedActorId, _selectedSkillSlot, out var policy))
            {
                return "none";
            }

            return $"SkillId:{policy.SkillId}";
        }

        private void SyncSelectedSkillSlot()
        {
            if (_planner == null || !_planner.HasActorSelection)
            {
                _selectedSkillSlot = 0;
                return;
            }

            var availableSkills = _planner.GetAvailableSkillCount(_planner.SelectedActorId);
            if (availableSkills <= 0 || _selectedSkillSlot < 0 || _selectedSkillSlot >= availableSkills)
            {
                _selectedSkillSlot = 0;
            }
        }
    }
}
