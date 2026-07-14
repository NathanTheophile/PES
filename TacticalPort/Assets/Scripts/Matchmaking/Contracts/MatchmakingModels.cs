#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Shared;

namespace TacticalPort.Matchmaking
{
    [Serializable]
    public readonly struct PlayerIdentity
    {
        public readonly string PlayerId;
        public readonly string DisplayName;

        public PlayerIdentity(string pPlayerId, string pDisplayName)
        {
            PlayerId = pPlayerId;
            DisplayName = pDisplayName;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId);
    }

    [Serializable]
    public sealed class MatchUnitStatAllocations
    {
        public int Health;
        public int Energy;
        public int Mobility;
        public int MeleeDamage;
        public int MeleeResistance;
        public int RangedDamage;
        public int RangedResistance;
        public int Velocity;
    }

    [Serializable]
    public sealed class MatchUnitBuild
    {
        public string UnitId = string.Empty;
        public string PassiveId = string.Empty;
        public List<string> SkillIds = new List<string>();
        public MatchUnitStatAllocations StatAllocations = new MatchUnitStatAllocations();
    }

    [Serializable]
    public sealed class MatchPlayerAssignment
    {
        public string PlayerId = string.Empty;
        public MatchPlayerSlot Slot = MatchPlayerSlot.None;
        public string TeamPresetId = string.Empty;
        public List<string> UnitIds = new List<string>();
        public List<MatchUnitBuild> UnitBuilds = new List<MatchUnitBuild>();
    }

    [Serializable]
    public sealed class MatchManifest
    {
        public string MatchId = string.Empty;
        public string MapId = string.Empty;
        public MatchPlayerSlot PerfectVelocityTieStartingSlot = MatchPlayerSlot.None;
        public List<MatchPlayerAssignment> Players = new List<MatchPlayerAssignment>();

        public bool HasPlayerAssignments => Players != null && Players.Count > 0;

        public MatchPlayerSlot ResolvePerfectVelocityTieStartingSlot()
        {
            if (PerfectVelocityTieStartingSlot is MatchPlayerSlot.TeamA or MatchPlayerSlot.TeamB)
                return PerfectVelocityTieStartingSlot;

            uint lHash = 2166136261;
            string lMatchId = MatchId ?? string.Empty;
            for (int lIndex = 0; lIndex < lMatchId.Length; lIndex++)
                lHash = (lHash ^ lMatchId[lIndex]) * 16777619;

            return (lHash & 1) == 0 ? MatchPlayerSlot.TeamA : MatchPlayerSlot.TeamB;
        }

        public void EnsurePerfectVelocityTieStartingSlot() =>
            PerfectVelocityTieStartingSlot = ResolvePerfectVelocityTieStartingSlot();

        public bool HasAllPlayerCompositions
        {
            get
            {
                if (Players == null || Players.Count == 0)
                    return false;

                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer == null || lPlayer.UnitIds == null || lPlayer.UnitIds.Count == 0)
                        return false;
                }

                return true;
            }
        }

        public bool TryGetSlot(string pPlayerId, out MatchPlayerSlot pSlot)
        {
            if (!string.IsNullOrWhiteSpace(pPlayerId) && Players != null)
            {
                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer != null && lPlayer.PlayerId == pPlayerId)
                    {
                        pSlot = lPlayer.Slot;
                        return pSlot != MatchPlayerSlot.None;
                    }
                }
            }

            pSlot = MatchPlayerSlot.None;
            return false;
        }

        public bool TryGetPlayerId(MatchPlayerSlot pSlot, out string pPlayerId)
        {
            if (pSlot != MatchPlayerSlot.None && Players != null)
            {
                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer != null && lPlayer.Slot == pSlot && !string.IsNullOrWhiteSpace(lPlayer.PlayerId))
                    {
                        pPlayerId = lPlayer.PlayerId;
                        return true;
                    }
                }
            }

            pPlayerId = string.Empty;
            return false;
        }

        public bool TryGetAssignment(string pPlayerId, out MatchPlayerAssignment pAssignment)
        {
            pAssignment = FindAssignment(pPlayerId);
            return pAssignment != null;
        }

        public bool TryGetAssignment(MatchPlayerSlot pSlot, out MatchPlayerAssignment pAssignment)
        {
            if (pSlot != MatchPlayerSlot.None && Players != null)
            {
                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer != null && lPlayer.Slot == pSlot)
                    {
                        pAssignment = lPlayer;
                        return true;
                    }
                }
            }

            pAssignment = null;
            return false;
        }

        public bool TryGetUnitIds(MatchPlayerSlot pSlot, out IReadOnlyList<string> pUnitIds)
        {
            if (pSlot != MatchPlayerSlot.None && Players != null)
            {
                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer != null && lPlayer.Slot == pSlot && lPlayer.UnitIds != null && lPlayer.UnitIds.Count > 0)
                    {
                        pUnitIds = lPlayer.UnitIds;
                        return true;
                    }
                }
            }

            pUnitIds = null;
            return false;
        }

        public void SetUnitIds(string pPlayerId, IReadOnlyList<string> pUnitIds)
        {
            MatchPlayerAssignment lAssignment = FindAssignment(pPlayerId);
            if (lAssignment != null)
                CopyUnitIds(pUnitIds, lAssignment);
        }

        public void SetUnitBuilds(string pPlayerId, IReadOnlyList<MatchUnitBuild> pUnitBuilds)
        {
            MatchPlayerAssignment lAssignment = FindAssignment(pPlayerId);
            if (lAssignment != null)
                CopyUnitBuilds(pUnitBuilds, lAssignment);
        }

        public void SetAssignment(MatchPlayerSlot pSlot, string pPlayerId, string pTeamPresetId, IReadOnlyList<string> pUnitIds)
        {
            SetAssignment(pSlot, pPlayerId, pTeamPresetId, pUnitIds, null);
        }

        public void SetAssignment(
            MatchPlayerSlot pSlot,
            string pPlayerId,
            string pTeamPresetId,
            IReadOnlyList<string> pUnitIds,
            IReadOnlyList<MatchUnitBuild> pUnitBuilds)
        {
            if (pSlot == MatchPlayerSlot.None || string.IsNullOrWhiteSpace(pPlayerId))
                return;

            Players ??= new List<MatchPlayerAssignment>();
            if (!TryGetAssignment(pSlot, out MatchPlayerAssignment lAssignment))
            {
                lAssignment = new MatchPlayerAssignment { Slot = pSlot };
                Players.Add(lAssignment);
            }

            lAssignment.PlayerId = pPlayerId;
            lAssignment.TeamPresetId = pTeamPresetId ?? string.Empty;
            CopyUnitIds(pUnitIds, lAssignment);
            CopyUnitBuilds(pUnitBuilds, lAssignment);
        }

        public void MergeCompositions(MatchManifest pOther)
        {
            if (pOther?.Players == null || Players == null)
                return;

            for (int lIndex = 0; lIndex < pOther.Players.Count; lIndex++)
            {
                MatchPlayerAssignment lIncoming = pOther.Players[lIndex];
                MatchPlayerAssignment lLocal = lIncoming != null ? FindAssignment(lIncoming.PlayerId) : null;
                if (lLocal == null)
                    continue;

                if (lIncoming.UnitIds != null && lIncoming.UnitIds.Count > 0)
                    CopyUnitIds(lIncoming.UnitIds, lLocal);
                if (lIncoming.UnitBuilds != null && lIncoming.UnitBuilds.Count > 0)
                    CopyUnitBuilds(lIncoming.UnitBuilds, lLocal);
            }
        }

        public bool TryValidatePlayerAssignments(out string pFailure)
        {
            pFailure = string.Empty;

            if (PerfectVelocityTieStartingSlot is not (MatchPlayerSlot.None or MatchPlayerSlot.TeamA or MatchPlayerSlot.TeamB))
            {
                pFailure = "Match manifest contains an invalid perfect Velocity tie starting slot.";
                return false;
            }

            if (Players == null || Players.Count == 0)
            {
                pFailure = "Match manifest has no player slot assignments.";
                return false;
            }

            int lTeamACount = 0;
            int lTeamBCount = 0;
            HashSet<string> lPlayerIds = new HashSet<string>();
            for (int lIndex = 0; lIndex < Players.Count; lIndex++)
            {
                MatchPlayerAssignment lPlayer = Players[lIndex];
                if (lPlayer == null || string.IsNullOrWhiteSpace(lPlayer.PlayerId))
                {
                    pFailure = "Match manifest contains an empty player assignment.";
                    return false;
                }

                if (!lPlayerIds.Add(lPlayer.PlayerId))
                {
                    pFailure = $"Match manifest contains duplicate player assignment '{lPlayer.PlayerId}'.";
                    return false;
                }

                if (lPlayer.Slot == MatchPlayerSlot.TeamA)
                    lTeamACount++;
                else if (lPlayer.Slot == MatchPlayerSlot.TeamB)
                    lTeamBCount++;
                else
                {
                    pFailure = $"Match manifest contains invalid slot '{lPlayer.Slot}' for player '{lPlayer.PlayerId}'.";
                    return false;
                }

                if (!TryValidateUnitBuilds(lPlayer, out pFailure))
                    return false;
            }

            if (lTeamACount == 1 && lTeamBCount == 1)
                return true;

            pFailure = $"Match manifest expected one TeamA player and one TeamB player, got TeamA={lTeamACount}, TeamB={lTeamBCount}.";
            return false;
        }

        public bool HasSamePlayerAssignments(MatchManifest pOther)
        {
            if (pOther == null || MatchId != pOther.MatchId || Players == null || pOther.Players == null || Players.Count != pOther.Players.Count)
                return false;

            for (int lIndex = 0; lIndex < Players.Count; lIndex++)
            {
                MatchPlayerAssignment lPlayer = Players[lIndex];
                if (lPlayer == null || !pOther.TryGetSlot(lPlayer.PlayerId, out MatchPlayerSlot lOtherSlot) || lOtherSlot != lPlayer.Slot)
                    return false;
            }

            return true;
        }

        private MatchPlayerAssignment FindAssignment(string pPlayerId)
        {
            if (string.IsNullOrWhiteSpace(pPlayerId) || Players == null)
                return null;

            for (int lIndex = 0; lIndex < Players.Count; lIndex++)
            {
                MatchPlayerAssignment lPlayer = Players[lIndex];
                if (lPlayer != null && lPlayer.PlayerId == pPlayerId)
                    return lPlayer;
            }

            return null;
        }

        private static void CopyUnitIds(IReadOnlyList<string> pUnitIds, MatchPlayerAssignment pTarget)
        {
            pTarget.UnitIds ??= new List<string>();
            pTarget.UnitIds.Clear();
            pTarget.UnitBuilds?.Clear();

            if (pUnitIds == null)
                return;

            for (int lIndex = 0; lIndex < pUnitIds.Count; lIndex++)
            {
                string lUnitId = pUnitIds[lIndex];
                if (!string.IsNullOrWhiteSpace(lUnitId))
                    pTarget.UnitIds.Add(lUnitId);
            }
        }

        private static void CopyUnitBuilds(IReadOnlyList<MatchUnitBuild> pBuilds, MatchPlayerAssignment pTarget)
        {
            pTarget.UnitBuilds ??= new List<MatchUnitBuild>();
            pTarget.UnitBuilds.Clear();
            if (pBuilds == null)
                return;

            for (int lIndex = 0; lIndex < pBuilds.Count; lIndex++)
            {
                MatchUnitBuild lBuild = pBuilds[lIndex];
                if (lBuild == null || string.IsNullOrWhiteSpace(lBuild.UnitId))
                    continue;

                MatchUnitBuild lCopy = new MatchUnitBuild
                {
                    UnitId = lBuild.UnitId,
                    PassiveId = lBuild.PassiveId ?? string.Empty,
                    StatAllocations = CopyStatAllocations(lBuild.StatAllocations)
                };

                if (lBuild.SkillIds != null)
                {
                    for (int lSkillIndex = 0; lSkillIndex < lBuild.SkillIds.Count; lSkillIndex++)
                    {
                        string lSkillId = lBuild.SkillIds[lSkillIndex];
                        if (!string.IsNullOrWhiteSpace(lSkillId))
                            lCopy.SkillIds.Add(lSkillId);
                    }
                }

                pTarget.UnitBuilds.Add(lCopy);
            }
        }

        private static MatchUnitStatAllocations CopyStatAllocations(MatchUnitStatAllocations pSource) => new MatchUnitStatAllocations
        {
            Health = pSource?.Health ?? 0,
            Energy = pSource?.Energy ?? 0,
            Mobility = pSource?.Mobility ?? 0,
            MeleeDamage = pSource?.MeleeDamage ?? 0,
            MeleeResistance = pSource?.MeleeResistance ?? 0,
            RangedDamage = pSource?.RangedDamage ?? 0,
            RangedResistance = pSource?.RangedResistance ?? 0,
            Velocity = pSource?.Velocity ?? 0
        };

        private static bool TryValidateUnitBuilds(MatchPlayerAssignment pPlayer, out string pFailure)
        {
            pFailure = string.Empty;
            if (pPlayer.UnitBuilds == null || pPlayer.UnitBuilds.Count == 0)
                return true;

            if (pPlayer.UnitIds == null || pPlayer.UnitBuilds.Count != pPlayer.UnitIds.Count)
            {
                pFailure = $"Match manifest build count does not match unit count for player '{pPlayer.PlayerId}'.";
                return false;
            }

            for (int lIndex = 0; lIndex < pPlayer.UnitBuilds.Count; lIndex++)
            {
                MatchUnitBuild lBuild = pPlayer.UnitBuilds[lIndex];
                if (lBuild == null || lBuild.UnitId != pPlayer.UnitIds[lIndex])
                {
                    pFailure = $"Match manifest build order does not match unit order for player '{pPlayer.PlayerId}'.";
                    return false;
                }

                if (lBuild.SkillIds == null || lBuild.SkillIds.Count > 6)
                {
                    pFailure = $"Match manifest contains an invalid skill count for unit '{lBuild.UnitId}'.";
                    return false;
                }

                HashSet<string> lSkillIds = new HashSet<string>(StringComparer.Ordinal);
                for (int lSkillIndex = 0; lSkillIndex < lBuild.SkillIds.Count; lSkillIndex++)
                {
                    string lSkillId = lBuild.SkillIds[lSkillIndex];
                    if (string.IsNullOrWhiteSpace(lSkillId) || !lSkillIds.Add(lSkillId))
                    {
                        pFailure = $"Match manifest contains an empty or duplicate skill for unit '{lBuild.UnitId}'.";
                        return false;
                    }
                }

                if (!HasNonNegativeStatAllocations(lBuild.StatAllocations))
                {
                    pFailure = $"Match manifest contains a negative stat allocation for unit '{lBuild.UnitId}'.";
                    return false;
                }
            }

            return true;
        }

        private static bool HasNonNegativeStatAllocations(MatchUnitStatAllocations pStats) =>
            pStats == null
            || (pStats.Health >= 0
                && pStats.Energy >= 0
                && pStats.Mobility >= 0
                && pStats.MeleeDamage >= 0
                && pStats.MeleeResistance >= 0
                && pStats.RangedDamage >= 0
                && pStats.RangedResistance >= 0
                && pStats.Velocity >= 0);
    }

    [Serializable]
    public sealed class MatchServerEndpoint
    {
        public string IpAddress = "127.0.0.1";
        public ushort Port = 7777;
        public string AllocationId = string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(IpAddress) && Port > 0;
    }

    public enum MatchConnectionMode
    {
        Offline = 0,
        CustomHost = 1,
        CustomJoin = 2,
        QuickMatchLocalServer = 3,
        DedicatedServer = 4,
        CustomRelayHost = 5,
        CustomRelayJoin = 6
    }

    public static class MatchTrustPolicy
    {
        public static bool IsCustom(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomHost or MatchConnectionMode.CustomJoin
                or MatchConnectionMode.CustomRelayHost or MatchConnectionMode.CustomRelayJoin;

        public static bool UsesDedicatedAuthority(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.QuickMatchLocalServer or MatchConnectionMode.DedicatedServer;

        public static bool CanReportRankedResults(MatchConnectionMode pMode, bool pIsTrusted, bool pReportsRankedResults) =>
            pMode == MatchConnectionMode.DedicatedServer && pIsTrusted && pReportsRankedResults;

        public static string BuildTrustLabel(MatchConnectionMode pMode, bool pIsTrusted, bool pReportsRankedResults)
        {
            if (IsCustom(pMode))
                return "Custom unranked";

            if (CanReportRankedResults(pMode, pIsTrusted, pReportsRankedResults))
                return "Ranked authoritative";

            return pMode switch
            {
                MatchConnectionMode.QuickMatchLocalServer => "Local server test",
                MatchConnectionMode.DedicatedServer => "Authoritative unranked",
                _ => "Offline"
            };
        }
    }

}
