#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  State
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TacticalPort.State
{
    public sealed class CachedTeamPresetRepository : ITeamPresetRepository
    {
        private readonly ITeamPresetRepository _Primary;
        private readonly ITeamPresetRepository _Cache;

        public CachedTeamPresetRepository(ITeamPresetRepository pPrimary, ITeamPresetRepository pCache)
        {
            _Primary = pPrimary ?? throw new ArgumentNullException(nameof(pPrimary));
            _Cache = pCache ?? throw new ArgumentNullException(nameof(pCache));
        }

        public async Task<TeamPresetRepositorySnapshot> LoadAsync(CancellationToken pCancellationToken = default)
        {
            TeamPresetRepositorySnapshot lPrimarySnapshot = null;
            try
            {
                lPrimarySnapshot = await _Primary.LoadAsync(pCancellationToken);
            }
            catch (Exception lException) when (lException is not OperationCanceledException)
            {
                Debug.LogWarning($"Cloud team presets are unavailable. Loading the local cache instead. {lException.Message}");
            }

            TeamPresetRepositorySnapshot lCachedSnapshot = await _Cache.LoadAsync(pCancellationToken)
                ?? new TeamPresetRepositorySnapshot();
            bool lHasPrimary = HasConfiguredTeam(lPrimarySnapshot);
            bool lHasCache = HasConfiguredTeam(lCachedSnapshot);

            if (lHasPrimary && (!lHasCache || lPrimarySnapshot.UpdatedAtUnixMilliseconds >= lCachedSnapshot.UpdatedAtUnixMilliseconds))
            {
                await _Cache.SaveAsync(lPrimarySnapshot.Clone(), pCancellationToken);
                return lPrimarySnapshot;
            }

            if (lHasCache)
                await TryMigrateCacheToPrimaryAsync(lCachedSnapshot.Clone(), pCancellationToken);
            return lCachedSnapshot;
        }

        public async Task SaveAsync(TeamPresetRepositorySnapshot pSnapshot, CancellationToken pCancellationToken = default)
        {
            TeamPresetRepositorySnapshot lSnapshot = pSnapshot ?? new TeamPresetRepositorySnapshot();
            await _Cache.SaveAsync(lSnapshot.Clone(), pCancellationToken);
            await _Primary.SaveAsync(lSnapshot, pCancellationToken);
        }

        private async Task TryMigrateCacheToPrimaryAsync(TeamPresetRepositorySnapshot pSnapshot, CancellationToken pCancellationToken)
        {
            try
            {
                await _Primary.SaveAsync(pSnapshot, pCancellationToken);
            }
            catch (Exception lException) when (lException is not OperationCanceledException)
            {
                Debug.LogWarning($"Local team presets could not be migrated to Cloud Save yet. {lException.Message}");
            }
        }

        private static bool HasConfiguredTeam(TeamPresetRepositorySnapshot pSnapshot)
        {
            if (pSnapshot?.Presets == null)
                return false;

            for (int lPresetIndex = 0; lPresetIndex < pSnapshot.Presets.Count; lPresetIndex++)
            {
                TeamPreset lPreset = pSnapshot.Presets[lPresetIndex];
                if (lPreset?.Slots == null)
                    continue;

                for (int lSlotIndex = 0; lSlotIndex < lPreset.Slots.Count; lSlotIndex++)
                {
                    if (!string.IsNullOrWhiteSpace(lPreset.Slots[lSlotIndex]?.UnitId))
                        return true;
                }
            }

            return false;
        }
    }
}
