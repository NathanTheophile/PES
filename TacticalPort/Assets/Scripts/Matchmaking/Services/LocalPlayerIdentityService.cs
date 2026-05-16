#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Matchmaking
#endregion

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class LocalPlayerIdentityService : MonoBehaviour, IPlayerIdentityService
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _PlayerId = "local-player";
        [SerializeField] private string _DisplayName = "Local Player";

        #endregion

        #region _____________________________| SIGN IN

        public Task<PlayerIdentity> SignInAsync(CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new PlayerIdentity(_PlayerId, _DisplayName));
        }

        #endregion
    }
}
