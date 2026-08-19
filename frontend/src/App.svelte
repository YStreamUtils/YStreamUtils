<script lang="ts">
  import AppDecorations from "./lib/types/components/AppDecorations.svelte";
  import Sidebar from "./lib/types/components/Sidebar.svelte";

  import { pages, router } from "./lib/types/navigation.svelte";

  let ActiveScreen = $derived(pages[router.current]);
</script>

<AppDecorations />
<Sidebar />

<main>
  <ActiveScreen />
</main>

<style>
  main {
    width: stretch;
    height: stretch;
    padding: var(--space-2);
  }
</style>
