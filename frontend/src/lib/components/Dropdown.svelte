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

  /* stylelint-disable-next-line selector-pseudo-class-no-unknown */
  :global(.dropdown-anchor-zone > button) {
    anchor-name: var(--anchor-id);
  }

  .dropdown-panel {
    position: absolute;
    inset: auto;
    top: anchor(bottom);
    right: anchor(right);
    z-index: 60;
    width: var(--drop-width);
    padding: var(--space-1);
    margin: 0;
    margin-top: var(--space-1_5);
    position-anchor: var(--anchor-id);
    background-color: light-dark(var(--neutral-050), var(--neutral-900-tint));
    border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
    border-radius: var(--space-1_5);
    box-shadow: 0 4px 12px rgb(0 0 0 / 10%);
    opacity: var(--_panel-opacity, 1);
    transform: scale(var(--_panel-scale, 1));
  }

  @media (prefers-reduced-motion: no-preference) {
    .dropdown-panel {
      --_panel-opacity: 0;
      --_panel-scale: 0.97;

      transition:
        opacity 0.12s ease,
        transform 0.12s cubic-bezier(0.4, 0, 0.2, 1),
        display 0.12s allow-discrete;
    }
  }

  .dropdown-panel:popover-open {
    --_panel-opacity: 1;
    --_panel-scale: 1;
  }

  @starting-style {
    @media (prefers-reduced-motion: no-preference) {
      .dropdown-panel:popover-open {
        --_panel-opacity: 0;
        --_panel-scale: 0.97;
      }
    }
  }
</style>
