using PES.Combat.Actions;
using PES.Presentation.Configuration;

namespace PES.Presentation.Adapters
{
    public static class SkillDefinitionAdapter
    {
        public static SkillActionPolicy? ToPolicy(SkillDefinitionAsset asset)
        {
            if (asset == null)
            {
                return null;
            }

            return asset.ToSkillActionPolicy();
        }
    }
}
