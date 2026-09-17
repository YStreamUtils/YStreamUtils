<script lang="ts">
  import Expander from '../components/base/Expander.svelte';
  import ColorPicker from '../components/ColorPicker.svelte';
  import { User, Palette } from '@lucide/svelte';
  import ThemeDropdown from '../components/ThemeDropdown.svelte';
  import SettingsRow from '../components/SettingsRow.svelte';
  import ConnectedAccount from '../components/ConnectedAccount.svelte';
  import * as auth from '../state/auth.svelte';
  import Card from '../components/base/Card.svelte';
  import Input from '../components/base/Input.svelte';
  import Button from '../components/base/Button.svelte';
  import { Platform } from '$lib/api';
  import { getSettingsState } from '$lib/state/settings.svelte';

  const settingsState = getSettingsState();

  async function handleFormSubmit(
    e: SubmitEvent & { currentTarget: EventTarget & HTMLFormElement },
    platformKey: Platform,
    popoverId: string
  ): Promise<void> {
    e.preventDefault();
    const form = e.currentTarget;
    const formData = new FormData(form);

    const clientId = String(formData.get('clientId') ?? '').trim();
    const clientSecret = String(formData.get('clientSecret') ?? '').trim();

    console.log(`Saving config for ${platformKey}:`, { clientId, clientSecret });

    await auth.saveConfig(platformKey, clientId, clientSecret);

    const dialog = document.getElementById(popoverId) as HTMLDialogElement | null;
    if (dialog) {
      dialog.close();
    }
    updateCounter++;
  }

  let updateCounter = $state(0);

  const platformColorMap = {
    [Platform.YouTube]: '#FF0000',
    [Platform.Twitch]: '#9146FF'
  };
</script>

{#if settingsState.settings}
  <div class="settings-page">
    <main class="settings-container">
      <header class="settings-header">
        <h1>Settings</h1>
      </header>

      <div class="settings-stack">
        <Card>
          <Expander label="Linked Accounts" icon={User}>
            {#each Object.entries(auth.platforms) as [platform, display], index (platform)}
              {@const platformKey = platform as Platform}
              {@const popoverId = `popover-${platform}-oauth`}

              <Card variant="custom" customColor={platformColorMap[platformKey] || ''}>
                <Expander label={platform} icon={User}>
                  <div class="settings-stack">
                    <SettingsRow title={display || 'Null?'}>
                      <ConnectedAccount
                        platform={platformKey}
                        isLoadingProfile={auth.isLoadingProfile[platformKey] || false}
                        profile={auth.profiles[platformKey]}
                        isLoggingIn={auth.isLoggingIn[platformKey] || false}
                        onConnect={() => auth.handleConnect(platformKey)} />
                    </SettingsRow>
                    <SettingsRow title="OAuth2 Config">
                      {#await auth.hasConfig(platformKey)}
                        <Button variant="surface" disabled>Checking config...</Button>
                      {:then hasConfig}
                        {#if hasConfig}
                          <Button variant="error" commandfor={popoverId} command="show-modal">
                            Change Client and Secret
                          </Button>
                        {:else}
                          <Button variant="success" commandfor={popoverId} command="show-modal">
                            Set Client and Secret
                          </Button>
                        {/if}
                      {:catch error}
                        <p>Error loading configuration: {error.message}</p>
                      {/await}
                    </SettingsRow>

                    <dialog id={popoverId} class="fullscreen-dialog" closedby="any">
                      <Card style=" width: 100%;padding: var(--space-8);">
                        <form
                          class="settings-stack"
                          onsubmit={async (e) => await handleFormSubmit(e, platformKey, popoverId)}>
                          <SettingsRow title="Client ID">
                            <Input name="clientId" type="text" placeholder="Client ID" align="left" />
                          </SettingsRow>
                          <SettingsRow title="Client Secret">
                            <Input name="clientSecret" type="password" placeholder="Client Secret" align="left" />
                          </SettingsRow>

                          <SettingsRow title="">
                            <Button variant="error" commandfor={popoverId} command="close">Cancel</Button>
                            <Button variant="success" commandfor={popoverId} command="close" type="submit">Save</Button>
                          </SettingsRow>
                        </form>
                      </Card>
                    </dialog>
                  </div>
                </Expander>
              </Card>
            {/each}
          </Expander>
        </Card>

        <!-- Changed bindings to map down into settingsState.settings.ui directly -->
        {#if settingsState.settings.ui}
          <Card>
            <Expander label="Appearance" icon={Palette}>
              <SettingsRow title="Theme" description="Changes the theme between dark, light, and system.">
                <ThemeDropdown bind:value={settingsState.settings.ui.theme} />
              </SettingsRow>
              <hr class="settings-divider" />
              <SettingsRow title="Accent Color" description="Sets the main accent color.">
                <ColorPicker bind:value={settingsState.settings.ui.color} />
              </SettingsRow>
              <hr class="settings-divider" />
              <SettingsRow title="Fully Close Sidebar?" description="Toggles whether the sidebar should fully close.">
                <input
                  type="checkbox"
                  class="toggle-checkbox"
                  bind:checked={settingsState.settings.ui.fullyCloseSidebar} />
              </SettingsRow>
            </Expander>
          </Card>
        {/if}
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

  .settings-stack {
    display: flex;
    flex-direction: column;
    gap: var(--space-4);
  }

  .settings-divider {
    width: 100%;
    height: 1px;
    margin: var(--space-1) var(--space-0);
    background-color: light-dark(var(--neutral-200), var(--neutral-800-tint));
    border: none;
  }

  .toggle-checkbox {
    width: 20px;
    height: 20px;
    cursor: pointer;
  }

  .fullscreen-dialog {
    position: fixed;
    inset: 0;
    width: 90%;
    max-width: 500px;
    height: max-content;
    margin: auto !important;
    background: transparent;
    border: none;
  }

  .fullscreen-dialog::backdrop {
    background-color: rgb(0 0 0 / 30%);
  }
</style>
