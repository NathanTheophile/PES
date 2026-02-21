using PES.Infrastructure.Serialization;

namespace PES.Presentation.Flow
{
    public enum ProductFlowState
    {
        Boot = 0,
        Menu = 1,
        Battle = 2
    }

    /// <summary>
    /// Orchestrateur minimal du flow produit boot/menu/battle avec persistance versionnée.
    /// </summary>
    public sealed class ProductFlowController
    {
        private readonly ISessionSaveStore _saveStore;

        public ProductFlowController(ISessionSaveStore saveStore)
        {
            _saveStore = saveStore;
            CurrentState = ProductFlowState.Boot;
        }

        public ProductFlowState CurrentState { get; private set; }

        public int LastBattleSeed { get; private set; }

        public bool HasBattleToResume { get; private set; }

        public void Boot()
        {
            CurrentState = ProductFlowState.Boot;

            if (_saveStore.TryLoad(out var saveData))
            {
                LastBattleSeed = saveData.FlowSnapshot.LastBattleSeed;
                HasBattleToResume = saveData.FlowSnapshot.HasBattleToResume;
            }
            else
            {
                LastBattleSeed = 0;
                HasBattleToResume = false;
            }

            CurrentState = ProductFlowState.Menu;
        }

        public void StartNewBattle(int seed)
        {
            LastBattleSeed = seed;
            HasBattleToResume = true;
            CurrentState = ProductFlowState.Battle;
            PersistBattleState();
        }

        public bool TryResumeBattle()
        {
            if (!HasBattleToResume)
            {
                return false;
            }

            CurrentState = ProductFlowState.Battle;
            PersistBattleState();
            return true;
        }

        public void ReturnToMenu()
        {
            CurrentState = ProductFlowState.Menu;
            PersistMenuState();
        }

        public void CompleteBattleAndReturnToMenu()
        {
            CurrentState = ProductFlowState.Menu;
            HasBattleToResume = false;
            PersistMenuState();
        }

        private void PersistBattleState()
        {
            _saveStore.Save(new SessionSaveData(ProductFlowSnapshot.Battle(LastBattleSeed)));
        }

        private void PersistMenuState()
        {
            var snapshot = HasBattleToResume
                ? ProductFlowSnapshot.Battle(LastBattleSeed)
                : ProductFlowSnapshot.Menu();

            _saveStore.Save(new SessionSaveData(snapshot));
        }
    }
}
