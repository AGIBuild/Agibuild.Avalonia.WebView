using System.IO.Compression;
using System.Security.Cryptography;

namespace Agibuild.Fulora;

/// <summary>
/// Result for signed SPA asset package operations.
/// </summary>
public sealed record SpaAssetHotUpdateResult(
    bool Succeeded,
    string Code,
    string? Version = null,
    string? DirectoryPath = null);

/// <summary>
/// Signed package installer + activation/rollback orchestrator for SPA assets.
/// </summary>
public sealed class SpaAssetHotUpdateService
{
    private readonly object _gate = new();
    private readonly string _rootDirectory;
    private readonly string _versionsDirectory;
    private readonly string _stateDirectory;
    private readonly string _activeVersionFile;
    private readonly string _previousVersionFile;

    /// <summary>
    /// Creates a hot-update service rooted at the given state directory.
    /// </summary>
    public SpaAssetHotUpdateService(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootDirectory);

        _rootDirectory = Path.GetFullPath(rootDirectory);
        _versionsDirectory = Path.Combine(_rootDirectory, "versions");
        _stateDirectory = Path.Combine(_rootDirectory, "state");
        _activeVersionFile = Path.Combine(_stateDirectory, "active-version.txt");
        _previousVersionFile = Path.Combine(_stateDirectory, "previous-version.txt");

        Directory.CreateDirectory(_rootDirectory);
        Directory.CreateDirectory(_versionsDirectory);
        Directory.CreateDirectory(_stateDirectory);
    }

    /// <summary>
    /// Installs a signed package into the version store without activating it.
    /// </summary>
    public async Task<SpaAssetHotUpdateResult> InstallSignedPackageAsync(
        Stream packageStream,
        string version,
        byte[] signature,
        RSA publicKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(packageStream);
        ArgumentException.ThrowIfNullOrEmpty(version);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentNullException.ThrowIfNull(publicKey);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedVersion = NormalizeVersion(version);
        using var ms = new MemoryStream();
        await packageStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var payload = ms.ToArray();

        var verified = publicKey.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        if (!verified)
        {
            return new SpaAssetHotUpdateResult(
                Succeeded: false,
                Code: "signature-invalid");
        }

        var stagingDirectory = Path.Combine(_rootDirectory, "staging", $"{normalizedVersion}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingDirectory);
        try
        {
            using var archiveStream = new MemoryStream(payload, writable: false);
            using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);
            archive.ExtractToDirectory(stagingDirectory);

            var targetVersionDirectory = Path.Combine(_versionsDirectory, normalizedVersion);
            lock (_gate)
            {
                if (Directory.Exists(targetVersionDirectory))
                    Directory.Delete(targetVersionDirectory, recursive: true);
                Directory.Move(stagingDirectory, targetVersionDirectory);
            }

            return new SpaAssetHotUpdateResult(
                Succeeded: true,
                Code: "installed",
                Version: normalizedVersion,
                DirectoryPath: targetVersionDirectory);
        }
        finally
        {
            if (Directory.Exists(stagingDirectory))
                Directory.Delete(stagingDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Atomically activates a previously installed version.
    /// </summary>
    public SpaAssetHotUpdateResult ActivateVersion(string version)
    {
        ArgumentException.ThrowIfNullOrEmpty(version);
        var normalizedVersion = NormalizeVersion(version);
        var targetDirectory = Path.Combine(_versionsDirectory, normalizedVersion);
        if (!Directory.Exists(targetDirectory))
        {
            return new SpaAssetHotUpdateResult(
                Succeeded: false,
                Code: "version-not-installed",
                Version: normalizedVersion);
        }

        lock (_gate)
        {
            var previous = ReadPointer(_activeVersionFile);
            WritePointerAtomic(_previousVersionFile, previous);
            WritePointerAtomic(_activeVersionFile, normalizedVersion);
        }

        return new SpaAssetHotUpdateResult(
            Succeeded: true,
            Code: "activated",
            Version: normalizedVersion,
            DirectoryPath: targetDirectory);
    }

    /// <summary>
    /// Rolls back active version to the previous activation pointer.
    /// </summary>
    public SpaAssetHotUpdateResult Rollback()
    {
        lock (_gate)
        {
            var previous = ReadPointer(_previousVersionFile);
            if (string.IsNullOrEmpty(previous))
            {
                return new SpaAssetHotUpdateResult(
                    Succeeded: false,
                    Code: "no-previous-version");
            }

            var previousDirectory = Path.Combine(_versionsDirectory, previous);
            if (!Directory.Exists(previousDirectory))
            {
                return new SpaAssetHotUpdateResult(
                    Succeeded: false,
                    Code: "previous-version-missing",
                    Version: previous);
            }

            WritePointerAtomic(_activeVersionFile, previous);
            WritePointerAtomic(_previousVersionFile, string.Empty);

            return new SpaAssetHotUpdateResult(
                Succeeded: true,
                Code: "rolled-back",
                Version: previous,
                DirectoryPath: previousDirectory);
        }
    }

    /// <summary>
    /// Gets active asset directory path for SPA hosting integration.
    /// Returns null when no active version is selected.
    /// </summary>
    public string? GetActiveAssetDirectory()
    {
        lock (_gate)
        {
            var active = ReadPointer(_activeVersionFile);
            if (string.IsNullOrEmpty(active))
                return null;

            var directory = Path.Combine(_versionsDirectory, active);
            return Directory.Exists(directory) ? directory : null;
        }
    }

    private static string NormalizeVersion(string version)
    {
        var normalized = version.Trim();
        if (normalized.Length == 0)
            throw new ArgumentException("Version cannot be empty.", nameof(version));
        return normalized;
    }

    private static string? ReadPointer(string path)
    {
        if (!File.Exists(path))
            return null;

        var value = File.ReadAllText(path).Trim();
        return value.Length == 0 ? null : value;
    }

    private static void WritePointerAtomic(string path, string? value)
    {
        var tempPath = $"{path}.tmp";
        File.WriteAllText(tempPath, value ?? string.Empty);
        File.Move(tempPath, path, overwrite: true);
    }
}
