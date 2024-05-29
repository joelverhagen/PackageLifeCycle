using System.CommandLine;
using Serilog.Events;

namespace NuGet.PackageLifeCycle;

public class PackageLifeCycleCommand : RootCommand
{
    public const string LogLevelOption = "--log-level";

    public PackageLifeCycleCommand() : base("A CLI tool to help you manage the lifecycle of published NuGet packages.")
    {
        AddGlobalOption(new Option<LogEventLevel>(
            LogLevelOption,
            () => LogEventLevel.Information,
            "The minimum log level to display. Possible values: " + string.Join(", ", Enum.GetNames<LogEventLevel>()))
        {
            ArgumentHelpName = "level",
        });

        Name = "nuget-plc";

        AddCommand(new DeprecateCommand());
    }
}
