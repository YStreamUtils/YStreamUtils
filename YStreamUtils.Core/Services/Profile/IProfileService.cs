using JetBrains.Annotations;

namespace YStreamUtils.Core.Services.Profile;

[UsedImplicitly]
public readonly record struct UserProfile(
    string DisplayName,
    string AvatarUrl,
    string ChannelId,
    string Handle
);

public interface IProfileService
{
    Task<UserProfile?> GetUserProfile(bool isBot);
}