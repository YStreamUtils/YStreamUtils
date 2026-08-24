<script lang="ts">
  import Dropdown from "./Dropdown.svelte";
  import { Monitor, Moon, Sun, ChevronDown } from "@lucide/svelte";

  interface Props {
    value?: string;
  }

  let { value = $bindable("") }: Props = $props();
  let isOpen = $state(false);

  const themes = [
    { value: "light", label: "Light", icon: Sun },
    { value: "dark", label: "Dark", icon: Moon },
    { value: "", label: "System", icon: Monitor },
  ];

  const activeTheme = $derived(
    themes.find((t) => t.value === value) || themes[2],
  );
  const ActiveIcon = $derived(activeTheme.icon);
</script>

<Dropdown id="theme-menu" width="8.5rem">
  {#snippet trigger()}
    <button 
      class="btn btn-between select-trigger" 
      popovertarget="theme-menu"
      onclick={() => isOpen = !isOpen}
    >
      <span class="active-group">
        <ActiveIcon size={14} /> 
        {activeTheme.label}
      </span>
      <span class="chevron" class:rotated={isOpen}>
        <ChevronDown size={14} opacity="0.7" />
      </span>
    </button>
  {/snippet}

  {#snippet content(close)}
    {#each themes as theme}
      <button
        type="button"
        class="btn btn-start menu-btn"
        class:selected={value === theme.value}
        onclick={() => {
          value = theme.value;
          isOpen = false;
          close();
        }}
      >
        <theme.icon size={14} />
        {theme.label}
      </button>
    {/each}
  {/snippet}
</Dropdown>

<style>
  .select-trigger {
    gap: var(--space-4);
    min-width: 8.5rem;
    padding: var(--space-1_5) var(--space-3);
    font-size: 0.75rem;
    border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
    border-radius: var(--space-1);
  }

  .active-group {
    display: flex;
    flex-wrap: wrap;
    gap: var(--space-2);
    align-items: center;
  }

  .chevron {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
  }

  .chevron.rotated {
    transform: rotate(180deg);
  }

  .menu-btn {
    gap: var(--space-2);
    width: 100%;
    padding: var(--space-2) var(--space-3);
    font-size: 0.75rem;
    color: var(--_menu-color, inherit);
    background-color: var(--_menu-bg, transparent);
    border-radius: var(--space-1);
  }

  .menu-btn.selected {
    --_menu-color: #fff;
    --_menu-bg: var(--color-brand);
  }

  @media (hover: hover) {
    .menu-btn:hover {
      background-color: light-dark(var(--neutral-100), var(--neutral-800-tint));
    }
  }

  @media (prefers-reduced-motion: no-preference) {
    .chevron {
      transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    }
  }
</style>
