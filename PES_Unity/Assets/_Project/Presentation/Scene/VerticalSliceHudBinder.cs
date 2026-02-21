using System;
using System.Collections.Generic;
using UnityEngine;

namespace PES.Presentation.Scene
{
    public sealed class VerticalSliceHudBinder
    {
        public void Draw(
            VerticalSliceBattleLoop battleLoop,
            VerticalSliceCommandPlanner planner,
            VerticalSliceMouseIntentMode mouseIntentMode,
            int selectedSkillSlot,
            Action selectUnitA,
            Action selectUnitB,
            Action setMoveMode,
            Action setAttackMode,
            Action setSkillMode,
            Action executePlanned,
            Action passTurn,
            Action drawSkillKitButtons,
            Action drawLegendLabel,
            Func<string> getSelectedSkillLabel,
            Func<string> getSelectedSkillTooltip,
            Func<string> getActionFeedbackLabel,
            Func<IReadOnlyList<string>> getRecentActionHistory)
        {
            var hpA = battleLoop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitA, out var valueA) ? valueA : -1;
            var hpB = battleLoop.State.TryGetEntityHitPoints(VerticalSliceBattleLoop.UnitB, out var valueB) ? valueB : -1;
            var selected = planner.HasActorSelection ? planner.SelectedActorId.ToString() : "None";
            var planned = planner.PlannedLabel;
            var availableSkills = planner.HasActorSelection ? planner.GetAvailableSkillCount(planner.SelectedActorId) : 0;

            var panel = new Rect(12f, 12f, 760f, 410f);
            GUI.Box(panel, "Vertical Slice");
            GUI.Label(new Rect(24f, 38f, 740f, 20f), $"Tick: {battleLoop.State.Tick} | Round: {battleLoop.CurrentRound}");
            GUI.Label(new Rect(24f, 58f, 740f, 20f), $"Actor: {battleLoop.PeekCurrentActorLabel()} | Next: {battleLoop.PeekNextStepLabel()} | AP:{battleLoop.RemainingActions} | PM:{battleLoop.CurrentActorMovementPoints} | Timer:{battleLoop.RemainingTurnSeconds:0.0}s");
            GUI.Label(new Rect(24f, 78f, 740f, 20f), $"HP UnitA: {hpA} | HP UnitB: {hpB}");
            GUI.Label(new Rect(24f, 98f, 740f, 20f), $"Selected: {selected} | Planned: {planned} | MouseMode: {mouseIntentMode} | SkillSlot:{selectedSkillSlot + 1}/{(availableSkills > 0 ? availableSkills : 0)} ({getSelectedSkillLabel()})");
            GUI.Label(new Rect(24f, 118f, 740f, 20f), $"Last action: {getActionFeedbackLabel()}");
            GUI.Label(new Rect(24f, 138f, 740f, 20f), battleLoop.IsBattleOver ? $"Winner Team: {battleLoop.WinnerTeamId}" : "Mouse: left click world/unit. Keys: 1/2 select, M/A/S mode, Q/E skill slot, P pass, SPACE execute.");

            if (GUI.Button(new Rect(24f, 166f, 90f, 28f), "Select A")) selectUnitA();
            if (GUI.Button(new Rect(120f, 166f, 90f, 28f), "Select B")) selectUnitB();
            if (GUI.Button(new Rect(230f, 166f, 90f, 28f), "Move")) setMoveMode();
            if (GUI.Button(new Rect(326f, 166f, 90f, 28f), "Attack")) setAttackMode();
            if (GUI.Button(new Rect(422f, 166f, 90f, 28f), "Skill")) setSkillMode();
            if (GUI.Button(new Rect(518f, 166f, 90f, 28f), "Execute")) executePlanned();
            if (GUI.Button(new Rect(614f, 166f, 90f, 28f), "Pass Turn")) passTurn();

            drawSkillKitButtons();
            GUI.Label(new Rect(24f, 282f, 740f, 20f), $"Skill tooltip: {getSelectedSkillTooltip()}");

            var recentActionHistory = getRecentActionHistory();
            GUI.Label(new Rect(24f, 302f, 740f, 20f), "Action log (latest):");
            for (var i = 0; i < recentActionHistory.Count && i < 3; i++)
            {
                GUI.Label(new Rect(24f, 322f + (i * 20f), 740f, 20f), recentActionHistory[i]);
            }
            drawLegendLabel();
        }
    }
}
