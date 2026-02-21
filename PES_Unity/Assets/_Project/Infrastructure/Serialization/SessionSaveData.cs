namespace PES.Infrastructure.Serialization
{
    /// <summary>
    /// Contrat minimal de sauvegarde de session produit (boot/menu/battle).
    /// Versionné pour supporter migrations futures.
    /// </summary>
    public sealed class SessionSaveData
    {
        public const int CurrentContractVersion = 1;

        public SessionSaveData(
            ProductFlowSnapshot flowSnapshot,
            int contractVersion = CurrentContractVersion)
        {
            ContractVersion = contractVersion > 0 ? contractVersion : CurrentContractVersion;
            FlowSnapshot = flowSnapshot ?? ProductFlowSnapshot.Menu();
        }

        public int ContractVersion { get; }

        public ProductFlowSnapshot FlowSnapshot { get; }
    }

    public sealed class ProductFlowSnapshot
    {
        private ProductFlowSnapshot(string lastKnownState, int lastBattleSeed, bool hasBattleToResume)
        {
            LastKnownState = string.IsNullOrWhiteSpace(lastKnownState) ? "Menu" : lastKnownState;
            LastBattleSeed = lastBattleSeed;
            HasBattleToResume = hasBattleToResume;
        }

        public string LastKnownState { get; }

        public int LastBattleSeed { get; }

        public bool HasBattleToResume { get; }

        public static ProductFlowSnapshot Menu()
        {
            return new ProductFlowSnapshot("Menu", 0, false);
        }

        public static ProductFlowSnapshot Battle(int seed)
        {
            return new ProductFlowSnapshot("Battle", seed, true);
        }
    }
}
