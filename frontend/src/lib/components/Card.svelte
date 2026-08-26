<script lang="ts">
  import type { Snippet } from "svelte";
  import type { HTMLAttributes } from "svelte/elements";

  interface Props extends HTMLAttributes<HTMLDivElement> {
    variant?:
      | "accent"
      | "surface"
      | "success"
      | "warning"
      | "error"
      | "transparent";
    class?: string;
    children?: Snippet;
    props?: any;
  }
  let {
    variant = "surface",
    class: className = "",
    children,
    ...props
  }: Props = $props();
</script>

<div data-variant={variant} class="card {className}" {...props}>
  {@render children?.()}
</div>

<style>
  .card {
    background: var(--_card_bg);
    border: var(--_card_border);
    border-radius: var(--space-2);
    box-shadow: 0 4px 30px rgb(0 0 0 / 3%);
    backdrop-filter: blur(20px);
  }

  .card[data-variant="surface"] {
    --_card_bg: light-dark(
      rgb(245 245 247 / 70%),
      color-mix(in srgb, var(--neutral-950) 65%, transparent)
    );
    --_card_border: 1px solid
      light-dark(
        rgb(0 0 0 / 8%),
        color-mix(in srgb, var(--neutral-800-tint) 40%, transparent)
      );
    --_card_border_hover_color: light-dark(
      rgb(0 0 0 / 15%),
      color-mix(in srgb, var(--color-brand) 40%, transparent)
    );
  }

  .card[data-variant="error"] {
    --_card_bg: color-mix(in srgb, red 8%, transparent);
    --_card_border: 1px solid color-mix(in srgb, red 10%, transparent);
    --_card_border_hover_color: color-mix(in srgb, red 40%, transparent);
  }

  @media (prefers-reduced-motion: no-preference) {
    .card {
      transition:
        border-color 0.2s ease,
        box-shadow 0.2s ease;
    }
  }

  @media (hover: hover) {
    .card:hover {
      border-color: var(--_card_border_hover_color, var(--_card_border));
      box-shadow: 0 4px 30px rgb(0 0 0 / 6%);
    }
  }
</style>
