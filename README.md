# Knapcode.PackageLifeCycle (nuget-plc)

A CLI tool to help you manage the lifecycle of published NuGet packages.

Right now, all it does is deprecates packages using a preview "deprecate" API on NuGet.org.

## Install

```console
dotnet tool install Knapcode.PackageLifeCycle --prerelease --global
```

This will install the `nuget-plc` command into your PATH.

## Deprecate

This command is used to mark packages as deprecated.

### Example

**Note:** The `--api-key` option is required when deprecating packages on NuGet.org.

Mark a specific version as having critical bugs with a message.

```console
nuget-plc deprecate jQuery --version 3.5.0 --critical-bugs --message "Bad, bad bugs!"
```

Mark a specific range of versions as deprecated with an alternate.

```console
nuget-plc deprecate NuGet.Core --range "[, 3.0.0)" --alternate-id NuGet.Protocol --message "Use this other thing."
```

Mark all versions as legacy.

```console
nuget-plc deprecate jQuery --all --legacy
```

### Help text

<!-- snippet: ProgramTests.Help.verified.txt -->
<a id='snippet-ProgramTests.Help.verified.txt'></a>
```txt
Description:
  A CLI tool to help you manage the lifecycle of published NuGet packages.

Usage:
  nuget-plc [command] [options]

Options:
  --log-level <level>  The minimum log level to display. Possible values: Verbose, Debug, Information, Warning, Error, Fatal [default: Information]
  -?, -h, --help       Show help and usage information
  --version            Show version information

Commands:
  deprecate <PACKAGE_ID>  Mark existing packages as deprecated.
```
<sup><a href='/src/Tests/ProgramTests.Help.verified.txt#L1-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-ProgramTests.Help.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->
