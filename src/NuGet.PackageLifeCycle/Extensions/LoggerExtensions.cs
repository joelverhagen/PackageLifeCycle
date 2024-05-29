using Microsoft.Extensions.Logging;

namespace NuGet.PackageLifeCycle;

public static class LoggerExtensions
{
    public static Common.ILogger ToNuGetLogger(this ILogger logger, bool mapInformationToDebug)
    {
        return new StandardToNuGetLogger(logger, mapInformationToDebug);
    }

    public static Common.ILogger ToNuGetLogger(this ILogger logger)
    {
        return new StandardToNuGetLogger(logger);
    }
}
