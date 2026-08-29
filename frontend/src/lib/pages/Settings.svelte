<script lang="ts">
  import Expander from "../components/Expander.svelte";
  import ColorPicker from "../components/ColorPicker.svelte";
  import { User, Palette, Form } from "@lucide/svelte";
  import ThemeDropdown from "../components/ThemeDropdown.svelte";
  import { appState } from "../state/settings.svelte";
  import SettingsRow from "../components/SettingsRow.svelte";
  import ConnectedAccount from "../components/ConnectedAccount.svelte";
  import * as auth from "../state/auth.svelte";
  import { Platform } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";
  import Card from "../components/Card.svelte";
  import Input from "../components/Input.svelte";
  import Button from "../components/Button.svelte";
  import { TokenVault } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services";
  import { onMount } from "svelte";

  async function handleFormSubmit(
    e: SubmitEvent & { currentTarget: EventTarget & HTMLFormElement },
    platformKey: Platform,
    popoverId: string
  ): Promise<void> {
    e.preventDefault();
    const form = e.currentTarget;
    const formData = new FormData(form);

    const clientId = String(formData.get("clientId") ?? "").trim();
    const clientSecret = String(formData.get("clientSecret") ?? "").trim();

    console.log(`Saving config for ${platformKey}:`, { clientId, clientSecret });

    await TokenVault.StoreConfig(platformKey, {
      client_id: clientId,
      client_secret: clientSecret,
    });

    const dialog = document.getElementById(popoverId) as HTMLDialogElement | null;
    if (dialog) {
      dialog.close();
    }
    updateCounter++;
  }

  let updateCounter = $state(0);

  function checkConfig(platform: Platform, trigger: number) {
    return TokenVault.GetConfig(platform);
  }
</script>

{#if appState.settings}
  <div class="settings-page">
    <main class="settings-container">
      <header class="settings-header">
        <h1>Settings</h1>
        <p class="settings-desc">Manage your account preferences and application configuration.</p>
      </header>

      <div class="settings-stack">
        <Card>
          <Expander label="Linked Accounts" icon={User}>
            {#each Object.entries(auth.platforms) as [platform, display], index (platform)}
              {@const platformKey = platform as Platform}
              {@const popoverId = `popover-${platform}-oauth`}

              {#if index > 0}
                <hr class="settings-divider" />
              {/if}

              <Card variant="error">
                <Expander label="Youtube" icon={User}>
                  <div class="settings-stack">
                    <SettingsRow title={display || "Null?"}>
                      <ConnectedAccount
                        platform={platformKey}
                        isLoadingProfile={auth.isLoadingProfile[platformKey] || false}
                        profile={auth.profiles[platformKey]}
                        isLoggingIn={auth.isLoggingIn[platformKey] || false}
                        onConnect={() => auth.handleConnect(platformKey)}
                      />
                    </SettingsRow>
                    <SettingsRow title="OAuth2 Config">
                      {#await checkConfig(platformKey, updateCounter)}
                        <Button disabled>Checking Config...</Button>
                      {:then value}
                        {#if value !== null}
                          <Button commandfor={popoverId} command="show-modal" variant="error">
                            Change Client and Secret
                          </Button>
                        {:else}
                          <Button commandfor={popoverId} command="show-modal">Set Client and Secret</Button>
                        {/if}
                      {/await}
                    </SettingsRow>

                    <dialog id={popoverId} class="fullscreen-dialog" closedby="any">
                      <Card style=" width: 100%;padding: var(--space-8);">
                        <form
                          class="settings-stack"
                          onsubmit={async (e) => await handleFormSubmit(e, platformKey, popoverId)}
                        >
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

        <Card>
          <Expander label="Appearance" icon={Palette}>
            <SettingsRow title="Theme" description="Changes the theme between dark, light, and system.">
              <ThemeDropdown bind:value={appState.settings.UISettings.Theme} />
            </SettingsRow>
            <hr class="settings-divider" />
            <SettingsRow title="Accent Color" description="Sets the main accent color.">
              <ColorPicker bind:value={appState.settings.UISettings.Color} />
            </SettingsRow>
            <hr class="settings-divider" />
            <SettingsRow title="Fully Close Sidebar?" description="Toggles whether the sidebar should fully close.">
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
    gap: var(--space-4);
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
