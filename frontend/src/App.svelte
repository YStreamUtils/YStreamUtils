<script lang="ts">
  import { onMount } from "svelte";
  import AppDecorations from "./lib/components/AppDecorations.svelte";
  import Sidebar from "./lib/components/Sidebar.svelte";

  import { pages, router } from "./lib/navigation.svelte";
  import { appState, initSettings } from "./lib/settings.svelte";

  let ActiveScreen = $derived(pages[router.current]);
  let sidebarOpen = $state<boolean>(true);

  onMount(async () => {
    await initSettings();
    console.log(appState);
  });

  $effect(() => {
    if (appState.settings?.UISettings) {
      const { Color, Theme } = appState.settings.UISettings;
      const cssScheme = Theme === "system" ? "light dark" : Theme;

      document.documentElement.style.setProperty("--color-brand", Color);
      document.documentElement.style.setProperty("color-scheme", cssScheme);
    }
  });
</script>

<div class="window-wrapper">
  <AppDecorations onMenuClicked={() => (sidebarOpen = !sidebarOpen)} />

  <div class="app-container" class:closed={!sidebarOpen}>
    <Sidebar />
    <main>
      <ActiveScreen />
    </main>
  </div>
</div>

<style>
  .window-wrapper {
    display: flex;
    flex-direction: column;
    width: 100%;
    height: 100%;
  }

  .app-container {
    display: grid;
    grid-template-columns: 200px 1fr;
    flex: 1;
    min-height: 0;
    transition: grid-template-columns 0.3s ease-in-out;
  }

  .app-container.closed {
    grid-template-columns: 45px 1fr;
  }

  main {
    padding: var(--space-2);
    overflow-y: auto;
  }
</style>
