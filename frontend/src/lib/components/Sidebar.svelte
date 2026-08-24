<script lang="ts">
  import Button from './Button.svelte';
  import { House, Tv, Settings } from '@lucide/svelte';
  import { navigateTo, router, sidebar } from '../navigation.svelte';
</script>

<div id="sidebar" class="sidebar">
  <nav class="top-nav">
    <Button
      variant="surface"
      align="left"
      icon={House}
      fullWidth={true}
      class={router.current === 'home' ? 'active' : ''}
      onclick={() => navigateTo('home')}
    >
      <p class:hidden={!sidebar.isOpen}>Home</p>
    </Button>

    <Button
      variant="surface"
      align="left"
      icon={Tv}
      fullWidth={true}
      class={router.current === 'stream' ? 'active' : ''}
      onclick={() => navigateTo('stream')}
    >
      <p class:hidden={!sidebar.isOpen}>Stream</p>
    </Button>
  </nav>

  <div class="bottom-nav">
    <Button
      variant="surface"
      align="left"
      icon={Settings}
      fullWidth={true}
      class={router.current === 'settings' ? 'active' : ''}
      onclick={() => navigateTo('settings')}
    >
      <p class:hidden={!sidebar.isOpen}>Settings</p>
    </Button>
  </div>
</div>

<style>
  .sidebar {
    box-sizing: border-box;
    display: flex;
    flex-direction: column;
    height: 100%;
    padding: var(--space-1_5);
    overflow: hidden;
    color: var(--color-text);
    background: light-dark(var(--neutral-100-tint), var(--neutral-900-tint));
    border: none;
    box-shadow: 0 0 10px rgb(0 0 0 / 20%);
  }

  .top-nav,
  .bottom-nav {
    display: flex;
    flex-direction: column;
    gap: var(--space-1_5);
    overflow-x: hidden;
  }

  .top-nav {
    flex-grow: 1;
    min-height: 0;
    overflow-y: auto;
  }

  .bottom-nav {
    margin-top: auto;
  }
</style>
