<script lang="ts">
  import Expander from "../components/Expander.svelte";
  import ColorPicker from "../components/ColorPicker.svelte";
  import { User, Palette } from "@lucide/svelte";
  import ThemeDropdown from "../components/ThemeDropdown.svelte";
  import { appState } from "../settings.svelte";
  import SettingsRow from "../components/SettingsRow.svelte";
  import ConnectedAccount from "../components/ConnectedAccount.svelte";
  import { onMount } from "svelte";
  import { profiles } from "../profiles.svelte";
  import {
    GetProfile,
    LoginPlatform,
  } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/authservice";
  import { Platform } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";

  let isLoadingProfile = $state<Partial<Record<Platform, boolean>>>({});
  let isLoggingIn = $state<Partial<Record<Platform, boolean>>>({});

  onMount(async () => {
    Object.values(Platform).forEach(async (p) => {
      await fetchProfile(p);
    });
  });

  async function fetchProfile(platform: Platform) {
    isLoadingProfile[platform] = true;
    try {
      profiles[platform] = await GetProfile(Platform.PlatformYouTube);
    } catch (err) {
      console.error("Failed to recover profile:", err);
    } finally {
      isLoadingProfile[platform] = false;
    }
  }

  async function handleConnect(platform: Platform) {
    isLoggingIn[platform] = true;
    try {
      const success = await LoginPlatform(Platform.PlatformYouTube);
      if (success) {
        await fetchProfile(platform);
      }
    } catch (err) {
      alert(`Authentication failed: ${err}`);
    } finally {
      isLoggingIn[platform] = false;
    }
  }

  const platforms: Partial<Record<Platform, string>> = {
    [Platform.PlatformYouTube]: "YouTube",
    [Platform.PlatformTwitch]: "Twitch",
    [Platform.PlatformKick]: "Kick",
  };
</script>

{#if appState.settings}
  <div class="settings-page">
    <main class="settings-container">
      <header class="settings-header">
        <h1>Settings</h1>
        <p class="settings-desc">
          Manage your account preferences and application configuration.
        </p>
      </header>

      <div class="settings-stack">
        <div class="expander">
          <Expander label="Linked Accounts" icon={User}>
            {#each Object.entries(platforms) as [platform, display]}
              {@const platformKey = platform as Platform}

              <SettingsRow title={display}>
                <ConnectedAccount
                  platform={platformKey}
                  isLoadingProfile={isLoadingProfile[platformKey] || false}
                  profile={profiles[platformKey]}
                  isLoggingIn={isLoggingIn[platformKey] || false}
                  onConnect={() => handleConnect(platformKey)}
                />
              </SettingsRow>
            {/each}
          </Expander>
        </div>

        <div class="expander">
          <Expander label="Appearance" icon={Palette}>
            <SettingsRow
              title="Theme"
              description="Changes the theme between dark, light, and system."
            >
              <ThemeDropdown bind:value={appState.settings.UISettings.Theme} />
            </SettingsRow>
            <hr class="settings-divider" />
            <SettingsRow
              title="Accent Color"
              description="Sets the main accent color."
            >
              <ColorPicker bind:value={appState.settings.UISettings.Color} />
            </SettingsRow>
            <hr class="settings-divider" />
            <SettingsRow
              title="Fully Close Sidebar?"
              description="Toggles whether the sidebar should fully close."
            >
              <input
                type="checkbox"
                class="toggle-checkbox"
                bind:checked={appState.settings.UISettings.FullCloseSidebar}
              />
            </SettingsRow>
          </Expander>
        </div>
      </div>
    </main>
  </div>
{/if}

<style>
  .settings-page {
    box-sizing: border-box;
    display: flex;
    flex-wrap: wrap;
    justify-content: center;
    width: 100%;
    height: 100%;
    padding: 2rem 1rem;
    border-radius: var(--space-1);
  }

  .settings-container {
    display: flex;
    flex-direction: column;
    gap: 2rem;
    width: 100%;
    max-width: 44rem;
  }

  .settings-header h1 {
    margin: 0 0 0.5rem;
    font-size: 1.75rem;
    font-weight: 600;
  }

  .settings-desc {
    margin: 0;
    font-size: 0.875rem;
    color: light-dark(var(--neutral-600), var(--neutral-400-tint));
  }

  .settings-stack {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .settings-divider {
    width: 100%;
    height: 1px;
    margin: var(--space-4) var(--space-0);
    background-color: light-dark(var(--neutral-200), var(--neutral-800-tint));
    border: none;
  }

  .expander {
    background: light-dark(
      rgb(245 245 247 / 70%),
      color-mix(in srgb, var(--neutral-950) 65%, transparent)
    );
    border: 1px solid
      light-dark(
        rgb(0 0 0 / 8%),
        color-mix(in srgb, var(--neutral-800-tint) 40%, transparent)
      );
    border-radius: var(--space-2);
    box-shadow: 0 4px 30px rgb(0 0 0 / 3%);
    backdrop-filter: blur(20px);
  }

  @media (prefers-reduced-motion: no-preference) {
    .expander {
      transition:
        border-color 0.2s ease,
        box-shadow 0.2s ease;
    }
  }

  @media (hover: hover) {
    .expander:hover {
      border-color: light-dark(
        rgb(0 0 0 / 15%),
        color-mix(in srgb, var(--color-brand) 40%, transparent)
      );
      box-shadow: 0 4px 30px rgb(0 0 0 / 6%);
    }
  }

  .toggle-checkbox {
    width: 20px;
    height: 20px;
    cursor: pointer;
  }
</style>
