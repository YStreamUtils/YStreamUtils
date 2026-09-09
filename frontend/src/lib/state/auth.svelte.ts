import {
  type Platform as PlatformType,
  type UserProfile,
  Platform,
  youtubeLoginUrl,
  getProfile,
  saveAuthConfig,
  hasAuthConfig
} from '$lib/api';

export const profiles = $state<Partial<Record<PlatformType, UserProfile | null>>>({});

export const isLoadingProfile = $state<Partial<Record<PlatformType, boolean>>>({});
export const isLoggingIn = $state<Partial<Record<PlatformType, boolean>>>({});

export async function fetchProfile(platform: Platform) {
  isLoadingProfile[platform] = true;
  try {
    let profile = await getProfile({
      tenantId: 'user',
      platform: platform,
      isBot: false
    });
    if (profile.status === 200) {
      profiles[platform] = profile.data;
    }
  } catch (err) {
    console.error('Failed to recover profile:', err);
  } finally {
    isLoadingProfile[platform] = false;
  }
}

export async function handleConnect(platform: Platform) {
  isLoggingIn[platform] = true;
  try {
    const url = await youtubeLoginUrl({
      tenantId: 'user',
      role: 'user'
    });
    if (url.status === 200) {
      window.open(url.data.url, '_blank');
    }
    console.log('Login URL:', url);
  } catch (err) {
    console.error('Authentication failed:', err);
  } finally {
    isLoggingIn[platform] = false;
  }
}

export async function saveConfig(platform: Platform, clientId: string, clientSecret: string) {
  try {
    await saveAuthConfig({
      tenantId: 'user',
      platform: platform,
      clientId: clientId,
      clientSecret: clientSecret
    });
  } catch (err) {
    console.error('Failed to save auth config:', err);
  }
}

export async function loadAllProfiles() {
  Object.entries(platforms).forEach(async ([platform, _]) => {
    if (await hasConfig(platform as Platform)) await fetchProfile(platform as Platform);
  });
}

export async function hasConfig(platform: Platform): Promise<boolean> {
  const res = await hasAuthConfig({
    platform: platform
  });
  return res.data;
}

export const platforms: Partial<Record<PlatformType, string>> = {
  [Platform.YouTube]: 'YouTube',
  [Platform.Twitch]: 'Twitch',
  //[Platform.PlatformKick]: "Kick",
};
