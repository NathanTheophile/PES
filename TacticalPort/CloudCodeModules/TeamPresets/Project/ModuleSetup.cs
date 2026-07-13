using Microsoft.Extensions.DependencyInjection;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace TeamPresetsModule;

public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
#pragma warning disable CS0618
        config.Dependencies.AddSingleton<IGameApiClient>(GameApiClient.Create());
#pragma warning restore CS0618
    }
}
