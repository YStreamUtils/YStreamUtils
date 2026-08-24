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

  let isLoadingProfile = $state(true);
  let isLoggingIn = $state(false);

  onMount(async () => {
    await fetchProfile();
  });

  async function fetchProfile() {
    isLoadingProfile = true;
    try {
      profiles["youtube"] = await GetProfile(Platform.Youtube);
    } catch (err) {
      console.error("Failed to recover profile:", err);
    } finally {
      isLoadingProfile = false;
    }
  }

  async function handleConnect() {
    isLoggingIn = true;
    try {
      const success = await LoginPlatform(Platform.Youtube);
      if (success) {
        await fetchProfile();
      }
    } catch (err) {
      alert(`Authentication failed: ${err}`);
    } finally {
      isLoggingIn = false;
    }
  }

  // Define platforms from OAuth configs
  const platforms = [
    {
      id: "youtube",
      title: "YouTube",
    },
  ];
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
            {#each platforms as platform}
              <SettingsRow title={platform.title}>
                <ConnectedAccount
                  platform={platform.id}
                  {isLoadingProfile}
                  profile={profiles[platform.id]}
                  {isLoggingIn}
                  onConnect={handleConnect}
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
    display: flex;
    justify-content: center;
    width: 100%;
    height: 100%;
    padding: 2rem 1rem;
    box-sizing: border-box;
    border-radius: var(--space-1);
  }

  .settings-container {
    width: 100%;
    max-width: 44rem;
    display: flex;
    flex-direction: column;
    gap: 2rem;
  }

  .settings-header h1 {
    font-size: 1.75rem;
    font-weight: 600;
    margin: 0 0 0.5rem 0;
  }

  .settings-desc {
    font-size: 0.875rem;
    margin: 0;
    color: light-dark(var(--neutral-600), var(--neutral-400-tint));
  }

  .settings-stack {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }

  .settings-divider {
    border: none;
    height: 1px;
    margin: var(--space-4) var(--space-0);
    background-color: light-dark(var(--neutral-200), var(--neutral-800-tint));
    width: 100%;
  }

  .expander {
    background: light-dark(
      rgba(245, 245, 247, 0.7),
      color-mix(in srgb, var(--neutral-950) 65%, transparent)
    );
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
    border: 1px solid
      light-dark(
        rgba(0, 0, 0, 0.08),
        color-mix(in srgb, var(--neutral-800-tint) 40%, transparent)
      );
    border-radius: var(--space-2);
    box-shadow: 0 4px 30px rgba(0, 0, 0, 0.03);
    transition:
      border-color 0.2s ease,
      box-shadow 0.2s ease;
  }

  .expander:hover {
    border-color: light-dark(
      rgba(0, 0, 0, 0.15),
      color-mix(in srgb, var(--color-brand) 40%, transparent)
    );
    box-shadow: 0 4px 30px rgba(0, 0, 0, 0.06);
  }

  .toggle-checkbox {
    width: 20px;
    height: 20px;
    cursor: pointer;
  }
</style>
