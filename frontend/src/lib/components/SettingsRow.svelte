<script lang="ts">
  import type { Snippet } from "svelte";

  interface Props {
    title: string;
    description?: string;
    children: Snippet;
  }

  let { title, description = "", children }: Props = $props();
</script>

<div class="setting-row">
  <div class="text-group">
    <span class="title">{title}</span>
    <span class="description">{description}</span>
  </div>

  {@render children()}
</div>

<style>
  .setting-row {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    font-size: 0.875rem;
    color: var(--color-text);
  }

  .text-group {
    display: flex;
    flex-direction: column;
    gap: 2px;
  }

  .title {
    font-weight: 500;
  }

  .description {
    font-size: 0.75rem;
    color: var(--color-text-muted);
  }
</style>
