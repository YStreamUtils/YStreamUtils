<script lang="ts">
  import { router, type pages } from "../navigation.svelte";
  import type { Component } from "svelte";
  import { House, Settings } from "@lucide/svelte";

  function navigateTo(targetPage: keyof typeof pages) {
    router.current = targetPage;

    const sidebar = document.getElementById("my-sidebar-id");
    if (sidebar && sidebar.matches(":popover-open")) {
      sidebar.hidePopover();
    }
  }
</script>

<nav id="sidebar" popover="auto" class="sidebar">
  <h2>Flyout Menu</h2>
  <nav>
    {@render navButton("home", "Home", House)}
    {@render navButton("settings", "Settings", Settings, "bottom-start")}
  </nav>
</nav>

{#snippet navButton(
  location: keyof typeof pages,
  label: string,
  icon: any,
  extraClasses: string = "",
)}
  <button
    class="btn {extraClasses}"
    class:active={router.current === location}
    popovertarget="sidebar"
    popovertargetaction="hide"
    on:click={(e) => {
      navigateTo(location);
    }}
  >
    {#if icon}
      <svelte:component this={icon} size={16} />
    {/if}
    <p>{label}</p>
  </button>
{/snippet}

<style>
  .sidebar {
    margin: 0;
    border: none;
    position: fixed;
    top: var(--space-10);
    left: 0;
    width: 200px;
    height: calc(100vh - var(--space-10));
    background: light-dark(var(--neutral-300-tint), var(--neutral-700-tint));
    color: var(--color-text);
    padding: var(--space-3);
    box-sizing: border-box;
    transform: translateX(-100%);
    transition:
      transform 0.3s ease-in-out,
      display 0.3s ease-in-out allow-discrete;
      

    display: flex;
    flex-direction: column;
  }

  .sidebar nav {
    display: flex;
    flex-direction: column;
    gap: var(--space-2);
    margin-top: var(--space-2);

    /* 2. Force the nav to stretch and handle internal scrolling */
    flex-grow: 1;
    min-height: 0; /* Keeps the nav from bursting past its parent */
    overflow-y: auto; /* Adds vertical scroll ONLY if links overflow the screen */
  }

  .sidebar:popover-open {
    transform: translateX(0);
  }

  .sidebar::backdrop {
    background: rgba(0, 0, 0, 0.4);
    opacity: 0;
    transition:
      opacity 0.3s ease,
      display 0.3s ease allow-discrete;
  }

  .sidebar:popover-open::backdrop {
    opacity: 1;
  }

  .sidebar nav button {
    width: 100%;
    padding: var(--space-1) var(--space-2);
    border-radius: var(--space-1);
    justify-content: flex-start;
    gap: var(--space-1);
  }
  .bottom-start {
    margin-top: auto;
  }
</style>
