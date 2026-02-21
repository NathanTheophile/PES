using System.Collections.Generic;
using PES.Core.Simulation;
using UnityEngine;
using EntityId = PES.Core.Simulation.EntityId;

namespace PES.Presentation.Scene
{
    public sealed partial class VerticalSliceBootstrap
    {
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

            var previousGuiColor = GUI.color;
            for (var slot = 0; slot < skillCount; slot++)
            {
                var x = startX + (slot * (width + spacing));
                var label = GetSkillButtonLabel(actorId, slot);
                if (_selectedSkillSlot == slot)
                {
                    label = $"> {label}";
                }

                GUI.color = ResolveSkillSlotGuiColor(actorId, slot);
                if (GUI.Button(new Rect(x, startY, width, height), label))
                {
                    _selectedSkillSlot = slot;
                    _mouseIntentMode = VerticalSliceMouseIntentMode.Skill;
                }
            }

            GUI.color = previousGuiColor;
        }

        private void DrawLegendLabel()
        {
            GUI.Label(new Rect(24f, 412f, 740f, 20f), "Bleu = cases atteignables, ligne blanche = path. Marker cyan = move planifié, rouge = attaque planifiée, doré = skill planifiée, orange/doré translucide = cible survolée (attack/skill). Pulses vert/jaune/rouge = succès/miss/rejet action. Slots skills: vert READY, orange CD, rouge NO_RES.");
        }

        private string GetActionFeedbackLabel()
        {
            return ActionFeedbackFormatter.FormatResolutionSummary(_lastResult);
        }

        private string GetPlannedActionPreviewLabel()
        {
            if (!_planner.HasPlannedAction)
            {
                return "Aucune action planifiée";
            }

            if (_planner.HasPlannedMove && _planner.TryGetPlannedMoveDestination(out var destination))
            {
                if (!_battleLoop.State.TryGetEntityPosition(_planner.SelectedActorId, out var actorPosition))
                {
                    return $"Move preview: destination {destination}";
                }

                var moveCost =
                    Mathf.Abs(destination.X - actorPosition.X) +
                    Mathf.Abs(destination.Z - actorPosition.Z) +
                    Mathf.Abs(destination.Y - actorPosition.Y);
                var currentPm = _battleLoop.CurrentActorMovementPoints;
                var projectedPm = currentPm - moveCost;
                if (projectedPm < 0)
                {
                    projectedPm = 0;
                }

                return $"Move -> {destination} | coût~{moveCost} PM {currentPm}->{projectedPm}";
            }

            if (!_planner.TryGetPlannedTarget(out var targetId))
            {
                return _planner.PlannedLabel;
            }

            if (_planner.HasPlannedSkill)
            {
                var actorId = _planner.SelectedActorId;
                var skillSlot = _planner.PlannedSkillSlot;
                if (!_planner.TryGetSkillPolicy(actorId, skillSlot, out var policy))
                {
                    return "Skill preview indisponible (policy manquante)";
                }

                if (!_battleLoop.State.TryGetEntityHitPoints(targetId, out var hp))
                {
                    return $"Skill preview: cible invalide {targetId}";
                }

                var projectedHp = hp - policy.BaseDamage;
                if (projectedHp < 0)
                {
                    projectedHp = 0;
                }

                return $"Skill[{skillSlot}] dmg~{policy.BaseDamage}, hit {policy.BaseHitChance}% => HP cible {hp}->{projectedHp}";
            }

            if (!_battleLoop.State.TryGetEntityHitPoints(targetId, out var targetHp))
            {
                return $"Attack preview: cible invalide {targetId}";
            }

            return $"Attack preview: cible {targetId} HP actuel {targetHp}";
        }

        private IReadOnlyList<string> GetRecentActionHistory()
        {
            const int maxEntries = 3;
            var history = new List<string>(maxEntries);
            var log = _battleLoop.State.StructuredEventLog;
            for (var i = log.Count - 1; i >= 0 && history.Count < maxEntries; i--)
            {
                history.Add(ActionFeedbackFormatter.FormatEventRecordLine(log[i]));
            }

            if (history.Count == 0)
            {
                history.Add("(no action yet)");
            }

            return history;
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

        private Color ResolveSkillSlotGuiColor(EntityId actorId, int slot)
        {
            if (!_planner.TryGetSkillPolicy(actorId, slot, out var policy))
            {
                return new Color(0.35f, 0.35f, 0.35f, 1f);
            }

            var cooldown = _battleLoop.State.GetSkillCooldown(actorId, policy.SkillId);
            var resource = _battleLoop.State.TryGetEntitySkillResource(actorId, out var value) ? value : 0;
            var stateTag = ActionFeedbackFormatter.BuildSkillSlotStatusLabel(policy, cooldown, resource);

            return stateTag switch
            {
                "READY" => new Color(0.2f, 0.82f, 0.32f, 1f),
                _ when stateTag.StartsWith("CD:") => new Color(0.88f, 0.64f, 0.2f, 1f),
                _ when stateTag.StartsWith("NO_RES:") => new Color(0.86f, 0.3f, 0.3f, 1f),
                _ => Color.white,
            };
        }

        private string GetSkillButtonLabel(EntityId actorId, int slot)
        {
            if (!_planner.TryGetSkillPolicy(actorId, slot, out var policy))
            {
                return $"Skill {slot + 1}: n/a";
            }

            var cooldown = _battleLoop.State.GetSkillCooldown(actorId, policy.SkillId);
            var resource = _battleLoop.State.TryGetEntitySkillResource(actorId, out var value) ? value : 0;
            var stateTag = ActionFeedbackFormatter.BuildSkillSlotStatusLabel(policy, cooldown, resource);
            return $"S{slot + 1} [Id:{policy.SkillId}] {stateTag}";
        }
    }
}
