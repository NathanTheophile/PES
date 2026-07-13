using System;
using System.Reflection;
using NUnit.Framework;
using TeamPresetsModule;

namespace TestProject;

public sealed class TeamPresetValidationTests
{
    private static readonly MethodInfo ValidateMethod = typeof(TeamPresets).GetMethod(
        "ValidateSnapshot",
        BindingFlags.NonPublic | BindingFlags.Static)!;

    [Test]
    public void ValidSnapshotIsAccepted()
    {
        Assert.DoesNotThrow(() => Validate(BuildSnapshot("forban", "simpleunit", "testunit")));
    }

    [Test]
    public void DuplicateUnitsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => Validate(BuildSnapshot("forban", "forban", "testunit")));
    }

    [Test]
    public void AllocationAtBudgetIsAccepted()
    {
        string json = BuildSnapshot("forban", "simpleunit", "testunit")
            .Replace("{ \"Health\": 0 }", "{ \"Power\": 4, \"MeleeDamage\": 4 }");

        Assert.DoesNotThrow(() => Validate(json));
    }

    [Test]
    public void AllocationAboveBudgetIsRejected()
    {
        string json = BuildSnapshot("forban", "simpleunit", "testunit")
            .Replace("{ \"Health\": 0 }", "{ \"Power\": 4, \"MeleeDamage\": 5 }");

        Assert.Throws<ArgumentException>(() => Validate(json));
    }

    [Test]
    public void UnitWithoutTrustedBudgetIsRejected()
    {
        Assert.Throws<ArgumentException>(() => Validate(BuildSnapshot("unknown-unit", "simpleunit", "testunit")));
    }

    private static void Validate(string json)
    {
        try
        {
            ValidateMethod.Invoke(null, new object[] { json });
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }

    private static string BuildSnapshot(string firstUnit, string secondUnit, string thirdUnit) => $$"""
        {
          "SchemaVersion": 1,
          "ActivePresetId": "default-team",
          "Presets": [
            {
              "SchemaVersion": 1,
              "PresetId": "default-team",
              "DisplayName": "Team 1",
              "Slots": [
                { "UnitId": "{{firstUnit}}", "PassiveId": "passive-a", "SkillIds": ["skill-a"], "StatAllocations": { "Health": 0 } },
                { "UnitId": "{{secondUnit}}", "PassiveId": "passive-b", "SkillIds": ["skill-b"], "StatAllocations": { "Health": 0 } },
                { "UnitId": "{{thirdUnit}}", "PassiveId": "passive-c", "SkillIds": ["skill-c"], "StatAllocations": { "Health": 0 } }
              ]
            }
          ]
        }
        """;
}
