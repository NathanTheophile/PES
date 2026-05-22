#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using PurrNet.UTP;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TacticalPort.Networking
{
    internal sealed class PurrNetRelayPreparationController : IDisposable
    {
        #region _____________________________/ VALUES

        private CancellationTokenSource _PreparationCancellation;
        private int _PreparationVersion;
        private bool _IsDisposed;
        private bool _IsPreparing;
        private string _PreparedRelayJoinCode = string.Empty;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool IsPreparing => _IsPreparing;
        public string PreparedRelayJoinCode => _PreparedRelayJoinCode;

        #endregion

        #region _____________________________| PREPARATION

        public void ResetPreparedJoinCode() => _PreparedRelayJoinCode = string.Empty;

        public void BeginPrepareClient(
            string pRelayJoinCode,
            UTPTransport pTransport,
            Action<bool, string> pCompleted,
            Action<string> pLog,
            Action<string> pWarning)
        {
            Cancel();
            ResetPreparedJoinCode();

            if (_IsDisposed)
            {
                pWarning?.Invoke("Cannot prepare Relay client because the preparation controller is disposed.");
                pCompleted?.Invoke(false, string.Empty);
                return;
            }

            if (string.IsNullOrWhiteSpace(pRelayJoinCode))
            {
                pWarning?.Invoke("Cannot prepare Relay client without a Relay join code.");
                pCompleted?.Invoke(false, string.Empty);
                return;
            }

            if (pTransport == null)
            {
                pWarning?.Invoke("Cannot prepare Relay client without a UTP transport.");
                pCompleted?.Invoke(false, string.Empty);
                return;
            }

            _PreparationCancellation = new CancellationTokenSource();
            _PreparationVersion++;
            _IsPreparing = true;

            _ = PrepareClientAsync(
                pRelayJoinCode,
                pTransport,
                _PreparationVersion,
                _PreparationCancellation.Token,
                pCompleted,
                pLog,
                pWarning);
        }

        public void Cancel()
        {
            _PreparationVersion++;
            _PreparationCancellation?.Cancel();
            _PreparationCancellation?.Dispose();
            _PreparationCancellation = null;
            _IsPreparing = false;
        }

        public void Dispose()
        {
            if (_IsDisposed)
                return;

            Cancel();
            _IsDisposed = true;
        }

        #endregion

        #region _____________________________| HELPERS

        private async Task PrepareClientAsync(
            string pRelayJoinCode,
            UTPTransport pTransport,
            int pVersion,
            CancellationToken pCancellationToken,
            Action<bool, string> pCompleted,
            Action<string> pLog,
            Action<string> pWarning)
        {
            pLog?.Invoke($"Preparing Relay client. RelayCode={pRelayJoinCode}");

            bool lIsReady = false;
            try
            {
                lIsReady = await pTransport.InitializeRelayClient(pRelayJoinCode);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception pException)
            {
                if (!pCancellationToken.IsCancellationRequested)
                    pWarning?.Invoke($"Relay client preparation failed. Reason={pException.Message}");
            }

            if (!IsCurrentPreparation(pVersion, pCancellationToken))
                return;

            _IsPreparing = false;
            DisposePreparationIfCurrent(pVersion);

            if (!lIsReady)
            {
                ResetPreparedJoinCode();
                pCompleted?.Invoke(false, string.Empty);
                return;
            }

            _PreparedRelayJoinCode = pRelayJoinCode;
            pCompleted?.Invoke(true, pRelayJoinCode);
        }

        private bool IsCurrentPreparation(int pVersion, CancellationToken pCancellationToken) =>
            !_IsDisposed && !pCancellationToken.IsCancellationRequested && pVersion == _PreparationVersion;

        private void DisposePreparationIfCurrent(int pVersion)
        {
            if (pVersion != _PreparationVersion)
                return;

            _PreparationCancellation?.Dispose();
            _PreparationCancellation = null;
        }

        #endregion
    }
}
