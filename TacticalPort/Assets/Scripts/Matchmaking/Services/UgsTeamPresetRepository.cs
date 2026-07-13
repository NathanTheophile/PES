#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Matchmaking / UGS
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TacticalPort.State;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class UgsTeamPresetRepository : ITeamPresetRepository
    {
        private const string ModuleName = "TeamPresets";
        private const string LoadEndpointName = "LoadTeamPresets";
        private const string SaveEndpointName = "SaveTeamPresets";

        public async Task<TeamPresetRepositorySnapshot> LoadAsync(CancellationToken pCancellationToken = default)
        {
            EnsureAuthenticated();
            pCancellationToken.ThrowIfCancellationRequested();

            string lJson = await CloudCodeService.Instance.CallModuleEndpointAsync<string>(
                ModuleName,
                LoadEndpointName,
                new Dictionary<string, object>());

            pCancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(lJson))
                return new TeamPresetRepositorySnapshot();

            try
            {
                return JsonUtility.FromJson<TeamPresetRepositorySnapshot>(lJson)
                       ?? new TeamPresetRepositorySnapshot();
            }
            catch (Exception lException)
            {
                throw new InvalidOperationException("Cloud Save returned an invalid team preset snapshot.", lException);
            }
        }

        public async Task SaveAsync(TeamPresetRepositorySnapshot pSnapshot, CancellationToken pCancellationToken = default)
        {
            EnsureAuthenticated();
            pCancellationToken.ThrowIfCancellationRequested();

            await CloudCodeService.Instance.CallModuleEndpointAsync(
                ModuleName,
                SaveEndpointName,
                new Dictionary<string, object>
                {
                    { "snapshotJson", JsonUtility.ToJson(pSnapshot ?? new TeamPresetRepositorySnapshot()) }
                });

            pCancellationToken.ThrowIfCancellationRequested();
        }

        private static void EnsureAuthenticated()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                throw new InvalidOperationException("Cloud team presets require an authenticated UGS player.");
        }
    }
}
