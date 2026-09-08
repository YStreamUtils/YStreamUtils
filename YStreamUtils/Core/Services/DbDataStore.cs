using System.Text.Json;
using Google.Apis.Util.Store;
using Microsoft.EntityFrameworkCore;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;
using YStreamUtils.Core.Models;
using YStreamUtils.Endpoints;

namespace YStreamUtils.Core.Services;

public class DbDataStore(AppDbContext dbContext) : IDataStore
{
    public async Task<T?> GetAsync<T>(string key)
    {
        var tokenRecord = await dbContext.OAuthTokens
            .FirstOrDefaultAsync(t => t.Platform == Platform.YouTube && t.IsBot == IsBot(key));
    
        return string.IsNullOrEmpty(tokenRecord?.TokenJson) ? default : JsonSerializer.Deserialize<T>(tokenRecord.TokenJson);
    }

    public async Task StoreAsync<T>(string key, T value)
    {
        if (value == null) return;
        var tokenRecord = await dbContext.OAuthTokens
            .FirstOrDefaultAsync(t => t.Platform == Platform.YouTube && t.IsBot == IsBot(key));

        if (tokenRecord == null)
        {
            tokenRecord = new OAuthToken { Platform = Platform.YouTube, IsBot = IsBot(key) };
            dbContext.OAuthTokens.Add(tokenRecord);
        }

        tokenRecord.TokenJson = JsonSerializer.Serialize(value);
        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync<T>(string key)
    {
        var tokenRecord = await dbContext.OAuthTokens
            .FirstOrDefaultAsync(t => t.Platform == Platform.YouTube && t.IsBot == IsBot(key));
        
        if (tokenRecord != null)
        {
            dbContext.OAuthTokens.Remove(tokenRecord);
            await dbContext.SaveChangesAsync();
        }
    }

    public Task ClearAsync() => Task.CompletedTask;

    private static bool IsBot(string key)
    {
        return key.EndsWith(":bot");
    }
}