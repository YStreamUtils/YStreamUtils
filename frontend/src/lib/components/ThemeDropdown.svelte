<script lang="ts">
  import Button from "./base/Button.svelte";
  import Dropdown from "./base/Dropdown.svelte";
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
      color="surface"
      variant="surface"
      align="left"
      popovertarget="theme-menu"
      onclick={() => (isOpen = !isOpen)}
    >
      <span class="trigger-layout">
        <span class="active-group">
          <ActiveIcon size={14} />
          {activeTheme.label}
        </span>
        <span class="chevron" class:rotated={isOpen}>
          <ChevronDown size={14} opacity="0.7" />
        </span>
      </span>
    </Button>
  {/snippet}

  {#snippet content(close)}
    {#each themes as theme}
      <Button
        type="button"
        variant="transparent"
        fullWidth={true}
        icon={theme.icon}
        iconSize={14}
        align="left"
        active={value === theme.value}
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
  .trigger-layout {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    width: 100%;
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
    margin-left: var(--space-2);
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
