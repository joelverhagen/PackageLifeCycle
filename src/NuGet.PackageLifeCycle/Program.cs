using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NuGet.PackageLifeCycle;
using NuGet.Protocol.Core.Types;
using Serilog;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Runtime.InteropServices;

var builder = new UserAgentStringBuilder("NuGet.PackageLifeCycle")
    .WithOSDescription(string.Join("; ", new object[]
    {
        RuntimeInformation.OSDescription,
        RuntimeInformation.OSArchitecture,
        RuntimeInformation.FrameworkDescription,
        RuntimeInformation.RuntimeIdentifier,
    }));
UserAgent.SetUserAgentString(builder);

var runner = new CommandLineBuilder(new PackageLifeCycleCommand())
    .UseExceptionHandler()
    .UseHost(_ => Host.CreateDefaultBuilder(args), (builder) => builder
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            services.AddTransient<DeprecationService>();
            services.AddCustomSerilog();
        })
        .UseCommandHandler<DeprecateCommand, DeprecateCommand.Handler>())
    .UseHelp()
    .Build();

if (args.Length == 0 || args.All(string.IsNullOrWhiteSpace))
{
    args = new[] { "--help" };
}

return await runner.Parse(args).InvokeAsync();