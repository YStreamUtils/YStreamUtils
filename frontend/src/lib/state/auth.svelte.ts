import {
  Platform,
  type UserProfile,
} from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";
import {
  GetProfile,
  LoginPlatform,
} from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/authservice";

export const profiles = $state<Partial<Record<Platform, UserProfile | null>>>(
  {},
);

export const isLoadingProfile = $state<Partial<Record<Platform, boolean>>>({});
export const isLoggingIn = $state<Partial<Record<Platform, boolean>>>({});

export async function fetchProfile(platform: Platform) {
  isLoadingProfile[platform] = true;
  try {
    profiles[platform] = await GetProfile(platform);
  } catch (err) {
    console.error("Failed to recover profile:", err);
  } finally {
    isLoadingProfile[platform] = false;
  }
}

export async function handleConnect(platform: Platform) {
  isLoggingIn[platform] = true;
  try {
    const success = await LoginPlatform(platform);
    if (success) {
      await fetchProfile(platform);
    }
  } catch (err) {
    alert(`Authentication failed: ${err}`);
  } finally {
    isLoggingIn[platform] = false;
  }
}

export async function loadAllProfiles() {
  Object.entries(platforms).forEach(async ([platform, _]) => {
    await fetchProfile(platform as Platform);
  });
}

export const platforms: Partial<Record<Platform, string>> = {
  [Platform.PlatformYouTube]: "YouTube",
  //[Platform.PlatformTwitch]: "Twitch",
  //[Platform.PlatformKick]: "Kick",
};
