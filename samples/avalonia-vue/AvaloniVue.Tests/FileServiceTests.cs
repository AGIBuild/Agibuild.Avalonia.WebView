using AvaloniVue.Bridge.Services;

namespace AvaloniVue.Tests;

public class FileServiceTests
{
    [Fact]
    public async Task ListFiles_returns_entries_for_temp_directory()
    {
        var service = new FileService();
        var tempDir = Path.GetTempPath();
        var entries = await service.ListFiles(tempDir);

        Assert.NotNull(entries);
        // temp directory always has at least some files
    }

    [Fact]
    public async Task ListFiles_returns_empty_for_nonexistent_path()
    {
        var service = new FileService();
        var entries = await service.ListFiles("/nonexistent/path/that/does/not/exist");

        Assert.Empty(entries);
    }

    [Fact]
    public async Task ListFiles_directories_come_before_files()
    {
        var service = new FileService();
        var entries = await service.ListFiles(Path.GetTempPath());

        if (entries.Count >= 2)
        {
            var firstFile = entries.FindIndex(e => !e.IsDirectory);
            var lastDir = entries.FindLastIndex(e => e.IsDirectory);
            if (firstFile >= 0 && lastDir >= 0)
            {
                Assert.True(lastDir < firstFile, "Directories should come before files");
            }
        }
    }

    [Fact]
    public async Task ReadTextFile_returns_content_for_existing_file()
    {
        var service = new FileService();
        var tmpFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmpFile, "Hello from test");

        try
        {
            var content = await service.ReadTextFile(tmpFile);
            Assert.Equal("Hello from test", content);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    [Fact]
    public async Task ReadTextFile_returns_error_for_missing_file()
    {
        var service = new FileService();
        var content = await service.ReadTextFile("/nonexistent/file.txt");

        Assert.StartsWith("Error:", content);
    }

    [Fact]
    public async Task GetUserDocumentsPath_returns_non_empty()
    {
        var service = new FileService();
        var path = await service.GetUserDocumentsPath();

        Assert.NotEmpty(path);
    }
}
