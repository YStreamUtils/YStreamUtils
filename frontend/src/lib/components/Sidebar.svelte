<script lang="ts">
  import { router, type pages, sidebar } from "../navigation.svelte";
  import type { Component } from "svelte";
  import { House, Settings, type LucideProps } from "@lucide/svelte";

  function navigateTo(targetPage: keyof typeof pages) {
    router.current = targetPage;
  }
</script>

<div id="sidebar" class="sidebar">
  <nav class="top-nav">
    {@render navButton("home", "Home", House)}
  </nav>

  <div class="bottom-nav">
    {@render navButton("settings", "Settings", Settings)}
  </div>
</div>

{#snippet navButton(
  location: keyof typeof pages,
  label: string,
  Icon: Component<LucideProps, {}, ""> | null = null,
  extraClasses: string = "",
)}
  <button
    class="btn {extraClasses}"
    class:active={router.current === location}
    onclick={() => {
      sidebar.isOpen = false;
      navigateTo(location);
    }}
  >
    {#if Icon}
      <span class="icon-wrapper">
        <Icon size={16} />
      </span>
    {/if}
    <p>{label}</p>
  </button>
{/snippet}

<style>
  .sidebar {
    border: none;
    background: light-dark(var(--neutral-300-tint), var(--neutral-700-tint));
    color: var(--color-text);
    padding: var(--space-1_5);
    height: 100%;
    display: flex;
    flex-direction: column;
    box-sizing: border-box;
    box-shadow: 0 0 10px rgba(0, 0, 0, 0.2);
  }

  .top-nav,
  .bottom-nav {
    display: flex;
    flex-direction: column;
    gap: var(--space-2);
  }

  .top-nav {
    flex-grow: 1;
    min-height: 0;
    overflow-y: auto;
  }

  .bottom-nav {
    flex-shrink: 0;
  }

  .sidebar button {
    width: 100%;
    padding: var(--space-1) var(--space-2);
    border-radius: var(--space-1);
    justify-content: flex-start;
    display: grid;
    grid-template-columns: 16px 1fr;
    justify-items: start;
    gap: var(--space-1_5);
    overflow: hidden;

    background: rgba(0, 0, 0, 0.1);
    border: var(--color-text) 1px solid;
    color: var(--color-text);
  }

  .icon-wrapper {
    flex-shrink: 0;
    justify-content: center;
    display: flex;
  }
</style>
