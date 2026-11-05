using YStreamUtils.Core.Entities;

namespace YStreamUtils.Core.Services.Auth;

public interface IAuthService
{
    public Task AuthenticateAsync(OAuthConfig oAuthConfig, bool isBot, CancellationToken ct = default);
}