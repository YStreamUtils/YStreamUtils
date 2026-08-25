<script lang="ts">
  import Button from "./Button.svelte";
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
    <Button 
      class="select-trigger" 
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
    </Button>
  {/snippet}

  {#snippet content(close)}
    {#each themes as theme}
      <Button
        type="button"
        fullWidth={true}
        variant="transparent"
        icon={theme.icon}
        iconSize={14}
        class="menu-btn {value === theme.value? "selected" : ""}"
        onclick={() => {
          value = theme.value;
          isOpen = false;
          close();
        }}
      >
        {theme.label}
      </Button>
    {/each}
  {/snippet}
</Dropdown>

<style>
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

  @media (prefers-reduced-motion: no-preference) {
    .chevron {
      transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    }
  }
</style>
