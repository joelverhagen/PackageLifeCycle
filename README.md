# Knapcode.PackageLifeCycle

A CLI tool to help you manage the lifecycle of published NuGet packages.

Right now, all it does is deprecates packages using a preview "deprecate" API on NuGet.org.


## Install

```
dotnet tool install Knapcode.PackageLifeCycle --prerelease --global
```

## Deprecate

### Example

```
nuget-plc deprecate jQuery --all --legacy --api-key "o2yhehehehehe"
```

### Help text

```
Description:
  Mark existing packages as deprecated.

Usage:
  nuget-plc deprecate <PACKAGE_ID> [options]

Arguments:
  <PACKAGE_ID>  The ID of the package that should be deprecated.

Options:
  --version <version>                           A specific version to mark as deprecated
                                                (multiple allowed).
  --range <range>                               A range of versions to mark as deprecated
                                                (multiple allowed).
  --all                                         Deprecate all versions.
  --api-key <api-key>                           The API key to use when deprecating the
                                                package.
  --legacy                                      Mark the deprecated versions as legacy.
  --has-critical-bugs                           Mark the deprecated versions as having
                                                critical bugs.
  --other                                       Mark the deprecated versions as having some
                                                other deprecation reason. Enabled by default
                                                if no other deprecation reason is selected.
  --message <message>                           A deprecation message to display. Required
                                                if --other is specified or no other
                                                deprecation reason is selected.
  --alternate-id <alternate-id>                 An alternate package ID to recommend instead
                                                of this package.
  --alternate-version <alternate-version>       A specific alternate package version to
                                                recommend. Only usable with --alternate-id.
  --dry-run                                     Runs the entire operation without actually
                                                submitting the deprecation request.
  --overwrite                                   Replace existing deprecation metadata on a
                                                package version.
  --allow-missing-versions                      Allow deprecating versions that are not yet
                                                available on the source.
  --skip-validation                             Skip as much validation as possible before
                                                submitting the request.
  --source <source>                             The package source to use. [default:
                                                https://api.nuget.org/v3/index.json]
  --package-publish-url <package-publish-url>   The URL to use for the PackagePublish
                                                resource. Defaults to discovering it from
                                                the --source option.
  --log-level                                   The minimum log level to display. [default:
  <Debug|Error|Fatal|Information|Verbose|Warni  Information]
  ng>
  -?, -h, --help                                Show help and usage information
```
