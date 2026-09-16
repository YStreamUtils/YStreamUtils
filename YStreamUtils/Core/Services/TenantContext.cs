using Microsoft.AspNetCore.Http;

namespace YStreamUtils.Core.Services;

public class TenantContext(IHttpContextAccessor httpContextAccessor, string fallbackTenant = "desktop")
{
    public string TenantId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            
            return context?.Request.Headers
                .TryGetValue("X-Tenant-ID", out var tenantId) == true ? tenantId.ToString() :
                fallbackTenant;
        }
    }
}