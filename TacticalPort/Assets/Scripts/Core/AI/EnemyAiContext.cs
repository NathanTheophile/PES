#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public sealed class EnemyAiContext
    {
        public EnemyAiContext(
            IBattleService pBattleService,
            UnitRuntime pActor,
            IReadOnlyCollection<GridCoord> pReachableCells,
            Func<SkillDefinition, SkillTarget, BattleActionResult> pValidateSkill,
            bool pForceKiteThisDecision = false)
        {
            BattleService = pBattleService;
            Actor = pActor;
            ReachableCells = pReachableCells ?? Array.Empty<GridCoord>();
            ValidateSkill = pValidateSkill;
            ForceKiteThisDecision = pForceKiteThisDecision;
        }

        public IBattleService BattleService { get; }
        public UnitRuntime Actor { get; }
        public IReadOnlyCollection<GridCoord> ReachableCells { get; }
        public Func<SkillDefinition, SkillTarget, BattleActionResult> ValidateSkill { get; }
        public bool ForceKiteThisDecision { get; }

        public bool TryValidate(SkillDefinition pSkill, SkillTarget pTarget, out BattleActionResult pResult)
        {
            pResult = ValidateSkill != null ? ValidateSkill(pSkill, pTarget) : null;
            return pResult != null && pResult.IsSuccess;
        }
    }

    internal enum EnemyAiMovementMode
    {
        Approach = 0,
        Kite = 1
    }

    internal readonly struct EnemyAiProfileSettings
    {
        private EnemyAiProfileSettings(
            EnemyAiTargetPriority pTargetPriority,
            EnemyAiMovementPolicy pMovementPolicy,
            int pPreferredDistance,
            int pThreatRadius,
            int pDamageWeight,
            int pHealWeight,
            int pKillConfirmWeight,
            int pAoeWeight,
            int pSafetyWeight,
            bool pDebugDecisions)
        {
            TargetPriority = pTargetPriority;
            MovementPolicy = pMovementPolicy;
            PreferredDistance = pPreferredDistance < 0 ? 0 : pPreferredDistance;
            ThreatRadius = pThreatRadius < 0 ? 0 : pThreatRadius;
            DamageWeight = pDamageWeight < 0 ? 0 : pDamageWeight;
            HealWeight = pHealWeight < 0 ? 0 : pHealWeight;
            KillConfirmWeight = pKillConfirmWeight < 0 ? 0 : pKillConfirmWeight;
            AoeWeight = pAoeWeight < 0 ? 0 : pAoeWeight;
            SafetyWeight = pSafetyWeight < 0 ? 0 : pSafetyWeight;
            DebugDecisions = pDebugDecisions;
        }

        public EnemyAiTargetPriority TargetPriority { get; }
        public EnemyAiMovementPolicy MovementPolicy { get; }
        public int PreferredDistance { get; }
        public int ThreatRadius { get; }
        public int DamageWeight { get; }
        public int HealWeight { get; }
        public int KillConfirmWeight { get; }
        public int AoeWeight { get; }
        public int SafetyWeight { get; }
        public bool DebugDecisions { get; }

        public static EnemyAiProfileSettings Default =>
            new EnemyAiProfileSettings(EnemyAiTargetPriority.WeakFirst, EnemyAiMovementPolicy.Auto, 0, 0, 100, 100, 100, 100, 100, false);

        public static EnemyAiProfileSettings FromActor(UnitRuntime pActor)
        {
            if (pActor?.Definition == null)
                return Default;

            return new EnemyAiProfileSettings(
                pActor.Definition.EnemyAiTargetPriority,
                pActor.Definition.EnemyAiMovementPolicy,
                pActor.Definition.EnemyAiPreferredDistance,
                pActor.Definition.EnemyAiThreatRadius,
                pActor.Definition.EnemyAiDamageWeight,
                pActor.Definition.EnemyAiHealWeight,
                pActor.Definition.EnemyAiKillConfirmWeight,
                pActor.Definition.EnemyAiAoeWeight,
                pActor.Definition.EnemyAiSafetyWeight,
                pActor.Definition.EnemyAiDebugDecisions);
        }
    }

    internal readonly struct EnemyAiProfile
    {
        private EnemyAiProfile(
            Team pHostileTeam,
            EnemyAiProfileSettings pSettings,
            EnemyAiMovementMode pMovementMode,
            int pPreferredThreatDistance)
        {
            HostileTeam = pHostileTeam;
            Settings = pSettings;
            MovementMode = pMovementMode;
            PreferredThreatDistance = Math.Max(1, pPreferredThreatDistance);
        }

        public Team HostileTeam { get; }
        public EnemyAiProfileSettings Settings { get; }
        public EnemyAiMovementMode MovementMode { get; }
        public int PreferredThreatDistance { get; }

        public static EnemyAiProfile Create(EnemyAiContext pContext)
        {
            UnitRuntime lActor = pContext?.Actor;
            Team lHostileTeam = ResolveHostileTeam(lActor);
            EnemyAiProfileSettings lSettings = EnemyAiProfileSettings.FromActor(lActor);
            if (lActor == null)
                return new EnemyAiProfile(lHostileTeam, lSettings, EnemyAiMovementMode.Approach, 1);

            int lBestOffensiveRange = ResolveBestOffensiveRange(lActor);
            bool lCanFightAtRange = lBestOffensiveRange >= 3;
            bool lLowHealth = lActor.CurrentHealth * 100 <= lActor.CurrentMaxHealth * 45;
            int lNearestThreatDistance = ResolveNearestThreatDistance(pContext, lHostileTeam);
            int lThreatRadius = lSettings.ThreatRadius > 0 ? lSettings.ThreatRadius : 2;
            bool lThreatened = lNearestThreatDistance <= lThreatRadius;
            bool lHasActed = lActor.RemainingEnergy < Math.Max(0, lActor.Definition.EnergyPerTurn + lActor.GetEnergyModifier());
            bool lShouldForceKite = pContext.ForceKiteThisDecision || (lActor.Definition.EnemyAiKiteAfterSuccessfulAction && lHasActed);

            EnemyAiMovementMode lMovementMode = lShouldForceKite
                ? EnemyAiMovementMode.Kite
                : ResolveMovementMode(lSettings.MovementPolicy, lCanFightAtRange, lLowHealth, lThreatened, lHasActed);
            int lPreferredThreatDistance = lSettings.PreferredDistance > 0
                ? lSettings.PreferredDistance
                : lCanFightAtRange ? Math.Min(lBestOffensiveRange, 5) : 1;

            return new EnemyAiProfile(lHostileTeam, lSettings, lMovementMode, lPreferredThreatDistance);
        }

        private static Team ResolveHostileTeam(UnitRuntime pActor)
        {
            if (pActor == null)
                return Team.TeamA;

            return pActor.Team == Team.TeamA ? Team.TeamB : Team.TeamA;
        }

        private static int ResolveBestOffensiveRange(UnitRuntime pActor)
        {
            int lBestRange = 1;
            IReadOnlyList<SkillDefinition> lSkills = pActor?.Skills;
            if (lSkills == null)
                return lBestRange;

            for (int lIndex = 0; lIndex < lSkills.Count; lIndex++)
            {
                SkillDefinition lEffectiveSkill = pActor.ResolveEffectiveSkill(lSkills[lIndex]);
                if (EnemyAiSkillScorer.IsOffensiveSkill(lEffectiveSkill))
                    lBestRange = Math.Max(lBestRange, pActor.GetSkillRangeMax(lEffectiveSkill));
            }

            return lBestRange;
        }

        private static int ResolveNearestThreatDistance(EnemyAiContext pContext, Team pHostileTeam)
        {
            UnitRuntime lActor = pContext?.Actor;
            if (pContext?.BattleService?.Units == null || lActor == null)
                return int.MaxValue;

            int lNearestDistance = int.MaxValue;
            foreach (UnitRuntime lUnit in pContext.BattleService.Units)
            {
                if (lUnit != null && lUnit.IsAlive && lUnit.Team == pHostileTeam)
                    lNearestDistance = Math.Min(lNearestDistance, lActor.Position.ManhattanDistanceTo(lUnit.Position));
            }

            return lNearestDistance;
        }

        private static EnemyAiMovementMode ResolveMovementMode(
            EnemyAiMovementPolicy pPolicy,
            bool pCanFightAtRange,
            bool pLowHealth,
            bool pThreatened,
            bool pHasActed)
        {
            switch (pPolicy)
            {
                case EnemyAiMovementPolicy.Approach:
                    return EnemyAiMovementMode.Approach;

                case EnemyAiMovementPolicy.KeepDistance:
                    return EnemyAiMovementMode.Kite;

                case EnemyAiMovementPolicy.FleeIfThreatened:
                    return pThreatened ? EnemyAiMovementMode.Kite : EnemyAiMovementMode.Approach;

                case EnemyAiMovementPolicy.KiteAfterActing:
                    return pHasActed ? EnemyAiMovementMode.Kite : EnemyAiMovementMode.Approach;

                default:
                    return pCanFightAtRange && (pLowHealth || pThreatened)
                        ? EnemyAiMovementMode.Kite
                        : EnemyAiMovementMode.Approach;
            }
        }
    }
}
