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
    public sealed class PlayerPrefsTeamPresetRepository : ITeamPresetRepository
    {
        public const string SaveKey = "TacticalPort.TeamPresets.v1";

        public Task<TeamPresetRepositorySnapshot> LoadAsync(CancellationToken pCancellationToken = default)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            string lJson = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrWhiteSpace(lJson))
                return Task.FromResult(new TeamPresetRepositorySnapshot());

            try
            {
                TeamPresetRepositorySnapshot lSnapshot = JsonUtility.FromJson<TeamPresetRepositorySnapshot>(lJson);
                return Task.FromResult(lSnapshot ?? new TeamPresetRepositorySnapshot());
            }
            catch (Exception lException)
            {
                Debug.LogError($"Failed to read local team presets. The invalid save was ignored. {lException.Message}");
                return Task.FromResult(new TeamPresetRepositorySnapshot());
            }
        }

        public Task SaveAsync(TeamPresetRepositorySnapshot pSnapshot, CancellationToken pCancellationToken = default)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            string lJson = JsonUtility.ToJson(pSnapshot ?? new TeamPresetRepositorySnapshot());
            PlayerPrefs.SetString(SaveKey, lJson);
            PlayerPrefs.Save();
            return Task.CompletedTask;
        }
    }
}
