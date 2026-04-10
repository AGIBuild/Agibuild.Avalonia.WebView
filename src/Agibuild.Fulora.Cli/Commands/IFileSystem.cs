namespace Agibuild.Fulora.Cli.Commands;

internal interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    string ReadAllText(string path);
    byte[] ReadAllBytes(string path);
    void WriteAllText(string path, string content);
    void CreateDirectory(string path);
    void DeleteDirectory(string path, bool recursive);
    void CopyFile(string sourcePath, string destinationPath, bool overwrite);
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
    string[] GetDirectories(string path);
}

internal sealed class RealFileSystem : IFileSystem
{
    internal static RealFileSystem Instance { get; } = new();

    private RealFileSystem()
    {
    }

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

    public void CopyFile(string sourcePath, string destinationPath, bool overwrite) => File.Copy(sourcePath, destinationPath, overwrite);

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        => Directory.Exists(path) ? Directory.GetFiles(path, searchPattern, searchOption) : [];

    public string[] GetDirectories(string path)
        => Directory.Exists(path) ? Directory.GetDirectories(path) : [];
}
