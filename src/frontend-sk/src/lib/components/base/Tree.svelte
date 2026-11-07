<script lang="ts" module>
  import type { Snippet } from 'svelte';

  export interface TreeNode {
    id: string;
    label: string;
    icon?: Snippet;
    children?: TreeNode[];
  }
</script>

<script lang="ts">
  import { Accordion } from 'bits-ui';
  import Button from './Button.svelte';
  import Tree from './Tree.svelte';

  interface Props {
    items: TreeNode[];
    activeId?: string;
    onSelect?: (node: TreeNode) => void;
  }

  let { items, activeId = '', onSelect }: Props = $props();
</script>

<Accordion.Root type="multiple" class="tree-root-list">
  {#each items as node (node.id)}
    {#if node.children && node.children.length > 0}
      <Accordion.Item value={node.id} class="tree-branch-item">
        <Accordion.Header>
          <Accordion.Trigger>
            {#snippet child({ props })}
              <Button {...props} variant="transparent" align="left" fullWidth class="tree-folder-trigger">
                <div class="tree-row-layout">
                  <span class="tree-chevron" aria-hidden="true">▶</span>
                  {#if node.icon}
                    <span class="tree-icon">{@render node.icon()}</span>
                  {/if}
                  <span class="tree-label folder-label">{node.label}</span>
                  <span class="tree-badge">{node.children?.length}</span>
                </div>
              </Button>
            {/snippet}
          </Accordion.Trigger>
        </Accordion.Header>

        <Accordion.Content class="tree-branch-content">
          <div class="tree-nested-group">
            <Tree items={node.children} {activeId} {onSelect} />
          </div>
        </Accordion.Content>
      </Accordion.Item>
    {:else}
      <div class="tree-leaf-item">
        <Button
          variant="transparent"
          align="left"
          fullWidth
          active={activeId === node.id}
          onclick={() => onSelect?.(node)}>
          <div class="tree-row-layout">
            {#if node.icon}
              <span class="tree-icon">{@render node.icon()}</span>
            {/if}
            <span class="tree-label leaf-label">{node.label}</span>
          </div>
        </Button>
      </div>
    {/if}
  {/each}
</Accordion.Root>

<style>
  .tree-leaf-item {
    display: flex;
    flex-direction: column;
    width: 100%;
  }

  .tree-row-layout {
    display: flex;
    flex-wrap: nowrap;
    gap: var(--space-2, 8px);
    align-items: center;
    width: 100%;
  }

  .tree-chevron {
    display: inline-block;
    font-size: 0.65em;
    color: light-dark(var(--neutral-400), var(--neutral-500));
  }

  :global(.tree-folder-trigger[data-state='open']) .tree-chevron {
    transform: rotate(90deg);
  }

  .tree-icon {
    display: inline-flex;
    flex-shrink: 0;
    flex-wrap: nowrap;
    align-items: center;
    justify-content: center;
  }

  .tree-label {
    flex-grow: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .folder-label {
    font-size: 0.85rem;
    font-weight: 600;

    /* color: light-dark(var(--neutral-600), var(--neutral-400-tint)); */
  }

  .leaf-label {
    font-size: 0.85rem;
  }

  .tree-badge {
    padding: 1px 5px;
    margin-left: auto;
    font-size: 0.7rem;
    font-weight: 500;
    background: light-dark(var(--neutral-200), var(--neutral-800));
    border-radius: var(--space-1, 4px);
  }

  .tree-nested-group {
    display: flex;
    flex-direction: column;
    padding-left: var(--space-1_5, 6px);
    margin-top: var(--space-0_5, 2px);
    margin-bottom: var(--space-0_5, 2px);
    margin-left: var(--space-3, 12px);
    border-left: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
  }

  @media (prefers-reduced-motion: no-preference) {
    .tree-chevron {
      transition: transform 0.12s ease;
    }
  }
</style>
