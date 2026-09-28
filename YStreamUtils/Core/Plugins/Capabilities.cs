namespace YStreamUtils.Core.Plugins;

public interface INetworkCapability
{
    HttpClient Client { get; }
}

public interface IFileReadCapability
{
    Task<string> ReadTextAsync(string relativePath);
}

public interface IFileWriteCapability
{
    Task WriteTextAsync(string relativePath, string content);
}