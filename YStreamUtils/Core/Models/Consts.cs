using System.Text.Json;
using Google.Apis.YouTube.v3;

namespace YStreamUtils.Core.Models;

public static class Consts
{
    public static string ApplicationDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "YStreamUtils");

    public static readonly IEnumerable<string> YoutubeScopes = [YouTubeService.Scope.YoutubeForceSsl];

    public static readonly JsonSerializerOptions IndentedSerializerOptions = new()
    {
        WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}