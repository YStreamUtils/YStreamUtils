<script lang="ts">
  import AppDecorations from "./lib/components/AppDecorations.svelte";
  import Sidebar from "./lib/components/Sidebar.svelte";

  import { pages, router } from "./lib/navigation.svelte";

  let ActiveScreen = $derived(pages[router.current]);
  let sidebarOpen = $state<boolean>(true);
</script>

<div class="window-wrapper">
  <!-- Titlebar defines its own height, Flexbox measures it automatically -->
  <AppDecorations onMenuClicked={() => (sidebarOpen = !sidebarOpen)} />

  <!-- Container takes exactly 100% of whatever height is left over -->
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
    height: 100%; /* Spans exactly the locked 100vh body height */
  }

  .app-container {
    display: grid;
    grid-template-columns: 200px 1fr;
    flex: 1; /* Magic pill: Automatically fills remaining vertical space */
    min-height: 0; /* Layout guard: Prevents grid from expanding past its parent flexbox */
    transition: grid-template-columns 0.3s ease-in-out;
  }

  .app-container.closed {
    grid-template-columns: 45px 1fr;
  }

  main {
    padding: var(--space-2);
    overflow-y: auto; /* Clean separation: ONLY the main dashboard scrolls if long */
  }
</style>
