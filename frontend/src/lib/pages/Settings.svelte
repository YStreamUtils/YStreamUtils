<script lang="ts">
  import Expander from "../components/Expander.svelte";
  import ColorPicker from "../components/ColorPicker.svelte";
  import { User, Palette } from "@lucide/svelte";
  import ThemeDropdown from "../components/ThemeDropdown.svelte";
  import { appState } from "../settings.svelte";
  import SettingsRow from "../components/SettingsRow.svelte";
  import ConnectedAccount from "../components/ConnectedAccount.svelte";
  import * as auth from "../auth.svelte";
  import { Platform } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";
  import Card from "../components/Card.svelte";
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
        <Card>
          <Expander label="Linked Accounts" icon={User}>
            {#each Object.entries(auth.platforms) as [platform, display], index (platform)}
              {@const platformKey = platform as Platform}

              {#if index > 0}
                <hr class="settings-divider" />
              {/if}

              <SettingsRow title={display || "Null?"}>
                <ConnectedAccount
                  platform={platformKey}
                  isLoadingProfile={auth.isLoadingProfile[platformKey] || false}
                  profile={auth.profiles[platformKey]}
                  isLoggingIn={auth.isLoggingIn[platformKey] || false}
                  onConnect={() => auth.handleConnect(platformKey)}
                />
              </SettingsRow>
            {/each}
          </Expander>
        </Card>

        <Card>
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
        </Card>
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

  .toggle-checkbox {
    width: 20px;
    height: 20px;
    cursor: pointer;
  }
</style>
