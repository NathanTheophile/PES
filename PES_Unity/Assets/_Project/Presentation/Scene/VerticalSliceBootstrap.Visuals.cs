using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Grid.Pathfinding;
using UnityEngine;
using EntityId = PES.Core.Simulation.EntityId;

namespace PES.Presentation.Scene
{
    public sealed partial class VerticalSliceBootstrap
    {
        private void SetupActionVfxPlaceholders()
        {
            _vfxSuccessMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(0.2f, 1f, 0.35f, 0.55f)
            };

            _vfxMissedMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.9f, 0.2f, 0.5f)
            };

            _vfxRejectedMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.25f, 0.25f, 0.52f)
            };
        }

        private void PlayActionVfxPlaceholder(EntityId actorId, EntityId? targetId, ActionResolution resolution)
        {
            if (_battleLoop == null)
            {
                return;
            }

            if (_battleLoop.State.TryGetEntityPosition(actorId, out var actorPosition))
            {
                SpawnTransientPulse(actorPosition, ResolveVfxMaterial(resolution.Code), 0.42f, 0.35f);
            }

            if (targetId.HasValue && _battleLoop.State.TryGetEntityPosition(targetId.Value, out var targetPosition))
            {
                SpawnTransientPulse(targetPosition, ResolveVfxMaterial(resolution.Code), 0.58f, 0.42f);
            }
        }

        private Material ResolveVfxMaterial(ActionResolutionCode code)
        {
            return code switch
            {
                ActionResolutionCode.Succeeded => _vfxSuccessMaterial,
                ActionResolutionCode.Missed => _vfxMissedMaterial,
                _ => _vfxRejectedMaterial,
            };
        }

        private void SpawnTransientPulse(Position3 position, Material material, float yOffset, float ttlSeconds)
        {
            if (material == null)
            {
                return;
            }

            var pulse = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pulse.name = $"VfxPulse_{position.X}_{position.Y}_{position.Z}_{_transientVfxPulses.Count}";
            pulse.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pulse.transform.position = new Vector3(position.X, position.Z + yOffset, position.Y);
            pulse.transform.localScale = new Vector3(0.82f, 0.82f, 0.82f);

            var renderer = pulse.GetComponent<Renderer>();
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var collider = pulse.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            _transientVfxPulses.Add(new TransientVfxPulse(pulse, ttlSeconds));
        }

        private void UpdateTransientVfxPulses(float deltaTime)
        {
            for (var i = _transientVfxPulses.Count - 1; i >= 0; i--)
            {
                var pulse = _transientVfxPulses[i];
                var nextTtl = pulse.TtlSeconds - deltaTime;
                if (nextTtl <= 0f)
                {
                    if (pulse.View != null)
                    {
                        UnityEngine.Object.Destroy(pulse.View);
                    }

                    _transientVfxPulses.RemoveAt(i);
                    continue;
                }

                _transientVfxPulses[i] = new TransientVfxPulse(pulse.View, nextTtl);
            }
        }

        private void SetupActionIntentPreviewVisuals()
        {
            _plannedMoveMarkerMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(0.2f, 0.95f, 0.9f, 0.5f)
            };

            _plannedAttackMarkerMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.25f, 0.2f, 0.6f)
            };

            _plannedSkillMarkerMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.8f, 0.2f, 0.65f)
            };

            _hoverAttackMarkerMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.45f, 0.15f, 0.42f)
            };

            _hoverSkillMarkerMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.92f, 0.25f, 0.42f)
            };

            _plannedMoveMarkerView = CreateFlatPreviewMarker("Preview_MoveIntent", _plannedMoveMarkerMaterial, 0.92f);
            _plannedTargetMarkerView = CreateFlatPreviewMarker("Preview_TargetIntent", _plannedAttackMarkerMaterial, 1.16f);
            _hoverTargetMarkerView = CreateFlatPreviewMarker("Preview_HoverTarget", _hoverAttackMarkerMaterial, 1.28f);
            HideActionIntentPreviewVisuals();
            HideHoveredTargetPreviewVisuals();
        }

        private void UpdateActionIntentPreviewVisuals()
        {
            if (_plannedMoveMarkerView == null || _plannedTargetMarkerView == null || !_planner.HasPlannedAction)
            {
                HideActionIntentPreviewVisuals();
                return;
            }

            if (_planner.HasPlannedMove && _planner.TryGetPlannedMoveDestination(out var destination))
            {
                _plannedMoveMarkerView.SetActive(true);
                _plannedTargetMarkerView.SetActive(false);
                _plannedMoveMarkerView.transform.position = new Vector3(destination.X, destination.Z + 0.54f, destination.Y);
                return;
            }

            if (!_planner.TryGetPlannedTarget(out var targetId) || !_battleLoop.State.TryGetEntityPosition(targetId, out var targetPosition))
            {
                HideActionIntentPreviewVisuals();
                return;
            }

            _plannedMoveMarkerView.SetActive(false);
            _plannedTargetMarkerView.SetActive(true);
            _plannedTargetMarkerView.transform.position = new Vector3(targetPosition.X, targetPosition.Z + 0.58f, targetPosition.Y);

            var targetRenderer = _plannedTargetMarkerView.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                targetRenderer.material = _planner.HasPlannedSkill ? _plannedSkillMarkerMaterial : _plannedAttackMarkerMaterial;
            }
        }

        private void UpdateHoveredTargetPreviewVisuals()
        {
            if (_hoverTargetMarkerView == null || !_planner.HasActorSelection || Camera.main == null)
            {
                HideHoveredTargetPreviewVisuals();
                return;
            }

            if (_mouseIntentMode != VerticalSliceMouseIntentMode.Attack && _mouseIntentMode != VerticalSliceMouseIntentMode.Skill)
            {
                HideHoveredTargetPreviewVisuals();
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out var hit, 250f) || !TryResolveActorFromHit(hit.collider.gameObject, out var hoveredActorId))
            {
                HideHoveredTargetPreviewVisuals();
                return;
            }

            if (hoveredActorId.Equals(_planner.SelectedActorId) || !_battleLoop.State.TryGetEntityPosition(hoveredActorId, out var hoveredPosition))
            {
                HideHoveredTargetPreviewVisuals();
                return;
            }

            _hoverTargetMarkerView.SetActive(true);
            _hoverTargetMarkerView.transform.position = new Vector3(hoveredPosition.X, hoveredPosition.Z + 0.62f, hoveredPosition.Y);

            var hoverRenderer = _hoverTargetMarkerView.GetComponent<Renderer>();
            if (hoverRenderer != null)
            {
                hoverRenderer.material = _mouseIntentMode == VerticalSliceMouseIntentMode.Skill
                    ? _hoverSkillMarkerMaterial
                    : _hoverAttackMarkerMaterial;
            }
        }

        private void HideActionIntentPreviewVisuals()
        {
            if (_plannedMoveMarkerView != null)
            {
                _plannedMoveMarkerView.SetActive(false);
            }

            if (_plannedTargetMarkerView != null)
            {
                _plannedTargetMarkerView.SetActive(false);
            }
        }

        private void HideHoveredTargetPreviewVisuals()
        {
            if (_hoverTargetMarkerView != null)
            {
                _hoverTargetMarkerView.SetActive(false);
            }
        }

        private static GameObject CreateFlatPreviewMarker(string objectName, Material material, float scale)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
            marker.name = objectName;
            marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            marker.transform.localScale = new Vector3(scale, scale, scale);

            var renderer = marker.GetComponent<Renderer>();
            renderer.material = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            return marker;
        }

        private void SetupMovementPreviewVisuals()
        {
            _reachableTileMaterial = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(0.1f, 0.5f, 1f, 0.38f)
            };

            var pathPreview = new GameObject("MovePathPreview");
            _pathLineRenderer = pathPreview.AddComponent<LineRenderer>();
            _pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(0.9f, 0.95f, 1f, 0.95f)
            };
            _pathLineRenderer.widthMultiplier = 0.11f;
            _pathLineRenderer.positionCount = 0;
            _pathLineRenderer.numCapVertices = 2;
            _pathLineRenderer.useWorldSpace = true;
            _pathLineRenderer.sortingOrder = 10;
        }

        private void UpdateMovementPreviewVisuals()
        {
            if (_battleLoop == null || _planner == null)
            {
                ClearMovementPreviewTiles();
                HidePathPreview();
                return;
            }

            var hasSelection = _planner.HasActorSelection;
            var inMoveMode = _mouseIntentMode == VerticalSliceMouseIntentMode.Move;
            if (!hasSelection || !inMoveMode)
            {
                if (_currentReachableTiles.Count > 0)
                {
                    ClearMovementPreviewTiles();
                }

                HidePathPreview();
                _lastPreviewHasSelection = false;
                return;
            }

            var previewActor = _planner.SelectedActorId;
            var currentPm = _battleLoop.State.TryGetEntityMovementPoints(previewActor, out var movementPoints)
                ? movementPoints
                : _effectiveMovePolicy.MaxMovementCostPerAction;

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
                UnityEngine.Object.Destroy(collider);
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

        private readonly struct TransientVfxPulse
        {
            public TransientVfxPulse(GameObject view, float ttlSeconds)
            {
                View = view;
                TtlSeconds = ttlSeconds;
            }

            public GameObject View { get; }

            public float TtlSeconds { get; }
        }
    }
}
