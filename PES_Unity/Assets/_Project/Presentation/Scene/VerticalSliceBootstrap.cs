using System.Collections.Generic;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Presentation.Configuration;
using PES.Presentation.Flow;
using PES.Infrastructure.Serialization;
using UnityEngine;
using EntityId = PES.Core.Simulation.EntityId;

namespace PES.Presentation.Scene
{
    public sealed partial class VerticalSliceBootstrap : MonoBehaviour
    {
        private VerticalSliceBattleLoop _battleLoop;
        private ProductFlowController _productFlow;
        private VerticalSliceCommandPlanner _planner;
        private VerticalSliceHudBinder _hudBinder;
        private VerticalSliceInputBinder _inputBinder;
        private GameObject _unitAView;
        private GameObject _unitBView;

        [SerializeField] private CombatRuntimeConfigAsset _runtimeConfig;
        [SerializeField] private int _sessionSeed = 7;

        [Header("Session Flow")]
        [SerializeField] private string _sessionSaveKey = "PES.VerticalSlice.Session";
        [SerializeField] private bool _preferResumeOnBoot = true;

        [Header("Authoring (optional)")]
        [SerializeField] private EntityArchetypeAsset _unitAArchetype;
        [SerializeField] private EntityArchetypeAsset _unitBArchetype;

        [Header("Test Profile (optional)")]
        [SerializeField] private VerticalSliceTestProfileAsset _testProfile;

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
        private readonly List<TransientVfxPulse> _transientVfxPulses = new();

        private MoveActionPolicy _effectiveMovePolicy;
        private Material _reachableTileMaterial;
        private LineRenderer _pathLineRenderer;
        private Material _plannedMoveMarkerMaterial;
        private Material _plannedAttackMarkerMaterial;
        private Material _plannedSkillMarkerMaterial;
        private Material _hoverAttackMarkerMaterial;
        private Material _hoverSkillMarkerMaterial;
        private GameObject _plannedMoveMarkerView;
        private GameObject _plannedTargetMarkerView;
        private GameObject _hoverTargetMarkerView;

        private Material _vfxSuccessMaterial;
        private Material _vfxMissedMaterial;
        private Material _vfxRejectedMaterial;

        private EntityId _lastPreviewActor;
        private int _lastPreviewMovementPoints = int.MinValue;
        private bool _lastPreviewMoveMode;
        private bool _lastPreviewHasSelection;

        private ActionResolution _lastResult;
        private VerticalSliceMouseIntentMode _mouseIntentMode = VerticalSliceMouseIntentMode.Move;
        private int _selectedSkillSlot;

        private void Start()
        {
            var runtimeConfig = _testProfile != null && _testProfile.RuntimeConfig != null
                ? _testProfile.RuntimeConfig
                : _runtimeConfig;
            var unitAArchetype = _testProfile != null && _testProfile.UnitAArchetype != null
                ? _testProfile.UnitAArchetype
                : _unitAArchetype;
            var unitBArchetype = _testProfile != null && _testProfile.UnitBArchetype != null
                ? _testProfile.UnitBArchetype
                : _unitBArchetype;

            _productFlow = new ProductFlowController(new PlayerPrefsSessionSaveStore(_sessionSaveKey));
            _productFlow.Boot();

            if (_preferResumeOnBoot && _productFlow.HasBattleToResume)
            {
                _productFlow.TryResumeBattle();
            }
            else
            {
                _productFlow.StartNewBattle(_sessionSeed);
            }

            var seedToUse = _productFlow.LastBattleSeed > 0 ? _productFlow.LastBattleSeed : _sessionSeed;
            var setup = VerticalSliceBattleSetup.Create(runtimeConfig, unitAArchetype, unitBArchetype, seedToUse);
            var composition = new VerticalSliceCompositionRoot().Compose(setup);

            _battleLoop = composition.BattleLoop;
            _planner = composition.Planner;
            _effectiveMovePolicy = setup.EffectiveMovePolicy;
            _hudBinder = new VerticalSliceHudBinder();
            _inputBinder = new VerticalSliceInputBinder();

            BuildSteppedMap();
            EnsureAnkamaLikeCamera();
            SetupMovementPreviewVisuals();
            SetupActionIntentPreviewVisuals();
            SetupActionVfxPlaceholders();

            _unitAView = CreateUnitVisual("UnitA", Color.cyan);
            _unitBView = CreateUnitVisual("UnitB", Color.red);
            SyncUnitViews();

            _lastResult = new ActionResolution(true, ActionResolutionCode.Succeeded, "VerticalSlice ready");
            EnsureSelectedActorIsCurrentTurnActor();
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

            EnsureSelectedActorIsCurrentTurnActor();

            _inputBinder.ProcessSelectionInputs(_planner, SyncSelectedSkillSlot);
            _inputBinder.ProcessPlanningInputs(
                _planner,
                ref _mouseIntentMode,
                ref _selectedSkillSlot,
                TryFindAdjacentMoveDestinationAndPlan,
                PlanAttackToOtherActor,
                TryPlanSkill,
                CancelPlannedAction,
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
            UpdateActionIntentPreviewVisuals();
            UpdateHoveredTargetPreviewVisuals();
            UpdateTransientVfxPulses(Time.deltaTime);
            TryAutoPassTurnWhenNoActionsRemaining();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (_planner.TryBuildCommand(out var actorId, out var command))
                {
                    var hadTarget = _planner.TryGetPlannedTarget(out var plannedTarget);
                    _battleLoop.TryExecutePlannedCommand(actorId, command, out _lastResult);
                    PlayActionVfxPlaceholder(actorId, hadTarget ? plannedTarget : (EntityId?)null, _lastResult);
                    _planner.ClearPlannedAction();
                }
                else
                {
                    var scriptedActor = _battleLoop.CurrentActorId;
                    _lastResult = _battleLoop.ExecuteNextStep();
                    PlayActionVfxPlaceholder(scriptedActor, null, _lastResult);
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
                CancelPlannedAction,
                TryPassTurn,
                DrawSkillKitButtons,
                DrawLegendLabel,
                GetSelectedSkillLabel,
                GetSelectedSkillTooltip,
                GetActionFeedbackLabel,
                GetPlannedActionPreviewLabel,
                GetRecentActionHistory);
        }
    }
}
