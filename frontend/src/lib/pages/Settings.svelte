<script lang="ts">
  import Expander from "../components/Expander.svelte";
  import ColorPicker from "../components/ColorPicker.svelte";
  import { Shield, User, Palette } from "@lucide/svelte";
  import ThemeDropdown from "../components/ThemeDropdown.svelte";
  import { appState } from "../settings.svelte";
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
          <Expander label="Account Details" icon={User}>
            <div class="placeholder-content">
              Account inputs and profile settings go here.
            </div>
          </Expander>
        </div>

        <div class="expander">
          <Expander label="Privacy & Security" icon={Shield}>
            <div class="placeholder-content">
              Security triggers, 2FA, and password management go here.
            </div>
          </Expander>
        </div>

        <div class="expander">
          <Expander label="Appearance" icon={Palette}>
            <ThemeDropdown bind:value={appState.settings.UISettings.Theme} />

            <hr class="settings-divider" />

            <ColorPicker bind:value={appState.settings.UISettings.Color} />
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
    color: var(--color-text);

    background-color: light-dark(
      var(--color-background),
      var(--neutral-950-tint)
    );
    background-image: light-dark(
      none,
      radial-gradient(
        circle at 80% 80%,
        color-mix(in srgb, var(--color-brand) 10%, transparent),
        transparent 45%
      )
    );
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

  .placeholder-content {
    font-size: 0.875rem;
    color: var(--color-text);
    opacity: 0.8;
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
</style>
