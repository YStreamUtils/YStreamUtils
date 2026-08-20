<script lang="ts">
  import { onMount } from "svelte";
  import AppDecorations from "./lib/components/AppDecorations.svelte";
  import Sidebar from "./lib/components/Sidebar.svelte";

  import { pages, router, sidebar } from "./lib/navigation.svelte";
  import { appState, initSettings } from "./lib/settings.svelte";

  import "@fontsource/jetbrains-mono";
  import "./lib/style.css";

  let ActiveScreen = $derived(pages[router.current]);

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
  <AppDecorations onMenuClicked={() => (sidebar.isOpen = !sidebar.isOpen)} />

  <div
    class="app-container"
    class:closed={!sidebar.isOpen}
    class:fullClosed={!sidebar.isOpen &&
      appState.settings?.UISettings.FullCloseSidebar}
  >
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
    grid-template-columns: 64px 1fr;
  }

  .app-container.fullClosed {
    grid-template-columns: 0px 1fr;
  }

  main {
    padding: var(--space-2);
    overflow-y: auto;

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
</style>
