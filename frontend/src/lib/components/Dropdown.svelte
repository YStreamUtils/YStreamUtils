<script lang="ts">
  import type { Snippet } from "svelte";

  interface Props {
    id: string;
    width?: string;
    trigger: Snippet;
    content: Snippet<[() => void]>;
  }

  let { id, width = "10rem", trigger, content }: Props = $props();
  let popoverElement: HTMLDivElement;

  const close = () => popoverElement?.hidePopover();
</script>

<div class="dropdown-anchor-zone" style:--anchor-id="--anchor-{id}">
  {@render trigger()}
</div>

<div
  bind:this={popoverElement}
  {id}
  popover
  class="dropdown-panel"
  style:--drop-width={width}
  style:--anchor-id="--anchor-{id}"
>
  {@render content(close)}
</div>

<style>
  .dropdown-anchor-zone {
    display: contents;
  }
  :global(.dropdown-anchor-zone > button) {
    anchor-name: var(--anchor-id);
  }

  .dropdown-panel {
    background-color: light-dark(var(--neutral-050), var(--neutral-900-tint));
    border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
    border-radius: var(--space-1_5);
    padding: var(--space-1);
    width: var(--drop-width);
    z-index: 60;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    position: absolute;
    margin: 0;
    inset: auto;
    position-anchor: var(--anchor-id);
    top: anchor(bottom);
    right: anchor(right);
    margin-top: var(--space-1_5);
    opacity: 0;
    transform: scale(0.97);
    transition:
      opacity 0.12s ease,
      transform 0.12s cubic-bezier(0.4, 0, 0.2, 1),
      display 0.12s allow-discrete;
  }

  .dropdown-panel:popover-open {
    opacity: 1;
    transform: scale(1);
  }
  @starting-style {
    .dropdown-panel:popover-open {
      opacity: 0;
      transform: scale(0.97);
    }
  }
</style>
