using Microsoft.AspNetCore.Http;
using YStreamUtils.Endpoints;

namespace YStreamUtils.Core.Services.Profile;

public interface IProfileService
{
    Task<UserProfile?> GetUserProfile(bool isBot);
}