using System;
using UnityEngine;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Presentation.Scene
{
    public sealed class VerticalSliceInputBinder
    {
        public void ProcessSelectionInputs(VerticalSliceCommandPlanner planner, Action syncSelectedSkillSlot)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                planner.SelectActor(VerticalSliceBattleLoop.UnitA);
                syncSelectedSkillSlot();
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                planner.SelectActor(VerticalSliceBattleLoop.UnitB);
                syncSelectedSkillSlot();
            }
        }

        public void ProcessPlanningInputs(
            VerticalSliceCommandPlanner planner,
            ref VerticalSliceMouseIntentMode mouseIntentMode,
            ref int selectedSkillSlot,
            Func<EntityId, bool> tryFindAdjacentMoveDestination,
            Action<EntityId> planAttackToOtherActor,
            Func<EntityId, bool> tryPlanSkill,
            Action tryPassTurn)
        {
            if (!planner.HasActorSelection)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                mouseIntentMode = VerticalSliceMouseIntentMode.Move;
                if (tryFindAdjacentMoveDestination(planner.SelectedActorId))
                {
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                mouseIntentMode = VerticalSliceMouseIntentMode.Attack;
                planAttackToOtherActor(planner.SelectedActorId);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                mouseIntentMode = VerticalSliceMouseIntentMode.Skill;
                var target = planner.SelectedActorId.Equals(VerticalSliceBattleLoop.UnitA)
                    ? VerticalSliceBattleLoop.UnitB
                    : VerticalSliceBattleLoop.UnitA;
                if (!tryPlanSkill(target))
                {
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                var availableSkills = planner.GetAvailableSkillCount(planner.SelectedActorId);
                selectedSkillSlot = availableSkills <= 0
                    ? 0
                    : selectedSkillSlot > 0 ? selectedSkillSlot - 1 : availableSkills - 1;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                var availableSkills = planner.GetAvailableSkillCount(planner.SelectedActorId);
                selectedSkillSlot = availableSkills <= 0
                    ? 0
                    : (selectedSkillSlot + 1) % availableSkills;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                tryPassTurn();
            }
        }

        public bool ProcessMouseInputs(
            VerticalSliceBattleLoop battleLoop,
            VerticalSliceCommandPlanner planner,
            VerticalSliceMouseIntentMode mouseIntentMode,
            Func<Vector3, GridCoord3> toGrid,
            Func<EntityId, GridCoord3, ActionResolution> planAndTryMove,
            Func<GameObject, (bool resolved, EntityId actorId)> resolveActorFromHit,
            Func<EntityId, bool> tryPlanSkill,
            Action<EntityId> planAttack,
            Action tryExecutePlanned,
            out ActionResolution immediateResult)
        {
            immediateResult = default;

            if (!planner.HasActorSelection || !Input.GetMouseButtonDown(0))
            {
                return false;
            }

            var ray = Camera.main != null ? Camera.main.ScreenPointToRay(Input.mousePosition) : new Ray();
            if (Camera.main == null || !Physics.Raycast(ray, out var hit, 250f))
            {
                return false;
            }

            if (mouseIntentMode == VerticalSliceMouseIntentMode.Move)
            {
                var destination = toGrid(hit.point);
                immediateResult = planAndTryMove(planner.SelectedActorId, destination);
                return true;
            }

            var resolved = resolveActorFromHit(hit.collider.gameObject);
            if (!resolved.resolved || resolved.actorId.Equals(planner.SelectedActorId))
            {
                return false;
            }

            if (mouseIntentMode == VerticalSliceMouseIntentMode.Attack)
            {
                planAttack(resolved.actorId);
                tryExecutePlanned();
                return false;
            }

            if (mouseIntentMode == VerticalSliceMouseIntentMode.Skill)
            {
                if (!tryPlanSkill(resolved.actorId))
                {
                    return false;
                }

                tryExecutePlanned();
            }

            return false;
        }
    }
}
