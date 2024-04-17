using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace NuGet.PackageLifeCycle;

public class DeprecateCommand : Command
{
    public const string AllOption = "--all";
    public const string AllowMissingVersionsOption = "--allow-missing-versions";
    public const string AlternateIdOption = "--alternate-id";
    public const string AlternateVersionOption = "--alternate-version";
    public const string ApiKeyOption = "--api-key";
    public const string DryRunOption = "--dry-run";
    public const string CriticalBugsOption = "--critical-bugs";
    public const string LegacyOption = "--legacy";
    public const string MessageOption = "--message";
    public const string OtherReasonOption = "--other-reason";
    public const string OverwriteOption = "--overwrite";
    public const string SkipValidation = "--skip-validation";
    public const string PackagePublishUrlOption = "--package-publish-url";
    public const string RangeOption = "--range";
    public const string SourceOption = "--source";
    public const string VersionOption = "--version";
    public const string Listed = "--listed";
    public const string Confirm = "--confirm";

    public DeprecateCommand() : base("deprecate", "Mark existing packages as deprecated.")
    {
        AddArgument(new Argument<string>("PACKAGE_ID", "The ID of the package that should be deprecated.")
        {
            Arity = ArgumentArity.ExactlyOne,
        });
        AddOption(new Option<List<string>>(VersionOption, "A specific version to mark as deprecated (multiple allowed).")
        {
            AllowMultipleArgumentsPerToken = true,
        });
        AddOption(new Option<List<string>>(RangeOption, "A range of versions to mark as deprecated (multiple allowed).")
        {
            AllowMultipleArgumentsPerToken = true,
        });
        AddOption(new Option<bool>(AllOption, "Deprecate all versions."));
        AddOption(new Option<string>(ApiKeyOption, "The API key to use when deprecating the package."));
        AddOption(new Option<bool>(LegacyOption, "Mark the deprecated versions as legacy."));
        AddOption(new Option<bool>(CriticalBugsOption, "Mark the deprecated versions as having critical bugs."));
        AddOption(new Option<bool>(OtherReasonOption, "Mark the deprecated versions as having some other deprecation reason. Enabled by default if no other deprecation reason is selected."));
        AddOption(new Option<string>(MessageOption, $"A deprecation message to display. Required if {OtherReasonOption} is specified or no other deprecation reason is selected."));
        AddOption(new Option<string>(AlternateIdOption, "An alternate package ID to recommend instead of this package.")
        {
            ArgumentHelpName = "id",
        });
        AddOption(new Option<string>(AlternateVersionOption, $"A specific alternate package version to recommend. Only usable with {AlternateIdOption}.")
        {
            ArgumentHelpName = "ver",
        });
        AddOption(new Option<bool>(DryRunOption, "Runs the entire operation without actually submitting the deprecation request."));
        AddOption(new Option<bool>(OverwriteOption, "Replace existing deprecation metadata on a package version."));
        AddOption(new Option<bool>(AllowMissingVersionsOption, "Allow deprecating versions that are not yet available on the source."));
        AddOption(new Option<bool>(SkipValidation, $"Skip as much validation as possible before submitting the request."));
        AddOption(new Option<string>(SourceOption, () => "https://api.nuget.org/v3/index.json", "The package source to use."));
        AddOption(new Option<string>(PackagePublishUrlOption, $"The URL to use for the PackagePublish resource. Defaults to discovering it from the {SourceOption} option.")
        {
            ArgumentHelpName = "url",
        });
        AddOption(new Option<bool?>(Listed, "Set the listed status of the versions while deprecating. Use 'false' to unlist the versions, 'true' to relist them. If the option is not provided, it defaults to not changing the listed status at all."));
        AddOption(new Option<bool>(Confirm, "Interactively confirm the contents of the deprecation API request before proceeding."));

    }

    public new class Handler : ICommandHandler
    {
        private readonly DeprecationService _service;
        private readonly ILogger<Handler> _logger;
        private readonly Common.ILogger _httpLogger;

        public string? Package_Id { get; set; }
        public bool All { get; set; }
        public List<string>? Version { get; set; }
        public List<string>? Range { get; set; }
        public string? ApiKey { get; set; }
        public bool Legacy { get; set; }
        public bool CriticalBugs { get; set; }
        public bool OtherReason { get; set; }
        public string? Message { get; set; }
        public string? AlternateId { get; set; }
        public string? AlternateVersion { get; set; }
        public bool DryRun { get; set; }
        public bool Overwrite { get; set; }
        public bool AllowMissingVersions { get; set; }
        public bool SkipValidation { get; set; }
        public string Source { get; set; } = "https://api.nuget.org/v3/index.json";
        public string? PackagePublishUrl { get; set; }
        public bool? Listed { get; set; }
        public bool Confirm { get; set; }
        private bool IsV3 { get; set; } = true;

        public Handler(DeprecationService service, ILogger<Handler> logger)
        {
            _service = service;
            _logger = logger;
            _httpLogger = logger.ToNuGetLogger(mapInformationToDebug: true);
        }

        public int Invoke(InvocationContext context)
        {
            throw new NotImplementedException();
        }

        public async Task<int> InvokeAsync(InvocationContext context)
        {
            if (Package_Id is null)
            {
                _logger.LogError("No package ID was provided.");
                return 1;
            }

            if (!SkipValidation && !AreArgumentsValid())
            {
                return 1;
            }

            using var cacheContext = new SourceCacheContext { MaxAge = DateTimeOffset.Now };
            var sourceRepository = Repository.Factory.GetCoreV3(Source);

            var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            if (serviceIndex is null)
            {
                IsV3 = false;
                _logger.LogWarning("The package source URL does not appear to be a V3 package source.");
            }

            if (!SkipValidation && AlternateId is not null)
            {
                _logger.LogDebug("Validating alternate package information.");
                if (!await IsAlternatePackageValidAsync(sourceRepository, cacheContext, context.GetCancellationToken()))
                {
                    return 1;
                }
            }

            _logger.LogInformation("Reading the version list for {Id} on {Source}.", Package_Id, Source);
            var versionsToDeprecate = await GetMatchingVersionsAsync(sourceRepository, cacheContext, context.GetCancellationToken());
            if (versionsToDeprecate is null)
            {
                return 1;
            }

            if (!Overwrite)
            {
                _logger.LogInformation("Reading the current deprecation information.");

                if (!IsV3)
                {
                    _logger.LogError($"Deprecation information is only available on V3 package sources. Specify {OverwriteOption} to skip checking current deprecation information.");
                    return 1;
                }

                await FilterOutDeprecatedVersionsAsync(versionsToDeprecate, sourceRepository, cacheContext, context.GetCancellationToken());
            }

            if (versionsToDeprecate.Count == 0)
            {
                _logger.LogWarning($"No versions will be deprecated. Check the parameters and logs (perhaps with {PackageLifeCycleCommand.LogLevelOption} {{LogLevel}}) if this is not expected.", LogLevel.Debug);
                return 0;
            }
            else
            {
                _logger.LogInformation("{Count} versions will be marked as deprecated.", versionsToDeprecate.Count);
            }

            if (PackagePublishUrl is null)
            {
                PackagePublishUrl = await GetPackagePublishUrlAsync(sourceRepository);
                if (PackagePublishUrl is null)
                {
                    return 1;
                }
            }

            _logger.LogInformation("Submitting the deprecation request for {Id} to {PackagePublishUrl}.", Package_Id, PackagePublishUrl);

            if (!DryRun)
            {
                if (!await DeprecateAsync(versionsToDeprecate, sourceRepository, context.GetCancellationToken()))
                {
                    return 1;
                }
            }
            else
            {
                _logger.LogWarning($"The deprecation request was skipped because {DryRunOption} is enabled.");
            }

            await LogPackageDetailsUrlAsync(versionsToDeprecate.Last(), sourceRepository);

            return 0;
        }

        private async Task LogPackageDetailsUrlAsync(string version, SourceRepository sourceRepository)
        {
            var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            var template = serviceIndex?.GetServiceEntryUri(ServiceTypes.PackageDetailsUriTemplate);
            if (template is not null)
            {
                _logger.LogInformation("Check the package details page to view the change: {Url}",
                    template.OriginalString.Replace("{id}", Package_Id).Replace("{version}", version));
            }
        }

        private async Task<string?> GetPackagePublishUrlAsync(SourceRepository sourceRepository)
        {
            var serviceIndex = await sourceRepository.GetResourceAsync<ServiceIndexResourceV3>();
            var publishUri = serviceIndex.GetServiceEntryUri(ServiceTypes.PackagePublish);
            if (publishUri is null)
            {
                _logger.LogError($"No PackagePublish resource was found in the service index. Try providing the {PackagePublishUrlOption} directly, if you know it.");
                return null;
            }

            return publishUri.AbsoluteUri;
        }

        private bool AreArgumentsValid()
        {
            if (Version is not null)
            {
                foreach (var version in Version)
                {
                    if (!NuGetVersion.TryParse(version, out _))
                    {
                        _logger.LogError($"The {VersionOption} '{{Version}}' is not a valid version string.", version);
                        return false;
                    }
                }
            }

            if (Range is not null)
            {
                foreach (var range in Range.ToList())
                {
                    if (!VersionRange.TryParse(range, out _))
                    {
                        _logger.LogError($"The {RangeOption} '{Range}' is not a valid version range.", range);
                        return false;
                    }
                }
            }

            if (!Legacy && !CriticalBugs && !OtherReason)
            {
                OtherReason = true;
                _logger.LogDebug($"Defaulting to deprecation reason {OtherReasonOption} since no other deprecation reason was provided.");
            }

            if (OtherReason && string.IsNullOrWhiteSpace(Message))
            {
                _logger.LogError($"No message was provided but deprecation reason {OtherReasonOption} is being used.");
                return false;
            }

            if (AlternateVersion is not null && !NuGetVersion.TryParse(AlternateVersion, out _))
            {
                _logger.LogError($"The {AlternateVersionOption} '{{AlternateVersion}}' is not a valid version string.", AlternateVersion);
                return false;
            }

            if (string.IsNullOrWhiteSpace(ApiKey) && (IsNuGetOrg(PackagePublishUrl, out var host) || IsNuGetOrg(Source, out host)))
            {
                _logger.LogError($"An {ApiKeyOption} is required when deprecating packages on {{Host}}.", host);
                return false;
            }

            if (!All && (Version is null || Version.Count == 0) && (Range is null || Range.Count == 0))
            {
                _logger.LogError($"You must specify a {VersionOption} option, {RangeOption}, or {AllOption} in order to select to versions to deprecate.");
                return false;
            }

            return true;
        }

        private async Task<bool> IsAlternatePackageValidAsync(SourceRepository sourceRepository, SourceCacheContext cacheContext, CancellationToken token)
        {
            var findPackageByIdResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>(token);
            var versions = await findPackageByIdResource.GetAllVersionsAsync(AlternateId, cacheContext, _httpLogger, token);

            if (versions is null || !versions.Any())
            {
                _logger.LogError("The alternate package {Id} does not exist.", AlternateId);
                return false;
            }

            if (AlternateVersion is not null)
            {
                var parsedVersion = NuGetVersion.Parse(AlternateVersion);
                if (!versions.Contains(parsedVersion))
                {
                    _logger.LogError("The version {Version} for alternate package {Id} does not exist.", AlternateVersion, AlternateId);
                }
            }

            return true;
        }

        private async Task<List<string>?> GetMatchingVersionsAsync(SourceRepository sourceRepository, SourceCacheContext cacheContext, CancellationToken token)
        {
            var findPackageByIdResource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>(token);
            var versions = await findPackageByIdResource.GetAllVersionsAsync(Package_Id, cacheContext, _httpLogger, token);
            var remainingVersions = versions.ToHashSet();

            if (remainingVersions.Count == 0)
            {
                if (!AllowMissingVersions)
                {
                    _logger.LogError($"No versions were found for package {{Id}}. If the package is not yet available on the package source, you can try {AllowMissingVersionsOption} and providing explicit {VersionOption} options.", Package_Id);
                    return null;
                }
            }

            var versionsToDeprecate = new List<(string Version, (string Filter, string? Value, bool Display) Filter)>();
            var unusedFilters = new List<(string Filter, string? Value, bool Display)>();

            if (All)
            {
                var filter = (AllOption, (string?)null, true);
                foreach (var version in remainingVersions)
                {
                    versionsToDeprecate.Add((version.ToNormalizedString(), filter));
                }

                if (remainingVersions.Count == 0)
                {
                    unusedFilters.Add(filter);
                }

                remainingVersions.Clear();
            }

            if (Range is not null)
            {
                foreach (var range in Range)
                {
                    if (VersionRange.TryParse(range, out var parsedRange))
                    {
                        var filter = ($"{RangeOption} '{{Range}}'", range, true);
                        var matchingVersions = remainingVersions.Where(parsedRange.Satisfies).ToList();
                        remainingVersions.ExceptWith(matchingVersions);
                        foreach (var version in matchingVersions)
                        {
                            versionsToDeprecate.Add((version.ToNormalizedString(), filter));
                        }

                        if (matchingVersions.Count == 0)
                        {
                            unusedFilters.Add(filter);
                        }
                    }

                }
            }

            if (Version is not null)
            {
                foreach (var version in Version)
                {
                    if (NuGetVersion.TryParse(version, out var parsedVersion))
                    {
                        var matched = remainingVersions.Remove(parsedVersion);
                        var filter = ($"{VersionOption} '{{Version}}'", version, matched);
                        if (matched || AllowMissingVersions)
                        {
                            versionsToDeprecate.Add((version, filter));
                        }
                        else
                        {
                            unusedFilters.Add(filter);
                        }
                    }
                    else if (SkipValidation)
                    {
                        versionsToDeprecate.Add((version, ($"{VersionOption} '{{Version}}'", version, false)));
                    }
                }
            }

            foreach ((var version, var filter) in versionsToDeprecate.OrderBy(x => x.Version))
            {
                if (!filter.Display)
                {
                    continue;
                }

                if (filter.Value is not null)
                {
                    _logger.LogDebug("Input " + filter.Filter + " matched {Version}", filter.Value, version);
                }
                else
                {
                    _logger.LogDebug("Input " + filter.Filter + " matched {Version}", version);
                }
            }

            foreach (var filter in unusedFilters)
            {
               if (!filter.Display)
               {
                   continue;
               }

                if (filter.Value is not null)
                {
                    _logger.LogDebug("Input " + filter.Filter + " did not match any additional versions.", filter.Value);
                }
                else
                {
                    _logger.LogDebug("Input " + filter.Filter + " did not match any additional versions.");
                }
            }

            foreach (var version in remainingVersions.Order())
            {
                _logger.LogDebug("Version {Version} did not match any deprecation options and will be ignored.", version);
            }

            return versionsToDeprecate
                .Select(v =>
                {
                    if (NuGetVersion.TryParse(v.Version, out var parsed))
                    {
                        return (Parsed: (NuGetVersion?)parsed, String: parsed.ToNormalizedString());
                    }
                    else
                    {
                        return (null, v.Version.Trim());
                    }
                })
                .DistinctBy(x => x.String, StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Parsed)
                .ThenBy(x => x.String, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.String)
                .ToList();
        }

        private async Task FilterOutDeprecatedVersionsAsync(List<string> versionsToDeprecate, SourceRepository sourceRepository, SourceCacheContext cacheContext, CancellationToken token)
        {
            var packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>(token);
            var allMetadata = await packageMetadataResource.GetMetadataAsync(
                Package_Id,
                includePrerelease: true,
                includeUnlisted: true,
                cacheContext,
                _httpLogger,
                token);

            var versionToMetadata = allMetadata.ToDictionary(x => x.Identity.Version.ToNormalizedString(), StringComparer.OrdinalIgnoreCase);
            var skippedCount = 0;
            foreach (var version in versionsToDeprecate.ToList())
            {
                if (!versionToMetadata.TryGetValue(version, out var metadata))
                {
                    _logger.LogWarning("Version {Version} was not found in the package metadata. It's assumed to be not deprecated.", version);
                }
                else
                {
                    var deprecation = await metadata.GetDeprecationMetadataAsync();
                    if (deprecation is not null)
                    {
                        _logger.LogInformation("Version {Version} is already deprecated and will be skipped.", version);
                        versionsToDeprecate.Remove(version);
                        skippedCount++;
                    }
                }
            }

            if (skippedCount > 0)
            {
                _logger.LogInformation("{Count} package version(s) are already deprecated so they won't be updated.", skippedCount);
            }
        }

        private async Task<bool> DeprecateAsync(List<string> versionsToDeprecate, SourceRepository sourceRepository, CancellationToken token)
        {
            var request = new DeprecationRequest
            {
                Versions = versionsToDeprecate,
                IsLegacy = Legacy ? true : null,
                HasCriticalBugs = CriticalBugs ? true : null,
                IsOther = OtherReason ? true : null,
                Message = Message,
                AlternatePackageId = AlternateId,
                AlternatePackageVersion = AlternateVersion,
                Listed = Listed,
            };

            if (await _service.DeprecateAsync(PackagePublishUrl!, Package_Id!, ApiKey, request, Confirm, sourceRepository, token))
            {
                _logger.LogInformation("Successfully marked {Count} package versions as deprecated.", versionsToDeprecate.Count);
                return true;
            }

            return false;
        }

        private static bool IsNuGetOrg(string? url, out string? host)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            {
                host = null;
                return false;
            }

            host = parsed.Host;

            return parsed.Host.Equals("nuget.org", StringComparison.OrdinalIgnoreCase)
                || parsed.Host.EndsWith(".nuget.org", StringComparison.OrdinalIgnoreCase)
                || parsed.Host.EndsWith(".nugettest.org", StringComparison.OrdinalIgnoreCase);
        }
    }
}