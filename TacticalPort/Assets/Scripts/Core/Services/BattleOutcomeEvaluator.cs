#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    internal static class BattleOutcomeEvaluator
    {
        public static BattleOutcome Evaluate(IEnumerable<UnitRuntime> pUnits)
        {
            bool lAnyPlayersAlive = false;
            bool lAnyEnemiesAlive = false;

            if (pUnits != null)
            {
                foreach (UnitRuntime lUnit in pUnits)
                {
                    if (lUnit == null || !lUnit.IsAlive)
                        continue;

                    if (lUnit.Team == Team.Player)
                        lAnyPlayersAlive = true;
                    else if (lUnit.Team == Team.Enemy)
                        lAnyEnemiesAlive = true;

                    if (lAnyPlayersAlive && lAnyEnemiesAlive)
                        return BattleOutcome.None;
                }
            }

            if (!lAnyPlayersAlive && !lAnyEnemiesAlive)
                return BattleOutcome.Draw;

            return !lAnyEnemiesAlive ? BattleOutcome.PlayerVictory : BattleOutcome.EnemyVictory;
        }
    }
}
