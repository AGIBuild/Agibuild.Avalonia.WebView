using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Agibuild.Fulora.UnitTests;

public sealed class SpaAssetHotUpdateTests
{
    [Fact]
    public async Task Install_signed_package_and_activate_sets_active_directory()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SpaAssetHotUpdateService(root);
            var package = CreatePackage(new Dictionary<string, string>
            {
                ["index.html"] = "<html>v1</html>",
                ["assets/app.js"] = "console.log('v1');"
            });

            using var rsa = RSA.Create(2048);
            var signature = rsa.SignData(package, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var install = await service.InstallSignedPackageAsync(
                new MemoryStream(package),
                "1.0.0",
                signature,
                rsa,
                TestContext.Current.CancellationToken);
            var activate = service.ActivateVersion("1.0.0");

            Assert.True(install.Succeeded);
            Assert.Equal("installed", install.Code);
            Assert.True(activate.Succeeded);
            Assert.Equal("activated", activate.Code);

            var activeDirectory = service.GetActiveAssetDirectory();
            Assert.NotNull(activeDirectory);
            Assert.True(File.Exists(Path.Combine(activeDirectory!, "index.html")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Invalid_signature_is_rejected_without_activation_change()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SpaAssetHotUpdateService(root);
            var package = CreatePackage(new Dictionary<string, string>
            {
                ["index.html"] = "<html>bad-signature</html>"
            });

            using var rsa = RSA.Create(2048);
            var invalidSignature = new byte[256];
            Random.Shared.NextBytes(invalidSignature);

            var install = await service.InstallSignedPackageAsync(
                new MemoryStream(package),
                "1.0.1",
                invalidSignature,
                rsa,
                TestContext.Current.CancellationToken);

            Assert.False(install.Succeeded);
            Assert.Equal("signature-invalid", install.Code);
            Assert.Null(service.GetActiveAssetDirectory());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Rollback_restores_previous_active_version()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SpaAssetHotUpdateService(root);
            using var rsa = RSA.Create(2048);

            var v1 = CreatePackage(new Dictionary<string, string> { ["index.html"] = "<html>v1</html>" });
            var s1 = rsa.SignData(v1, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            await service.InstallSignedPackageAsync(new MemoryStream(v1), "1.0.0", s1, rsa, TestContext.Current.CancellationToken);
            service.ActivateVersion("1.0.0");

            var v2 = CreatePackage(new Dictionary<string, string> { ["index.html"] = "<html>v2</html>" });
            var s2 = rsa.SignData(v2, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            await service.InstallSignedPackageAsync(new MemoryStream(v2), "2.0.0", s2, rsa, TestContext.Current.CancellationToken);
            service.ActivateVersion("2.0.0");

            var rollback = service.Rollback();
            var activeDirectory = service.GetActiveAssetDirectory();
            var html = File.ReadAllText(Path.Combine(activeDirectory!, "index.html"));

            Assert.True(rollback.Succeeded);
            Assert.Equal("rolled-back", rollback.Code);
            Assert.Equal("1.0.0", rollback.Version);
            Assert.Equal("<html>v1</html>", html);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Spa_hosting_can_serve_active_external_assets()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SpaAssetHotUpdateService(root);
            var package = CreatePackage(new Dictionary<string, string>
            {
                ["index.html"] = "<html>external</html>",
                ["assets/app.js"] = "console.log('external');"
            });

            using var rsa = RSA.Create(2048);
            var signature = rsa.SignData(package, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            await service.InstallSignedPackageAsync(
                new MemoryStream(package),
                "3.0.0",
                signature,
                rsa,
                TestContext.Current.CancellationToken);
            service.ActivateVersion("3.0.0");

            using var hosting = new SpaHostingService(
                new SpaHostingOptions
                {
                    Scheme = "app",
                    Host = "localhost",
                    FallbackDocument = "index.html",
                    ActiveAssetDirectoryProvider = service.GetActiveAssetDirectory
                },
                NullTestLogger.Instance);

            var request = new WebResourceRequestedEventArgs(new Uri("app://localhost/index.html"), "GET");
            var handled = hosting.TryHandle(request);
            Assert.True(handled);
            Assert.Equal(200, request.ResponseStatusCode);
            using var reader = new StreamReader(request.ResponseBody!, Encoding.UTF8, leaveOpen: false);
            var html = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
            Assert.Equal("<html>external</html>", html);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ActivateVersion_returns_not_installed_for_missing_version()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SpaAssetHotUpdateService(root);
            var result = service.ActivateVersion("missing");

            Assert.False(result.Succeeded);
            Assert.Equal("version-not-installed", result.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Rollback_without_previous_version_returns_expected_code()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SpaAssetHotUpdateService(root);
            var result = service.Rollback();

            Assert.False(result.Succeeded);
            Assert.Equal("no-previous-version", result.Code);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Rollback_returns_previous_missing_when_previous_directory_removed()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SpaAssetHotUpdateService(root);
            using var rsa = RSA.Create(2048);

            var v1 = CreatePackage(new Dictionary<string, string> { ["index.html"] = "<html>v1</html>" });
            var s1 = rsa.SignData(v1, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            await service.InstallSignedPackageAsync(new MemoryStream(v1), "1.0.0", s1, rsa, TestContext.Current.CancellationToken);
            service.ActivateVersion("1.0.0");

            var v2 = CreatePackage(new Dictionary<string, string> { ["index.html"] = "<html>v2</html>" });
            var s2 = rsa.SignData(v2, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            await service.InstallSignedPackageAsync(new MemoryStream(v2), "2.0.0", s2, rsa, TestContext.Current.CancellationToken);
            service.ActivateVersion("2.0.0");

            var previousDir = Path.Combine(root, "versions", "1.0.0");
            Directory.Delete(previousDir, recursive: true);

            var rollback = service.Rollback();
            Assert.False(rollback.Succeeded);
            Assert.Equal("previous-version-missing", rollback.Code);
            Assert.Equal("1.0.0", rollback.Version);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Active_asset_directory_returns_null_when_active_pointer_points_to_removed_directory()
    {
        var root = CreateTempDirectory();
        try
        {
            var service = new SpaAssetHotUpdateService(root);
            using var rsa = RSA.Create(2048);
            var v1 = CreatePackage(new Dictionary<string, string> { ["index.html"] = "<html>v1</html>" });
            var s1 = rsa.SignData(v1, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            await service.InstallSignedPackageAsync(new MemoryStream(v1), "1.0.0", s1, rsa, TestContext.Current.CancellationToken);
            service.ActivateVersion("1.0.0");

            var activeDirectory = service.GetActiveAssetDirectory();
            Directory.Delete(activeDirectory!, recursive: true);

            Assert.Null(service.GetActiveAssetDirectory());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Spa_hosting_external_assets_returns_not_found_for_missing_active_directory()
    {
        var root = CreateTempDirectory();
        try
        {
            var hosting = new SpaHostingService(
                new SpaHostingOptions
                {
                    Scheme = "app",
                    Host = "localhost",
                    FallbackDocument = "index.html",
                    ActiveAssetDirectoryProvider = () => Path.Combine(root, "not-exists")
                },
                NullTestLogger.Instance);

            var request = new WebResourceRequestedEventArgs(new Uri("app://localhost/index.html"), "GET");
            var handled = hosting.TryHandle(request);

            Assert.True(handled);
            Assert.Equal(404, request.ResponseStatusCode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Spa_hosting_external_assets_blocks_path_traversal()
    {
        var root = CreateTempDirectory();
        try
        {
            var activeDir = Path.Combine(root, "active");
            Directory.CreateDirectory(activeDir);
            File.WriteAllText(Path.Combine(activeDir, "index.html"), "<html>safe</html>");

            var hosting = new SpaHostingService(
                new SpaHostingOptions
                {
                    Scheme = "app",
                    Host = "localhost",
                    FallbackDocument = "index.html",
                    ActiveAssetDirectoryProvider = () => activeDir
                },
                NullTestLogger.Instance);

            var method = typeof(SpaHostingService)
                .GetMethod("HandleViaExternalAssets", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var request = new WebResourceRequestedEventArgs(new Uri("app://localhost/index.html"), "GET");
            var handled = (bool)method!.Invoke(hosting, [request, "../../secret.txt"])!;
            var status = request.ResponseStatusCode;
            request.ResponseBody?.Dispose();

            Assert.True(handled);
            Assert.Equal(400, status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Spa_hosting_without_embedded_or_external_file_returns_not_found()
    {
        var root = CreateTempDirectory();
        try
        {
            var hosting = new SpaHostingService(
                new SpaHostingOptions
                {
                    Scheme = "app",
                    Host = "localhost",
                    FallbackDocument = "index.html",
                    ActiveAssetDirectoryProvider = () => "   "
                },
                NullTestLogger.Instance);

            var request = new WebResourceRequestedEventArgs(new Uri("app://localhost/asset.js"), "GET");
            var handled = hosting.TryHandle(request);

            Assert.True(handled);
            Assert.Equal(404, request.ResponseStatusCode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "fulora-hot-update-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static byte[] CreatePackage(IReadOnlyDictionary<string, string> files)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Key, CompressionLevel.Fastest);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(file.Value);
            }
        }

        return ms.ToArray();
    }
}
