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
    internal static class CombatMatchManifestImporter
    {
        #region _____________________________| IMPORT

        public static bool TryImport(
            MatchManifest pCurrentManifest,
            string pMatchId,
            string pManifestJson,
            bool pAcceptClientManifestWhenServerHasNoManifest,
            out MatchManifest pResolvedManifest,
            out string pFailure)
        {
            pResolvedManifest = pCurrentManifest;
            pFailure = string.Empty;

            if (string.IsNullOrWhiteSpace(pMatchId))
            {
                pFailure = "Match handshake failed: missing match id.";
                return false;
            }

            if (pCurrentManifest != null
                && !string.IsNullOrWhiteSpace(pCurrentManifest.MatchId)
                && pCurrentManifest.MatchId != pMatchId)
            {
                pFailure = $"Match handshake failed: expected match '{pCurrentManifest.MatchId}', got '{pMatchId}'.";
                return false;
            }

            if (!TryParseManifest(pMatchId, pManifestJson, out MatchManifest lIncomingManifest, out pFailure))
                return false;

            if (pCurrentManifest != null && pCurrentManifest.HasPlayerAssignments)
            {
                if (!pCurrentManifest.HasSamePlayerAssignments(lIncomingManifest))
                {
                    if (!TryMergeCompatibleAssignments(pCurrentManifest, lIncomingManifest, out pFailure))
                        return false;

                    return true;
                }

                pCurrentManifest.MergeCompositions(lIncomingManifest);
                return true;
            }

            if (!pAcceptClientManifestWhenServerHasNoManifest)
            {
                pFailure = "Match handshake failed: server has no authoritative match manifest.";
                return false;
            }

            pResolvedManifest = lIncomingManifest;
            return true;
        }

        private static bool TryParseManifest(string pMatchId, string pManifestJson, out MatchManifest pManifest, out string pFailure)
        {
            pManifest = null;
            pFailure = string.Empty;

            try
            {
                if (!string.IsNullOrWhiteSpace(pManifestJson))
                    pManifest = JsonUtility.FromJson<MatchManifest>(pManifestJson);
            }
            catch (System.Exception pException)
            {
                pFailure = $"Match handshake failed: invalid match manifest. {pException.Message}";
                return false;
            }

            if (pManifest == null)
            {
                pFailure = "Match handshake failed: missing match manifest.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(pManifest.MatchId))
                pManifest.MatchId = pMatchId;

            if (pManifest.MatchId != pMatchId)
            {
                pFailure = $"Match handshake failed: manifest match '{pManifest.MatchId}' does not match '{pMatchId}'.";
                return false;
            }

            if (!pManifest.HasPlayerAssignments)
            {
                pFailure = "Match handshake failed: manifest has no player slot assignments.";
                return false;
            }

            if (pManifest.TryValidatePlayerAssignments(out pFailure))
                return true;

            pFailure = $"Match handshake failed: {pFailure}";
            return false;
        }

        private static bool TryMergeCompatibleAssignments(MatchManifest pCurrentManifest, MatchManifest pIncomingManifest, out string pFailure)
        {
            pFailure = string.Empty;

            if (pCurrentManifest == null || pIncomingManifest == null || pCurrentManifest.MatchId != pIncomingManifest.MatchId)
            {
                pFailure = "Match handshake failed: client manifest assignments do not match the server manifest.";
                return false;
            }

            if (!TryMergeAssignment(pCurrentManifest, pIncomingManifest, MatchPlayerSlot.TeamA, out pFailure))
                return false;

            if (!TryMergeAssignment(pCurrentManifest, pIncomingManifest, MatchPlayerSlot.TeamB, out pFailure))
                return false;

            return true;
        }

        private static bool TryMergeAssignment(MatchManifest pCurrentManifest, MatchManifest pIncomingManifest, MatchPlayerSlot pSlot, out string pFailure)
        {
            pFailure = string.Empty;

            if (!pCurrentManifest.TryGetAssignment(pSlot, out MatchPlayerAssignment lCurrent)
                || !pIncomingManifest.TryGetAssignment(pSlot, out MatchPlayerAssignment lIncoming))
            {
                pFailure = $"Match handshake failed: missing {pSlot} assignment.";
                return false;
            }

            bool lCurrentPending = IsPendingPlayerId(lCurrent.PlayerId);
            bool lIncomingPending = IsPendingPlayerId(lIncoming.PlayerId);

            if (!lCurrentPending && !lIncomingPending && lCurrent.PlayerId != lIncoming.PlayerId)
            {
                pFailure = $"Match handshake failed: conflicting {pSlot} players '{lCurrent.PlayerId}' and '{lIncoming.PlayerId}'.";
                return false;
            }

            if (lCurrentPending && !lIncomingPending)
                pCurrentManifest.SetAssignment(pSlot, lIncoming.PlayerId, lIncoming.TeamPresetId, lIncoming.UnitIds, lIncoming.UnitBuilds);
            else if (!lIncomingPending && lIncoming.UnitIds != null && lIncoming.UnitIds.Count > 0)
                pCurrentManifest.SetAssignment(pSlot, lCurrent.PlayerId, lIncoming.TeamPresetId, lIncoming.UnitIds, lIncoming.UnitBuilds);

            return true;
        }

        private static bool IsPendingPlayerId(string pPlayerId) =>
            string.IsNullOrWhiteSpace(pPlayerId) || pPlayerId.Contains("-pending");

        #endregion
    }
}
