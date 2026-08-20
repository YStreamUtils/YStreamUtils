<script lang="ts">
  import { router, type pages, sidebar } from "../navigation.svelte";
  import type { Component } from "svelte";
  import { House, Settings, Tv, type LucideProps } from "@lucide/svelte";

  function navigateTo(targetPage: keyof typeof pages) {
    router.current = targetPage;
  }
</script>

<div id="sidebar" class="sidebar">
  <nav class="top-nav">
    {@render navButton("home", "Home", House)}
    {@render navButton("stream", "Stream", Tv)}
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
      navigateTo(location);
    }}
  >
    {#if Icon}
      <span class="icon-wrapper">
        <Icon size={16} />
      </span>
    {/if}
    <p class:hidden={!sidebar.isOpen}>{label}</p>
  </button>
{/snippet}

<style>
  .sidebar {
    border: none;
    background: light-dark(var(--neutral-100-tint), var(--neutral-900-tint));
    color: var(--color-text);
    padding: var(--space-1_5);
    height: 100%;
    display: flex;
    flex-direction: column;
    box-sizing: border-box;
    box-shadow: 0 0 10px rgba(0, 0, 0, 0.2);
    overflow: hidden;
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

  .sidebar button {
    width: 100%;
    padding: var(--space-3) var(--space-4);
    border-radius: var(--space-1_5);
    justify-content: flex-start;
    display: grid;
    grid-template-columns: 16px 1fr;
    align-items: center;
    justify-items: start;
    gap: var(--space-1_5);
    overflow: hidden;
    cursor: pointer;

    background: transparent;
    border: 1px solid transparent;
    color: var(--color-text);

    transition:
      background-color 0.2s cubic-bezier(0.4, 0, 0.2, 1),
      border-color 0.2s cubic-bezier(0.4, 0, 0.2, 1),
      transform 0.1s ease;
  }

  .sidebar button:hover {
    background: light-dark(
      rgba(0, 0, 0, 0.03),
      color-mix(in srgb, var(--neutral-800-tint) 25%, transparent)
    );
    border-color: light-dark(
      rgba(0, 0, 0, 0.15),
      color-mix(in srgb, var(--color-brand) 40%, transparent)
    );
    backdrop-filter: blur(10px);
    -webkit-backdrop-filter: blur(10px);
  }

  .sidebar button.active {
    background: light-dark(
      rgba(255, 255, 255, 0.4),
      color-mix(in srgb, var(--neutral-950) 40%, transparent)
    );
    border-color: var(--color-brand);
    backdrop-filter: blur(20px);
    -webkit-backdrop-filter: blur(20px);
  }

  .sidebar button.active:hover {
    background: light-dark(
      rgba(255, 255, 255, 0.6),
      color-mix(in srgb, var(--neutral-950) 55%, transparent)
    );
    border-color: var(--color-brand);
    box-shadow: 0 0 8px color-mix(in srgb, var(--color-brand) 20%, transparent);
  }

  .sidebar button:active {
    transform: scale(0.98);
  }

  .icon-wrapper {
    flex-shrink: 0;
    width: 16px;
    height: 16px;
    justify-content: center;
    display: flex;
    align-items: center;
    color: light-dark(var(--neutral-500), var(--neutral-400-tint));
    transition: color 0.2s ease;
  }

  .sidebar button:hover .icon-wrapper,
  .sidebar button.active .icon-wrapper {
    color: var(--color-brand);
  }

  .sidebar button p {
    font-size: 0.875rem;
    font-family: inherit;
    line-height: 1;
    transition: 0.3s;
  }
</style>
