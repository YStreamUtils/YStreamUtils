<script lang="ts">
  import Dropdown from "./Dropdown.svelte";
  import { Monitor, Moon, Sun, ChevronDown } from "@lucide/svelte";

  interface Props {
    value?: string;
  }

  let { value = $bindable("") }: Props = $props();

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
    <button class="btn btn-between select-trigger" popovertarget="theme-menu">
      <span class="active-group"
        ><ActiveIcon size={14} /> {activeTheme.label}</span
      >
      <ChevronDown size={14} opacity="0.7" />
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
    font-size: 0.75rem;
    gap: var(--space-4);
    min-width: 8.5rem;
    padding: var(--space-1_5) var(--space-3);
    border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
    border-radius: var(--space-1);
  }
  .active-group {
    display: flex;
    align-items: center;
    gap: var(--space-2);
  }
  .menu-btn {
    font-size: 0.75rem;
    gap: var(--space-2);
    width: 100%;
    padding: var(--space-2) var(--space-3);
    border-radius: var(--space-1);
  }
  .menu-btn:hover {
    background-color: light-dark(var(--neutral-100), var(--neutral-800-tint));
  }
  .menu-btn.selected {
    background-color: var(--color-brand);
    color: #fff;
  }
</style>
