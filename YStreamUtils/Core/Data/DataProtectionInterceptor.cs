using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YStreamUtils.Core.Entities;

namespace YStreamUtils.Core.Data;

public class DataProtectionInterceptor(ILogger logger, IDataProtectionProvider provider)
    : SaveChangesInterceptor, IMaterializationInterceptor
{
    private readonly IDataProtector _protector = provider.CreateProtector("YStreamUtils.OAuthConfig.Secrets.v1");
    public object InitializedInstance(MaterializationInterceptionData materializationData, object instance)
    {
        if (instance is not OAuthConfig config || string.IsNullOrEmpty(config.ClientSecret)) return instance;
        try
        {
            config.ClientSecret = _protector.Unprotect(config.ClientSecret);
        }
        catch (System.Security.Cryptography.CryptographicException e)
        {
            logger.LogError(e, "Failed to encrypt/decrypt");
        }
        return instance;
    }
    
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        EncryptSecrets(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        EncryptSecrets(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void EncryptSecrets(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<OAuthConfig>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            if (!string.IsNullOrEmpty(entry.Entity.ClientSecret))
            {
                entry.Entity.ClientSecret = _protector.Protect(entry.Entity.ClientSecret);
            }
        }
    }
}