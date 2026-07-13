#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class UgsPlayerIdentityService : MonoBehaviour, IPlayerIdentityService
    {
        #region _____________________________/ VALUES

        [SerializeField] private bool _ClearSessionBeforeSignIn;

        #endregion

        #region _____________________________| SIGN IN

        public Task<PlayerIdentity> SignInAsync(CancellationToken pCancellationToken) =>
            UgsAuthentication.SignInAnonymouslyAsync(_ClearSessionBeforeSignIn, this, pCancellationToken);

        public void PreserveAuthenticationSession() => _ClearSessionBeforeSignIn = false;

        #endregion
    }

    internal static class UgsAuthentication
    {
        public static async Task<PlayerIdentity> SignInAnonymouslyAsync(bool pClearSessionBeforeSignIn, Object pContext, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();

            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            pCancellationToken.ThrowIfCancellationRequested();

            if (pClearSessionBeforeSignIn)
            {
                AuthenticationService.Instance.SignOut(true);
                Debug.Log("[UGS] Authentication session cleared before anonymous sign-in.", pContext);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            pCancellationToken.ThrowIfCancellationRequested();

            string lPlayerId = AuthenticationService.Instance.PlayerId;
            string lDisplayName = string.IsNullOrWhiteSpace(AuthenticationService.Instance.PlayerName)
                ? lPlayerId
                : AuthenticationService.Instance.PlayerName;

            return new PlayerIdentity(lPlayerId, lDisplayName);
        }
    }
}
