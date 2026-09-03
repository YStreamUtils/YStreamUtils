<script lang="ts">
  import type { VariantProps } from '$lib/utils/variant';
  import type { Snippet, Component } from 'svelte';
  import type { HTMLButtonAttributes } from 'svelte/elements';

  interface Props extends VariantProps<HTMLButtonAttributes> {
    icon?: Component<any> | null;
    iconSize?: number;
    children?: Snippet;
    active?: boolean;
  }

  let {
    variant = 'surface',
    align = 'center',
    fullWidth = false,
    icon: Icon = null,
    iconSize = 16,
    children,
    class: className = '',
    active = false,
    ...restProps
  }: Props = $props();
</script>

<button
  class="btn {className}"
  data-variant={variant}
  data-align={align}
  data-fullwidth={fullWidth || undefined}
  aria-pressed={active ? 'true' : 'false'}
  {...restProps}
>
  {#if Icon}
    <span class="icon-wrapper">
      <Icon size={iconSize} />
    </span>
  {/if}

  {#if children}
    <div class="content-wrapper">
      {@render children()}
    </div>
  {/if}
</button>

<style>
  .btn {
    display: inline-flex;
    flex-wrap: nowrap;
    align-items: center;
    padding: var(--space-2_5) var(--space-4);
    font-family: inherit;
    font-size: 0.875rem;
    font-weight: 500;
    color: var(--_btn-color, var(--color-text));
    cursor: pointer;
    background: var(--_btn-bg, transparent);
    border: 1px solid var(--_btn-border, transparent);
    border-radius: var(--space-1_5);
  }

  .btn[data-fullwidth] {
    display: flex;
    flex-wrap: nowrap;
    width: 100%;
  }

  .content-wrapper {
    display: flex;
    flex-wrap: wrap;
    overflow: hidden;
  }

  .icon-wrapper {
    display: flex;
    flex-shrink: 0;
    flex-wrap: wrap;
    align-items: center;
    justify-content: center;
    color: var(--_icon-color, inherit);
  }

  .btn[data-align='left'] {
    justify-content: flex-start;
    text-align: left;
  }

  .btn[data-align='center'] {
    justify-content: center;
    text-align: center;
  }

  [data-align='left'] .icon-wrapper + .content-wrapper {
    margin-left: var(--space-2_5);
  }

  [data-align='center'] .icon-wrapper + .content-wrapper {
    margin-left: var(--space-2);
  }

  .btn[data-variant='surface'] {
    --_btn-bg: color-mix(in srgb, light-dark(var(--neutral-100), var(--neutral-800-tint)) 30%, transparent);
    --_btn-border: color-mix(in srgb, light-dark(var(--neutral-200), var(--neutral-700-tint)) 45%, transparent);
    --_icon-color: light-dark(var(--neutral-500), var(--neutral-400-tint));
  }

  .btn[data-variant='transparent'] {
    --_btn-bg: transparent;
    --_btn-border: transparent;
    --_icon-color: light-dark(var(--neutral-500-tint), var(--neutral-400-tint));
  }

  .btn[data-variant='accent'] {
    --_btn-bg: color-mix(in srgb, var(--color-accent) 15%, transparent);
    --_btn-border: color-mix(in srgb, var(--color-accent) 30%, transparent);
    --_btn-color: var(--color-accent);
  }

  .btn[data-variant='success'] {
    --_btn-bg: color-mix(in srgb, var(--color-success) 15%, transparent);
    --_btn-border: color-mix(in srgb, var(--color-success) 30%, transparent);
    --_btn-color: var(--color-success);
  }

  .btn[data-variant='warning'] {
    --_btn-bg: color-mix(in srgb, var(--color-warning) 15%, transparent);
    --_btn-border: color-mix(in srgb, var(--color-warning) 30%, transparent);
    --_btn-color: var(--color-warning);
  }

  .btn[data-variant='error'] {
    --_btn-bg: color-mix(in srgb, var(--color-error) 15%, transparent);
    --_btn-border: color-mix(in srgb, var(--color-error) 30%, transparent);
    --_btn-color: var(--color-error);
  }

  .btn[aria-pressed='true'] {
    --_btn-bg: var(--_active-bg);
    --_btn-border: var(--_active-border, transparent);
    --_btn-color: var(--_active-color, white);
  }

  .btn[data-variant='surface'][aria-pressed='true'] {
    --_active-bg: light-dark(rgb(255 255 255 / 40%), color-mix(in srgb, var(--neutral-950) 40%, transparent));
    --_active-border: var(--color-brand);
    --_active-color: var(--color-text);
    --_icon-color: var(--color-brand);
  }

  .btn[data-variant='transparent'][aria-pressed='true'] {
    --_active-bg: light-dark(var(--neutral-100), var(--neutral-800-tint));
    --_active-border: light-dark(var(--neutral-200-tint), var(--neutral-700-tint));
    --_active-color: var(--color-text);
    --_icon-color: var(--color-brand);
  }

  .btn[data-variant='accent'][aria-pressed='true'] {
    --_active-bg: var(--color-accent);
  }

  .btn[data-variant='success'][aria-pressed='true'] {
    --_active-bg: var(--color-success);
  }

  .btn[data-variant='warning'][aria-pressed='true'] {
    --_active-bg: var(--color-warning);
  }

  .btn[data-variant='error'][aria-pressed='true'] {
    --_active-bg: var(--color-error);
  }

  .btn:active {
    --_btn-bg: var(--_click-bg);
  }

  .btn[data-variant='surface']:active,
  .btn[data-variant='transparent']:active {
    --_click-bg: light-dark(var(--neutral-300-tint), var(--neutral-700-tint));
  }

  @media (hover: hover) {
    .btn:hover {
      --_btn-bg: var(--_hover-bg);
    }

    .btn[data-variant='surface']:hover,
    .btn[data-variant='transparent']:hover {
      --_hover-bg: light-dark(var(--neutral-200-tint), var(--neutral-700-tint));
      --_icon-color: var(--color-brand);
    }

    .btn[data-variant='accent']:hover {
      --_hover-bg: color-mix(in srgb, var(--color-accent) 25%, transparent);
    }

    .btn[data-variant='success']:hover {
      --_hover-bg: color-mix(in srgb, var(--color-success) 25%, transparent);
    }

    .btn[data-variant='warning']:hover {
      --_hover-bg: color-mix(in srgb, var(--color-warning) 25%, transparent);
    }

    .btn[data-variant='error']:hover {
      --_hover-bg: color-mix(in srgb, var(--color-error) 25%, transparent);
    }

    .btn[aria-pressed='true']:hover {
      --_btn-bg: var(--_active-hover-bg);
    }

    .btn:where([data-variant='surface'], [data-variant='transparent']):where([aria-pressed='true']):hover {
      --_active-hover-bg: light-dark(
        color-mix(in srgb, var(--neutral-200-tint) 55%, transparent),
        color-mix(in srgb, var(--neutral-950-tint) 55%, transparent)
      );
    }
  }

  @media (prefers-reduced-motion: no-preference) {
    .btn {
      transition:
        background-color 0.15s ease,
        border-color 0.15s ease,
        color 0.15s ease;
    }

    .btn:active {
      transform: scale(0.98);
    }

    .icon-wrapper {
      transition: color 0.15s ease;
    }
  }
</style>
