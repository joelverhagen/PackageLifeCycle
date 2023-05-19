using Serilog.Events;
using System.CommandLine;

namespace NuGet.PackageLifeCycle;

public class PackageLifeCycleCommand : RootCommand
{
    public const string LogLevelOption = "--log-level";

    public PackageLifeCycleCommand() : base("A CLI tool to help you manage the lifecycle of published NuGet packages. You can relist and deprecate packages.")
    {
        AddGlobalOption(new Option<LogEventLevel>(
            LogLevelOption,
            () => LogEventLevel.Information,
            "The minimum log level to display."));

        AddCommand(new DeprecateCommand());
    }
}
