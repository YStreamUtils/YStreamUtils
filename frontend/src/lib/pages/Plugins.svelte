<script lang="ts">
  import { onMount } from 'svelte';
  import { marked } from 'marked';
    import { FetchAllRegistryPlugins, DownloadAndInstallPlugin } from '../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/pluginservice';
    import { type PluginManifest } from '../../../bindings/github.com/ystreamutils/YStreamUtils-Plugin-Registry/ci/types';

  let plugins = $state<PluginManifest[] | null>([]);
  let selectedPlugin = $state<PluginManifest | null>(null);
  let readmeHtml = $state('');
  let activeTab = $state<string>('readme');
  let loadingList = $state(true);
  let loadingDetails = $state(false);
  let installStatus = $state<Record<string, string>>({});
  let showModal = $state(false);

  onMount(async () => {
    await refreshRegistry();
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

    const rawUrl = `https://githubusercontent.com/${selectedPlugin.Source.Owner}/${selectedPlugin.Source.Repository}/v${selectedPlugin.Version}/${fileName}`;

    try {
      const res = await fetch(rawUrl);
      if (res.status === 200) {
        const text = await res.text();
        readmeHtml = await marked.parse(text);
      } else {
        const fallbackUrl = `https://githubusercontent.com{selectedPlugin.source.owner}/${selectedPlugin.Source.Repository}/v${selectedPlugin.Version}/${fileName}`;
        const fallbackRes = await fetch(fallbackUrl);
        if (fallbackRes.status === 200) {
          const text = await fallbackRes.text();
          readmeHtml = await marked.parse(text);
        } else {
          readmeHtml = `No ${fileName} found.`;
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
      setTimeout(() => { installStatus[plugin.Name] = ''; }, 3000);
    } catch (err) {
      console.error(err);
      installStatus[plugin.Name] = 'error';
    }
  }
</script>

<div style=" padding: 20px;font-family: sans-serif;">
  <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px;">
    <h2>Plugin Marketplace</h2>
    <button onclick={refreshRegistry} disabled={loadingList}>Refresh</button>
  </div>

  {#if loadingList}
    <p>Loading registries...</p>
  {:else if plugins?.length === 0}
    <p>No plugins found.</p>
  {:else}
    <div style="display: grid; grid-template-columns: repeat(auto-fill, [content] minmax(280px, 1fr)); gap: 15px;">
      {#each plugins as plugin}
        <div style=" display: flex; flex-direction: column; justify-content: space-between; padding: 15px;border: 1px solid #ccc;">
          <div>
            <div style="display: flex; align-items: center; justify-content: space-between;">
              <strong>{plugin.Name}</strong>
              <span style="font-size: 0.85em; color: #666;">v{plugin.Version}</span>
            </div>
            <p style=" margin: 10px 0;font-size: 0.9em; color: #555;">{plugin.Documentation.Description}</p>
            <small style="color: #888;">By {plugin.Authors?.join(', ')}</small>
          </div>

          <div style=" display: flex; gap: 10px;margin-top: 15px;">
            <button onclick={() => openReadme(plugin)} style="flex-grow: 1;">View Docs</button>
            <button 
              onclick={() => installPlugin(plugin)} 
              disabled={installStatus[plugin.Name] === 'installing'} 
              style="flex-grow: 1;"
            >
              {#if installStatus[plugin.Name] === 'installing'}Installing...
              {:else if installStatus[plugin.Name] === 'success'}Installed!
              {:else if installStatus[plugin.Name] === 'error'}Error
              {:else}Install
              {/if}
            </button>
          </div>
        </div>
      {/each}
    </div>
  {/if}
</div>

<!-- Documentation Modal overlay -->
{#if showModal}
  <div style="position: fixed; top: 0; left: 0; z-index: 1000; display: flex; align-items: center; justify-content: center; width: 100vw; height: 100dvh; background: rgb(0 0 0 / 50%);">
    <div style=" display: flex; flex-direction: column; width: 600px; max-width: 90vw; max-height: 80vh; padding: 20px; overflow: hidden;background: white; border-radius: 4px;">
      
      <div style="display: flex; align-items: center; justify-content: space-between; padding-bottom: 10px; border-bottom: 1px solid #ccc;">
        <h3>{selectedPlugin?.Name} Documentation</h3>
        <button onclick={() => showModal = false} style=" font-size: 1.25em; cursor: pointer; background: none;border: none;">&times;</button>
      </div>

      <div style=" display: flex; gap: 10px;margin: 10px 0;">
        <button style="font-weight: {activeTab === 'readme' ? 'bold' : 'normal'}" onclick={() => switchTab('readme')}>README</button>
        <button style="font-weight: {activeTab === 'changelog' ? 'bold' : 'normal'}" onclick={() => switchTab('changelog')}>CHANGELOG</button>
      </div>

      <div style=" flex-grow: 1; padding-right: 5px;overflow-y: auto; line-height: 1.5; opacity: {loadingDetails ? 0.5 : 1};">
        {@html readmeHtml}
      </div>

    </div>
  </div>
{/if}
