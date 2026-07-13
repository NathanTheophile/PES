#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  State
#endregion

using System.Threading;
using System.Threading.Tasks;

namespace TacticalPort.State
{
    public interface ITeamPresetRepository
    {
        Task<TeamPresetRepositorySnapshot> LoadAsync(CancellationToken pCancellationToken = default);
        Task SaveAsync(TeamPresetRepositorySnapshot pSnapshot, CancellationToken pCancellationToken = default);
    }
}
