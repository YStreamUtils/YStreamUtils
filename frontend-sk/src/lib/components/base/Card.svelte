<script lang="ts">
  import type { Snippet } from 'svelte';
  import type { HTMLAttributes } from 'svelte/elements';
  import type { VariantProps } from '$lib/utils/variant';

  interface Props extends VariantProps<HTMLAttributes<HTMLDivElement>> {
    children?: Snippet;
    props?: any;
  }
  let { variant = 'surface', customColor = '', class: className = '', children, ...props }: Props = $props();
</script>

<div
  data-variant={variant}
  class="card {className}"
  {...props}
  style:--color-custom={variant === 'custom' ? customColor : undefined}>
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

  .card[data-variant='surface'] {
    --_card_bg: light-dark(rgb(245 245 247 / 70%), color-mix(in srgb, var(--neutral-950) 65%, transparent));
    --_card_border: 1px solid light-dark(rgb(0 0 0 / 8%), color-mix(in srgb, var(--neutral-800-tint) 40%, transparent));
    --_card_border_hover_color: light-dark(rgb(0 0 0 / 15%), color-mix(in srgb, var(--color-brand) 40%, transparent));
  }

  .card[data-variant='error'] {
    --_card_bg: color-mix(in srgb, var(--color-error) 15%, transparent);
    --_card_border: 1px solid color-mix(in srgb, var(--color-error) 30%, transparent);
    --_card_border_hover_color: var(--color-error);
  }

  .card[data-variant='transparent'] {
    --_card_bg: transparent;
    --_card_border: transparent;
    --_card_border_hover_color: light-dark(var(--neutral-500-tint), var(--neutral-400-tint));
  }

  .card[data-variant='accent'] {
    --_card_bg: color-mix(in srgb, var(--color-accent) 15%, transparent);
    --_card_border: color-mix(in srgb, var(--color-accent) 30%, transparent);
    --_card_border_hover_color: var(--color-accent);
  }

  .card[data-variant='success'] {
    --_card_bg: color-mix(in srgb, var(--color-success) 15%, transparent);
    --_card_border: color-mix(in srgb, var(--color-success) 30%, transparent);
    --_card_border_hover_color: var(--color-success);
  }

  .card[data-variant='warning'] {
    --_card_bg: color-mix(in srgb, var(--color-warning) 15%, transparent);
    --_card_border: color-mix(in srgb, var(--color-warning) 30%, transparent);
    --_card_border_hover_color: var(--color-warning);
  }

  .card[data-variant='custom'] {
    --_card_bg: color-mix(in srgb, var(--color-custom) 15%, transparent);
    --_card_border: color-mix(in srgb, var(--color-custom) 30%, transparent);
    --_card_border_hover_color: var(--color-custom);
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
