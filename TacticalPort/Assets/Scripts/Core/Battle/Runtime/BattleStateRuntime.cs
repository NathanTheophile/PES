#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using TacticalPort.Data;

namespace TacticalPort.Core
{
    public sealed class BattleStateRuntime
    {
        public BattleStateRuntime(string pKey, StateDefinition pDefinition, int pStacks, int pRemainingTurns, bool pIsPersistent)
        {
            Key = pKey ?? string.Empty;
            Reconfigure(pDefinition, pStacks, pRemainingTurns, pIsPersistent);
        }

        public string Key { get; }
        public StateDefinition Definition { get; private set; }
        public int Stacks { get; private set; }
        public int RemainingTurns { get; private set; }
        public bool IsPersistent { get; private set; }
        public bool HasDuration => RemainingTurns > 0;

        internal void Reconfigure(StateDefinition pDefinition, int pStacks, int pRemainingTurns, bool pIsPersistent)
        {
            if (pDefinition == null)
                throw new ArgumentNullException(nameof(pDefinition));

            Definition = pDefinition;
            Stacks = Math.Max(1, Math.Min(pDefinition.MaxStacks, pStacks));
            RemainingTurns = Math.Max(0, pRemainingTurns);
            IsPersistent = pIsPersistent;
        }

        internal void AddStacks(int pStacks)
        {
            if (pStacks <= 0 || Definition == null)
                return;

            Stacks = Math.Max(1, Math.Min(Definition.MaxStacks, Stacks + pStacks));
        }

        internal void SetRemainingTurns(int pRemainingTurns) => RemainingTurns = Math.Max(0, pRemainingTurns);

        internal bool TickTurnEnd()
        {
            if (IsPersistent || RemainingTurns <= 0)
                return false;

            RemainingTurns = Math.Max(0, RemainingTurns - 1);
            return RemainingTurns == 0;
        }
    }
}
