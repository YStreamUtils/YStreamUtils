<script lang="ts">
  import type { Snippet } from "svelte";
  import type { HTMLAttributes } from "svelte/elements";

  interface Props extends HTMLAttributes<HTMLDivElement> {
    class?: string;
    children?: Snippet;
    props?: any;
  }
  let { class: className = "", children, ...props }: Props = $props();
</script>

<div class="card {className}" {...props}>
  {@render children?.()}
</div>

<style>
  .card {
    background: light-dark(
      rgb(245 245 247 / 70%),
      color-mix(in srgb, var(--neutral-950) 65%, transparent)
    );
    border: 1px solid
      light-dark(
        rgb(0 0 0 / 8%),
        color-mix(in srgb, var(--neutral-800-tint) 40%, transparent)
      );
    border-radius: var(--space-2);
    box-shadow: 0 4px 30px rgb(0 0 0 / 3%);
    backdrop-filter: blur(20px);
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
      border-color: light-dark(
        rgb(0 0 0 / 15%),
        color-mix(in srgb, var(--color-brand) 40%, transparent)
      );
      box-shadow: 0 4px 30px rgb(0 0 0 / 6%);
    }
  }
</style>
