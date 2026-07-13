using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace TeamPresetsModule;

public sealed class TeamPresets(IGameApiClient gameApiClient, ILogger<TeamPresets> logger)
{
    private const string CloudSaveKey = "teamPresetsV1";
    private const int CurrentSchemaVersion = 1;
    private const int TeamSize = 3;
    private const int EquippedSkillCount = 6;
    private const int MaxPresetCount = 20;
    private const int MaxSnapshotCharacters = 131072;
    private const int MaxIdCharacters = 128;
    private const int MaxDisplayNameCharacters = 64;
    private const int HealthCost = 1;
    private const int ActionPointCost = 25;
    private const int MovementCost = 20;
    private const int DamageCost = 5;
    private const int ResistanceCost = 5;
    private const int InitiativeCost = 1;

    [CloudCodeFunction("LoadTeamPresets")]
    public async Task<string> LoadTeamPresets(IExecutionContext context)
    {
        EnsureAuthenticated(context);
        string playerId = context.PlayerId!;

        try
        {
            var result = await gameApiClient.CloudSaveData.GetProtectedItemsAsync(
                context,
                context.AccessToken,
                context.ProjectId,
                playerId,
                new List<string> { CloudSaveKey });

            return result.Data.Results.FirstOrDefault()?.Value?.ToString() ?? string.Empty;
        }
        catch (ApiException exception)
        {
            logger.LogError(exception, "Failed to load team presets for player {PlayerId}.", playerId);
            throw;
        }
    }

    [CloudCodeFunction("SaveTeamPresets")]
    public async Task SaveTeamPresets(IExecutionContext context, string snapshotJson)
    {
        EnsureAuthenticated(context);
        string playerId = context.PlayerId!;
        ValidateSnapshot(snapshotJson);

        try
        {
            await gameApiClient.CloudSaveData.SetProtectedItemAsync(
                context,
                context.ServiceToken,
                context.ProjectId,
                playerId,
                new SetItemBody(CloudSaveKey, snapshotJson));
        }
        catch (ApiException exception)
        {
            logger.LogError(exception, "Failed to save team presets for player {PlayerId}.", playerId);
            throw;
        }
    }

    private static void ValidateSnapshot(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson) || snapshotJson.Length > MaxSnapshotCharacters)
            throw new ArgumentException("Team preset snapshot is empty or exceeds the size limit.", nameof(snapshotJson));

        JObject root;
        try
        {
            root = JObject.Parse(snapshotJson);
        }
        catch (Exception exception)
        {
            throw new ArgumentException("Team preset snapshot is not valid JSON.", nameof(snapshotJson), exception);
        }

        if (root.Value<int?>("SchemaVersion") != CurrentSchemaVersion)
            throw new ArgumentException("Unsupported team preset schema version.", nameof(snapshotJson));
        if (root.Value<long?>("UpdatedAtUnixMilliseconds") is < 0)
            throw new ArgumentException("Team preset update timestamp cannot be negative.", nameof(snapshotJson));

        string activePresetId = ReadRequiredId(root, "ActivePresetId");
        JArray presets = root["Presets"] as JArray
            ?? throw new ArgumentException("Team preset snapshot is missing Presets.", nameof(snapshotJson));
        if (presets.Count == 0 || presets.Count > MaxPresetCount)
            throw new ArgumentException($"Team preset count must be between 1 and {MaxPresetCount}.", nameof(snapshotJson));

        HashSet<string> presetIds = new(StringComparer.Ordinal);
        foreach (JObject preset in presets.OfType<JObject>())
            ValidatePreset(preset, presetIds);

        if (presetIds.Count != presets.Count || !presetIds.Contains(activePresetId))
            throw new ArgumentException("Team preset IDs must be unique and include ActivePresetId.", nameof(snapshotJson));
    }

    private static void ValidatePreset(JObject preset, ISet<string> presetIds)
    {
        if (preset.Value<int?>("SchemaVersion") != CurrentSchemaVersion)
            throw new ArgumentException("A team preset has an unsupported schema version.");

        string presetId = ReadRequiredId(preset, "PresetId");
        if (!presetIds.Add(presetId))
            throw new ArgumentException($"Duplicate team preset ID '{presetId}'.");

        string displayName = preset.Value<string>("DisplayName") ?? string.Empty;
        if (displayName.Length > MaxDisplayNameCharacters)
            throw new ArgumentException($"Team preset '{presetId}' has a display name that is too long.");

        JArray slots = preset["Slots"] as JArray
            ?? throw new ArgumentException($"Team preset '{presetId}' is missing Slots.");
        if (slots.Count != TeamSize)
            throw new ArgumentException($"Team preset '{presetId}' must contain exactly {TeamSize} slots.");

        HashSet<string> unitIds = new(StringComparer.Ordinal);
        foreach (JObject slot in slots.OfType<JObject>())
            ValidateSlot(slot, unitIds);
        if (slots.OfType<JObject>().Count() != TeamSize)
            throw new ArgumentException($"Team preset '{presetId}' contains an invalid slot.");
    }

    private static void ValidateSlot(JObject slot, ISet<string> unitIds)
    {
        string unitId = ReadOptionalId(slot, "UnitId");
        string passiveId = ReadOptionalId(slot, "PassiveId");
        JArray skillIds = slot["SkillIds"] as JArray ?? new JArray();

        if (string.IsNullOrEmpty(unitId))
        {
            if (!string.IsNullOrEmpty(passiveId) || skillIds.Count > 0)
                throw new ArgumentException("An empty team slot cannot contain a passive or skills.");
        }
        else
        {
            if (!unitIds.Add(unitId))
                throw new ArgumentException($"Duplicate unit ID '{unitId}' in a team preset.");
            if (!UnitStatBudgetCatalog.TryGetBudget(unitId, out _))
                throw new ArgumentException($"Unit '{unitId}' has no trusted stat budget configuration.");
        }

        if (skillIds.Count > EquippedSkillCount)
            throw new ArgumentException($"A unit build cannot contain more than {EquippedSkillCount} skills.");

        HashSet<string> uniqueSkillIds = new(StringComparer.Ordinal);
        foreach (JToken skillToken in skillIds)
        {
            string skillId = ValidateId(skillToken.Value<string>() ?? string.Empty, "SkillId", false);
            if (!uniqueSkillIds.Add(skillId))
                throw new ArgumentException($"Duplicate skill ID '{skillId}' in a unit build.");
        }

        if (slot["StatAllocations"] is JObject stats)
            ValidateStatAllocations(unitId, stats);
    }

    private static void ValidateStatAllocations(string unitId, JObject stats)
    {
        if (string.IsNullOrEmpty(unitId))
        {
            if (stats.Properties().Any(property => property.Value.Value<int>() != 0))
                throw new ArgumentException("An empty team slot cannot contain stat allocations.");
            return;
        }

        if (!UnitStatBudgetCatalog.TryGetBudget(unitId, out int budget))
            throw new ArgumentException($"Unit '{unitId}' has no trusted stat budget configuration.");

        Dictionary<string, int> costs = new(StringComparer.Ordinal)
        {
            ["Health"] = HealthCost,
            ["Power"] = ActionPointCost,
            ["Movement"] = MovementCost,
            ["MeleeDamage"] = DamageCost,
            ["MeleeResistance"] = ResistanceCost,
            ["RangedDamage"] = DamageCost,
            ["RangedResistance"] = ResistanceCost,
            ["Initiative"] = InitiativeCost
        };

        long spentPoints = 0;
        foreach (JProperty property in stats.Properties())
        {
            if (!costs.TryGetValue(property.Name, out int cost))
                throw new ArgumentException($"Unknown stat allocation '{property.Name}'.");

            int value = property.Value.Value<int>();
            if (value < 0)
                throw new ArgumentException($"Stat allocation '{property.Name}' cannot be negative.");

            spentPoints += (long)value * cost;
            if (spentPoints > budget)
                throw new ArgumentException($"Stat allocations for unit '{unitId}' cost {spentPoints}, above its budget of {budget}.");
        }
    }

    private static string ReadRequiredId(JObject source, string propertyName) =>
        ValidateId(source.Value<string>(propertyName) ?? string.Empty, propertyName, false);

    private static string ReadOptionalId(JObject source, string propertyName) =>
        ValidateId(source.Value<string>(propertyName) ?? string.Empty, propertyName, true);

    private static string ValidateId(string value, string label, bool allowEmpty)
    {
        string normalized = value.Trim();
        if ((!allowEmpty && normalized.Length == 0) || normalized.Length > MaxIdCharacters)
            throw new ArgumentException($"{label} is empty or exceeds {MaxIdCharacters} characters.");
        return normalized;
    }

    private static void EnsureAuthenticated(IExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.PlayerId))
            throw new InvalidOperationException("Team presets require an authenticated player.");
    }
}
