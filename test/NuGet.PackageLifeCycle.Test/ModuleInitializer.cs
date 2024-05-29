using System.Runtime.CompilerServices;

namespace NuGet.PackageLifeCycle;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.InitializePlugins();
    }
}
