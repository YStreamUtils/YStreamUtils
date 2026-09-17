using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace YStreamUtils.Core.Services;

public class TenantContext(IHttpContextAccessor httpContextAccessor, string fallbackTenant = "desktop")
{
    public string TenantId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return !string.IsNullOrEmpty(userId) ? userId : fallbackTenant;
        }
    }
}