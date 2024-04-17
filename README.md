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

```plaintext
Description:
  Mark existing packages as deprecated.

Usage:
  nuget-plc deprecate <PACKAGE_ID> [options]

Arguments:
  <PACKAGE_ID>  The ID of the package that should be deprecated.

Options:
  --version <version>          A specific version to mark as deprecated (multiple allowed).
  --range <range>              A range of versions to mark as deprecated (multiple allowed).
  --all                        Deprecate all versions.
  --api-key <api-key>          The API key to use when deprecating the package.
  --legacy                     Mark the deprecated versions as legacy.
  --critical-bugs              Mark the deprecated versions as having critical bugs.
  --other-reason               Mark the deprecated versions as having some other deprecation
                               reason. Enabled by default if no other deprecation reason is
                               selected.
  --message <message>          A deprecation message to display. Required if --other-reason is
                               specified or no other deprecation reason is selected.
  --alternate-id <id>          An alternate package ID to recommend instead of this package.
  --alternate-version <ver>    A specific alternate package version to recommend. Only usable with
                               --alternate-id.
  --dry-run                    Runs the entire operation without actually submitting the
                               deprecation request.
  --overwrite                  Replace existing deprecation metadata on a package version.
  --allow-missing-versions     Allow deprecating versions that are not yet available on the source.
  --skip-validation            Skip as much validation as possible before submitting the request.
  --source <source>            The package source to use. [default:
                               https://api.nuget.org/v3/index.json]
  --package-publish-url <url>  The URL to use for the PackagePublish resource. Defaults to
                               discovering it from the --source option.
  --listed                     Set the listed status of the versions while deprecating. Use 'false'
                               to unlist the versions, 'true' to relist them. If the option is not
                               provided, it defaults to not changing the listed status at all.
  --confirm                    Interactively confirm the contents of the deprecation API request
                               before proceeding.
  --log-level <level>          The minimum log level to display. Possible values: Verbose, Debug,
                               Information, Warning, Error, Fatal [default: Information]
  -?, -h, --help               Show help and usage information
```
