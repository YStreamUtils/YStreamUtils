<script lang="ts">
  import { onMount } from "svelte";
  import AppDecorations from "./lib/components/AppDecorations.svelte";
  import Sidebar from "./lib/components/Sidebar.svelte";

  import { pages, router, sidebar } from "./lib/navigation.svelte";
  import { appState, initSettings } from "./lib/settings.svelte";

  import "@fontsource/jetbrains-mono";
  import "./lib/style.css";
  import { loadAllProfiles } from "./lib/auth.svelte";
    import { InitStreamState } from "./lib/streamState.svelte";

  let ActiveScreen = $derived(pages[router.current]);
  $effect(() => {
    sessionStorage.setItem("route", router.current);
  });

  $effect(() => {
    sessionStorage.setItem("sidebarOpen", JSON.stringify(sidebar.isOpen));
  });

  onMount(async () => {
    await initSettings();
    await loadAllProfiles();
    await InitStreamState();
  });

  $effect(() => {
    if (appState.settings?.UISettings) {
      const { Color, Theme } = appState.settings.UISettings;
      document.documentElement.style.setProperty("--color-brand", Color);
      document.documentElement.setAttribute("data-theme", Theme);
    }
  });
</script>

<div class="window-wrapper">
  <AppDecorations onMenuClicked={() => (sidebar.isOpen = !sidebar.isOpen)} />

  <div
    class="app-container"
    class:closed={!sidebar.isOpen}
    class:full-closed={!sidebar.isOpen &&
      appState.settings?.UISettings.FullCloseSidebar}
  >
    <Sidebar />
    <main class="main-content">
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
    flex: 1;
    grid-template-columns: [sidebar] 200px [main] minmax(0, 1fr);
    min-height: 0;
  }

  .app-container.closed {
    grid-template-columns: [sidebar] 64px [main] minmax(0, 1fr);
  }

  .app-container.full-closed {
    grid-template-columns: [sidebar] 0 [main] minmax(0, 1fr);
  }

  .main-content {
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
    background-repeat: no-repeat;
  }

  @media (prefers-reduced-motion: no-preference) {
    .app-container {
      transition: grid-template-columns 0.3s ease-in-out;
    }
  }
</style>
