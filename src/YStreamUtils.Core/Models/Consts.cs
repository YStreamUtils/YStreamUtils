using System.Text.Json;
using Google.Apis.YouTube.v3;

namespace YStreamUtils.Core.Models;

public static class Consts
{
    public static string ApplicationDataFolder => ApplicationEnvironment.CurrentPlatform == ApplicationPlatform.Docker
        ? DockerDataFolder
        : LocalDataFolder;

    private static string LocalDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YStreamUtils.Core");

    private static string DockerDataFolder => Environment.GetEnvironmentVariable("DATA_FOLDER") ?? "/data";
    public static readonly IEnumerable<string> YoutubeScopes = [YouTubeService.Scope.YoutubeForceSsl];

    public static readonly JsonSerializerOptions IndentedSerializerOptions = new()
    {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}