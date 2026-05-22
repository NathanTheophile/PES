#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using TacticalPort.Matchmaking;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Networking
{
    internal static class PurrNetMatchManifestMessageParser
    {
        #region _____________________________| PARSE

        public static bool TryParseCustomJoinManifest(PurrNetCustomMatchJoinRequestMessage pMessage, out MatchManifest pManifest, out string pFailure)
        {
            pManifest = null;
            pFailure = string.Empty;

            if (string.IsNullOrWhiteSpace(pMessage.PlayerId) || string.IsNullOrWhiteSpace(pMessage.MatchId))
            {
                pFailure = "Custom join request is missing player id or match id.";
                return false;
            }

            if (!TryParseManifestJson(pMessage.ManifestJson, "Custom join request has an invalid manifest.", out pManifest, out pFailure))
                return false;

            if (pManifest == null || pManifest.MatchId != pMessage.MatchId || !pManifest.TryValidatePlayerAssignments(out pFailure))
            {
                pFailure = string.IsNullOrWhiteSpace(pFailure) ? "Custom join request has an invalid manifest." : pFailure;
                return false;
            }

            return true;
        }

        public static bool TryAcceptCustomServerManifest(
            PurrNetMatchManifestMessage pMessage,
            string pExpectedMatchId,
            string pLocalPlayerId,
            out MatchManifest pManifest,
            out string pFailure)
        {
            pManifest = null;
            pFailure = string.Empty;

            if (string.IsNullOrWhiteSpace(pExpectedMatchId))
            {
                pFailure = "Invalid custom match manifest: missing expected match id.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(pLocalPlayerId))
            {
                pFailure = "Invalid custom match manifest: missing local player id.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(pMessage.MatchId) && pMessage.MatchId != pExpectedMatchId)
            {
                pFailure = $"Invalid custom match manifest: expected match '{pExpectedMatchId}', got '{pMessage.MatchId}'.";
                return false;
            }

            if (!TryParseManifestJson(pMessage.ManifestJson, "Invalid manifest.", out pManifest, out pFailure))
                return false;

            if (pManifest == null || pManifest.MatchId != pExpectedMatchId || !pManifest.TryValidatePlayerAssignments(out pFailure))
            {
                pFailure = string.IsNullOrWhiteSpace(pFailure) ? "Invalid custom match manifest." : pFailure;
                return false;
            }

            if (!pManifest.TryGetSlot(pLocalPlayerId, out MatchPlayerSlot lSlot) || lSlot != MatchPlayerSlot.TeamB)
            {
                pFailure = $"Custom match manifest does not assign local player '{pLocalPlayerId}' to TeamB.";
                return false;
            }

            return true;
        }

        private static bool TryParseManifestJson(string pManifestJson, string pFailurePrefix, out MatchManifest pManifest, out string pFailure)
        {
            pManifest = null;
            pFailure = string.Empty;

            try
            {
                pManifest = string.IsNullOrWhiteSpace(pManifestJson) ? null : JsonUtility.FromJson<MatchManifest>(pManifestJson);
                return true;
            }
            catch (System.Exception pException)
            {
                pFailure = $"{pFailurePrefix} {pException.Message}";
                return false;
            }
        }

        #endregion
    }
}
