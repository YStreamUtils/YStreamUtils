<script lang="ts">
  import { onMount } from 'svelte';
  import { marked } from 'marked';
  import {
    FetchAllRegistryPlugins,
    DownloadAndInstallPlugin,
    GetActivePlugins
  } from '../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/pluginservice';
  import { type PluginManifest } from '../../../bindings/github.com/ystreamutils/YStreamUtils-Plugin-Registry/ci/types';
  import Card from '../components/Card.svelte';
  import Button from '../components/Button.svelte';

  let plugins = $state<PluginManifest[] | null>([]);
  let selectedPlugin = $state<PluginManifest | null>(null);
  let readmeHtml = $state('');
  let activeTab = $state<string>('readme');
  let loadingList = $state(true);
  let loadingDetails = $state(false);
  let installStatus = $state<Record<string, string>>({});
  let showModal = $state(false);

  let installedPlugins = $state<string[]>([]);

  onMount(async () => {
    await refreshRegistry();
    const plugins = await GetActivePlugins();
    if (plugins != null) {
      installedPlugins = Object.keys(plugins);
    }
  });

  async function refreshRegistry() {
    loadingList = true;
    try {
      plugins = await FetchAllRegistryPlugins();
    } catch (err) {
      console.error(err);
    } finally {
      loadingList = false;
    }
  }

  async function openReadme(plugin: PluginManifest) {
    selectedPlugin = plugin;
    activeTab = 'readme';
    showModal = true;
    await fetchMarkdownContent('README.md');
  }

  async function switchTab(tab: string) {
    activeTab = tab;
    const fileName = tab === 'readme' ? 'README.md' : 'CHANGELOG.md';
    await fetchMarkdownContent(fileName);
  }

  async function fetchMarkdownContent(fileName: string) {
    if (!selectedPlugin) return;
    loadingDetails = true;
    readmeHtml = 'Loading...';

    const branches = ['refs/heads/main', 'refs/heads/master'];
    try {
      for (const branch of branches) {
        const rawUrl = `https://raw.githubusercontent.com/${selectedPlugin.Source.Owner}/${selectedPlugin.Source.Repository}/${branch}/${fileName}`;
        const res = await fetch(rawUrl);
        if (res.status === 200) {
          const text = await res.text();
          readmeHtml = await marked.parse(text);
        }
      }
    } catch (err) {
      readmeHtml = 'Failed to load content.';
    } finally {
      loadingDetails = false;
    }
  }

  async function installPlugin(plugin: PluginManifest) {
    installStatus[plugin.Name] = 'installing';
    try {
      await DownloadAndInstallPlugin(plugin);
      installStatus[plugin.Name] = 'success';
      setTimeout(() => {
        installStatus[plugin.Name] = '';
      }, 3000);
    } catch (err) {
      console.error(err);
      installStatus[plugin.Name] = 'error';
    }
  }

  async function isPluginInstalled(pluginName: string): Promise<boolean> {
    const plugins = await GetActivePlugins();
    if (plugins == null) return false;
    return Object.keys(plugins).filter((s) => s === pluginName).length > 0;
  }
</script>

<div>
  <div>
    <h2>Plugin Marketplace</h2>
    <button onclick={refreshRegistry} disabled={loadingList}>Refresh</button>
  </div>

  {#if loadingList}
    <p>Loading registries...</p>
  {:else if plugins?.length === 0}
    <p>No plugins found.</p>
  {:else}
    <div class="plugins-list">
      {#each plugins as plugin}
        <Card>
          <div class="plugin-card">
            <div>
              <strong>{plugin.Name}</strong>
              <span class="span">v{plugin.Version}</span>
            </div>
            <p>
              {plugin.Documentation.Description}
            </p>
            <small class="span">By {plugin.Authors?.join(', ')}</small>

            <div style="padding-top: var(--space-4);">
              <Button commandfor="plugin-modal" command="show-modal" onclick={() => (selectedPlugin = plugin)}>
                View Docs</Button
              >
              {#if installedPlugins.includes(plugin.Name.replaceAll('-', '_'))}
                <Button variant="success">Installed!</Button>
              {:else}
                <Button
                  onclick={() => installPlugin(plugin)}
                  disabled={installStatus[plugin.Name] === 'installing'}
                  variant={installStatus[plugin.Name] as
                    'accent' | 'surface' | 'success' | 'warning' | 'error' | 'transparent' | undefined}
                >
                  {#if installStatus[plugin.Name] === 'installing'}Installing...
                  {:else if installStatus[plugin.Name] === 'success'}Installed!
                  {:else if installStatus[plugin.Name] === 'error'}Error
                  {:else}Install
                  {/if}
                </Button>
              {/if}
            </div>
          </div>
        </Card>
      {/each}
    </div>
  {/if}
</div>

<dialog id="plugin-modal" class="fullscreen-dialog" closedby="any">
  <Card style=" width: 100%;padding: var(--space-8);">
    <div class="plugin-card">
      <div>
        <h3>{selectedPlugin?.Name} Documentation</h3>
        <Button commandfor="plugin-modal" command="request-close">&times;</Button>
      </div>

      <div>
        <Button onclick={() => switchTab('readme')}>README</Button>
        <Button onclick={() => switchTab('changelog')}>CHANGELOG</Button>
      </div>

      <div>
        {@html readmeHtml}
      </div>
    </div>
  </Card>
</dialog>

<style>
  .plugins-list {
    display: flex;
    flex-wrap: wrap;
    gap: var(--space-4);
  }

  .plugin-card {
    display: flex;
    flex-flow: column nowrap;
    gap: var(--space-2);
    width: fit-content;
    padding: var(--space-2);
  }

  .span {
    font-weight: 300;
    color: var(--color-text-muted);
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
</style>
