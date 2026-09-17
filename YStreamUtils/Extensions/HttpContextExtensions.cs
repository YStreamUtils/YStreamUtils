using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using YStreamUtils.Core.Services;

namespace YStreamUtils.Extensions;

public static class HttpContextExtensions
{
    public static TenantContext GetTenantContext(this IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var tenantContext = httpContext?.RequestServices.GetRequiredService<TenantContext>();
        
        return tenantContext ?? throw new InvalidOperationException($"No tenant context found for {httpContext?.User.Identity?.Name}");
    }
}