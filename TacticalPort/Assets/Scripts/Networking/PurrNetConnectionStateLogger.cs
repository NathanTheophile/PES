#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using PurrNet;
using PurrNet.Transports;
using System;
using UnityEngine;

namespace TacticalPort.Networking
{
    internal sealed class PurrNetConnectionStateLogger
    {
        #region _____________________________/ VALUES

        private readonly UnityEngine.Object _Owner;
        private readonly Func<bool> _CanLog;
        private readonly Func<string> _DescribeConnectionState;
        private ConnectionState _LastLoggedClientState = ConnectionState.Disconnected;
        private ConnectionState _LastLoggedServerState = ConnectionState.Disconnected;

        #endregion

        #region _____________________________| INIT

        public PurrNetConnectionStateLogger(UnityEngine.Object pOwner, Func<bool> pCanLog, Func<string> pDescribeConnectionState)
        {
            _Owner = pOwner;
            _CanLog = pCanLog;
            _DescribeConnectionState = pDescribeConnectionState;
        }

        #endregion

        #region _____________________________| SUBSCRIPTIONS

        public void Subscribe(NetworkManager pNetworkManager)
        {
            if (pNetworkManager == null)
                return;

            pNetworkManager.onClientConnectionState -= HandleClientConnectionState;
            pNetworkManager.onServerConnectionState -= HandleServerConnectionState;
            pNetworkManager.onClientConnectionState += HandleClientConnectionState;
            pNetworkManager.onServerConnectionState += HandleServerConnectionState;
        }

        public void Unsubscribe(NetworkManager pNetworkManager)
        {
            if (pNetworkManager == null)
                return;

            pNetworkManager.onClientConnectionState -= HandleClientConnectionState;
            pNetworkManager.onServerConnectionState -= HandleServerConnectionState;
        }

        #endregion

        #region _____________________________| CALLBACKS

        private void HandleClientConnectionState(ConnectionState pState)
        {
            if (!CanLog(pState, _LastLoggedClientState))
                return;

            _LastLoggedClientState = pState;
            Debug.Log($"[PurrNet Match Connector] Client connection state: {pState}. {_DescribeConnectionState?.Invoke()}", _Owner);
        }

        private void HandleServerConnectionState(ConnectionState pState)
        {
            if (!CanLog(pState, _LastLoggedServerState))
                return;

            _LastLoggedServerState = pState;
            Debug.Log($"[PurrNet Match Connector] Server connection state: {pState}. {_DescribeConnectionState?.Invoke()}", _Owner);
        }

        private bool CanLog(ConnectionState pState, ConnectionState pLastState) =>
            (_CanLog?.Invoke() ?? false) && pLastState != pState;

        #endregion
    }
}
