<script lang="ts">
  import { Collapsible } from "bits-ui";
  import type { LucideProps } from "@lucide/svelte";
  import { ChevronDown } from "@lucide/svelte";
  import type { Component, Snippet } from "svelte";

  interface Props {
    label?: string;
    icon?: Component<LucideProps, {}, ""> | null;
    children?: Snippet;
    class?: string;
  }

  let {
    label = "Menu",
    icon: Icon,
    children,
    class: className = "",
    ...rest
  }: Props = $props();

  let open = $state(false);
</script>

<Collapsible.Root bind:open class="expander-card {className}" {...rest}>
  <Collapsible.Trigger>
    {#snippet child({ props })}
      <button class="trigger-btn" {...props}>
        {#if Icon}
          <span class="leading-icon"><Icon size={16} /></span>
        {/if}
        <span class="label">{label}</span>
        <span class="chevron" class:rotated={open}>
          <ChevronDown size={16} />
        </span>
      </button>
    {/snippet}
  </Collapsible.Trigger>

  <Collapsible.Content>
    {#snippet child({ props })}
      <div class="collapsible-content" {...props}>
        <div class="content-wrapper" class:visible={open}>
          <div class="content-padding">
            {@render children?.()}
          </div>
        </div>
      </div>
    {/snippet}
  </Collapsible.Content>
</Collapsible.Root>

<style>
  .expander-card {
    display: block;
    width: 100%;
    border: 1px solid var(--neutral-200);
    border-radius: var(--space-1_5);
    overflow: hidden;
  }

  @media (prefers-color-scheme: dark) {
    .expander-card {
      border-color: var(--neutral-800-tint);
    }
  }

  .trigger-btn {
    display: flex;
    align-items: center;
    width: 100%;
    gap: var(--space-3);
    padding: var(--space-4);
    cursor: pointer;
    background: transparent;
    border: none;
    text-align: left;
    font-size: 0.875rem;
    font-family: inherit;
    color: var(--color-text);
  }

  .leading-icon,
  .chevron {
    display: flex;
    align-items: center;
  }

  .chevron {
    margin-left: auto;
    transition: transform 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  }

  .chevron.rotated {
    transform: rotate(180deg);
  }

  .collapsible-content {
    overflow: hidden;
    transition: height 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  }

  :global(.collapsible-content[data-state="open"]) {
    height: var(--bits-collapsible-content-height);
  }

  :global(.collapsible-content[data-state="closed"]) {
    height: 0;
  }

  .content-wrapper {
    opacity: 0;
    transform: scale(0.97);
    transform-origin: top center;
    transition:
      opacity 0.2s cubic-bezier(0.4, 0, 0.2, 1),
      transform 0.2s cubic-bezier(0.34, 1.56, 0.64, 1);
  }

  .content-wrapper.visible {
    opacity: 1;
    transform: scale(1);
  }

  .content-padding {
    padding: var(--space-4);
    border-top: 1px solid var(--neutral-200);
  }

  @media (prefers-color-scheme: dark) {
    .content-padding {
      border-top-color: var(--neutral-800-tint);
    }
  }
</style>
