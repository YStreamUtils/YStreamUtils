<script lang="ts">
  import Button from "./Button.svelte";
  import { House, Tv, Settings, FileTerminal, Component, Home, Puzzle } from "@lucide/svelte";
  import { navigateTo, router, sidebar, pages } from "../state/navigation.svelte";
</script>

<div id="sidebar" class="sidebar">
  <nav class="top-nav">
    {@render navButton(House, "home", "Home")}
    {@render navButton(Tv, "stream", "Stream")}
    {@render navButton(FileTerminal, "script", "Script")}
  </nav>

  <div class="bottom-nav">
    {@render navButton(Puzzle, "plugins", "Plugins")}
    {@render navButton(Settings, "settings", "Settings")}
  </div>
</div>

{#snippet navButton(icon: typeof House, path: keyof typeof pages, title: string)}
  <Button
    variant="transparent"
    align="left"
    {icon}
    fullWidth={true}
    active={router.current === path}
    onclick={() => navigateTo(path)}
  >
    <p class:hidden={!sidebar.isOpen}>{title}</p>
  </Button>
{/snippet}

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
