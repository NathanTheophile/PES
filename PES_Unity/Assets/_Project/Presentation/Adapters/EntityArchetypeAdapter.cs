using System;
using System.Collections.Generic;
using PES.Core.Simulation;
using PES.Presentation.Configuration;

namespace PES.Presentation.Adapters
{
    public static class EntityArchetypeAdapter
    {
        public static EntityInitializationData ToInitializationData(
            EntityArchetypeAsset archetype,
            EntityId entityId,
            Position3 spawnPosition)
        {
            if (archetype == null)
            {
                throw new ArgumentNullException(nameof(archetype));
            }

            return new EntityInitializationData(
                entityId: entityId,
                spawnPosition: spawnPosition,
                hitPoints: ClampNonNegative(archetype.BaseHitPoints),
                movementPoints: ClampNonNegative(archetype.BaseMovementPoints),
                skillResource: ClampNonNegative(archetype.BaseSkillResource),
                tags: NormalizeTags(archetype.Tags));
        }

        private static int ClampNonNegative(int value)
        {
            return value < 0 ? 0 : value;
        }

        private static string[] NormalizeTags(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            var tags = new List<string>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                var value = source[i];
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var trimmed = value.Trim();
                var hasEquivalent = false;
                for (var j = 0; j < tags.Count; j++)
                {
                    if (!string.Equals(tags[j], trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    hasEquivalent = true;
                    break;
                }

                if (!hasEquivalent)
                {
                    tags.Add(trimmed);
                }
            }

            tags.Sort(StringComparer.Ordinal);
            return tags.ToArray();
        }
    }
}
