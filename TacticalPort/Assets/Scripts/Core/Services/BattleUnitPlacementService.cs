#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    internal sealed class BattleUnitPlacementService
    {
        #region _____________________________/ VALUES

        private readonly IGridService _GridService;
        private readonly ITurnSystem _TurnSystem;
        private readonly IDictionary<UnitId, UnitRuntime> _UnitsById;
        private int _NextUnitIdValue = 1;

        #endregion

        #region _____________________________| INIT

        public BattleUnitPlacementService(
            IGridService pGridService,
            ITurnSystem pTurnSystem,
            IDictionary<UnitId, UnitRuntime> pUnitsById)
        {
            _GridService = pGridService ?? throw new ArgumentNullException(nameof(pGridService));
            _TurnSystem = pTurnSystem ?? throw new ArgumentNullException(nameof(pTurnSystem));
            _UnitsById = pUnitsById ?? throw new ArgumentNullException(nameof(pUnitsById));
        }

        #endregion

        #region _____________________________| SETUP

        public void ResetNextUnitId(int pFirstUnitIdValue) => _NextUnitIdValue = Math.Max(1, pFirstUnitIdValue);

        public void TrackExistingUnitId(UnitId pUnitId)
        {
            if (pUnitId.IsValid)
                _NextUnitIdValue = Math.Max(_NextUnitIdValue, pUnitId.Value + 1);
        }

        public bool TryPlaceInitialUnit(UnitRuntime pUnit)
        {
            if (pUnit == null)
                return false;

            _GridService.RegisterUnitFootprint(pUnit.Id, pUnit.OccupiedCellOffsets);
            return _GridService.TryPlaceUnit(pUnit.Id, pUnit.Position);
        }

        #endregion

        #region _____________________________| PLACEMENT

        public bool TryRelocateUnit(UnitRuntime pUnit, GridCoord pDestination)
        {
            if (pUnit == null || !pUnit.IsAlive)
                return false;

            GridCoord lOrigin = pUnit.Position;
            if (!_GridService.TryMoveUnit(pUnit.Id, pDestination))
                return false;

            pUnit.SetPosition(pDestination);
            pUnit.FaceDirection(new GridCoord(pDestination.X - lOrigin.X, pDestination.Y - lOrigin.Y));
            return true;
        }

        public bool TrySwapUnits(UnitRuntime pFirstUnit, UnitRuntime pSecondUnit)
        {
            if (pFirstUnit == null || pSecondUnit == null || !pFirstUnit.IsAlive || !pSecondUnit.IsAlive)
                return false;

            GridCoord lFirstPosition = pFirstUnit.Position;
            GridCoord lSecondPosition = pSecondUnit.Position;

            if (!_GridService.RemoveUnit(pFirstUnit.Id))
                return false;

            if (!_GridService.RemoveUnit(pSecondUnit.Id))
            {
                _GridService.TryPlaceUnit(pFirstUnit.Id, lFirstPosition);
                return false;
            }

            bool lPlacedSecond = _GridService.TryPlaceUnit(pSecondUnit.Id, lFirstPosition);
            bool lPlacedFirst = lPlacedSecond && _GridService.TryPlaceUnit(pFirstUnit.Id, lSecondPosition);
            if (!lPlacedFirst)
            {
                _GridService.RemoveUnit(pFirstUnit.Id);
                _GridService.RemoveUnit(pSecondUnit.Id);
                _GridService.TryPlaceUnit(pFirstUnit.Id, lFirstPosition);
                _GridService.TryPlaceUnit(pSecondUnit.Id, lSecondPosition);
                return false;
            }

            pFirstUnit.SetPosition(lSecondPosition);
            pSecondUnit.SetPosition(lFirstPosition);
            pFirstUnit.FaceTowards(lSecondPosition);
            pSecondUnit.FaceTowards(lFirstPosition);
            return true;
        }

        public UnitRuntime TrySummonUnit(
            UnitDefinition pDefinition,
            SkillSummonTeamRule pTeamRule,
            UnitRuntime pSummoner,
            GridCoord pDestination)
        {
            if (pDefinition == null || !_GridService.IsInside(pDestination))
                return null;

            Team? lTeamOverride = ResolveSummonTeamOverride(pDefinition, pTeamRule, pSummoner);
            Team? lRuntimeTeamOverride = lTeamOverride.HasValue && lTeamOverride.Value != pDefinition.Team
                ? lTeamOverride
                : null;

            UnitId lUnitId = new UnitId(_NextUnitIdValue++);
            UnitRuntime lRuntime = new UnitRuntime(lUnitId, pDefinition, pDestination, lRuntimeTeamOverride);
            _GridService.RegisterUnitFootprint(lUnitId, lRuntime.OccupiedCellOffsets);

            if (!_GridService.TryPlaceUnit(lUnitId, pDestination))
                return null;

            _UnitsById[lUnitId] = lRuntime;
            _TurnSystem.AddUnit(lRuntime, pSummoner != null ? pSummoner.Id : UnitId.None);
            return lRuntime;
        }

        private static Team? ResolveSummonTeamOverride(UnitDefinition pDefinition, SkillSummonTeamRule pTeamRule, UnitRuntime pSummoner)
        {
            switch (pTeamRule)
            {
                case SkillSummonTeamRule.Caster:
                    return pSummoner != null ? pSummoner.Team : pDefinition.Team;

                case SkillSummonTeamRule.EnemyOfCaster:
                    if (pSummoner == null)
                        return pDefinition.Team;

                    return pSummoner.Team == Team.Player ? Team.Enemy : Team.Player;

                default:
                    return pDefinition != null ? pDefinition.Team : Team.Neutral;
            }
        }

        #endregion
    }
}
