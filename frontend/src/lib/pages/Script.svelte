<script lang="ts">
  import Button from '$lib/components/base/Button.svelte';
  import Card from '$lib/components/base/Card.svelte';
  import MonacoEditor from '$lib/components/MonacoEditor.svelte';
  import Select from '$lib/components/base/Select.svelte';
  import Tree, { type TreeNode } from '$lib/components/base/Tree.svelte';
  import { getScriptState } from '$lib/state/scriptState.svelte';
  import { Check, Trash } from '@lucide/svelte';
  import { deleteScriptById, EventKey, invokeScriptTest, saveScript } from '$lib/api';

  const scriptState = getScriptState();

  let currentScriptName = $state(Object.keys(scriptState.scripts)[0] || '');
  let currentScript = $derived(scriptState.scripts[currentScriptName]);

  function createScript() {
    const name = crypto.randomUUID().substring(0, 12);
    scriptState.scripts[name] = scriptState.defaultScriptState();
    currentScriptName = name;
  }

  async function saveCurrentScript() {
    saveScript({
      scriptId: currentScriptName,
      topic: currentScript.event,
      rawJsString: currentScript.source,
      isEnabled: true
    });
  }

  let treeItems = $derived.by<TreeNode[]>(() => {
    const eventGroups: Record<string, TreeNode[]> = {};

    Object.entries(scriptState.scripts).forEach(([scriptName, state]) => {
      const eventKey = state.event || 'Unassigned';
      if (!eventGroups[eventKey]) eventGroups[eventKey] = [];

      eventGroups[eventKey].push({
        id: scriptName,
        label: scriptName
      });
    });

    return Object.entries(eventGroups).map(([eventKey, children]) => ({
      id: `group-${eventKey}`,
      label: eventKey,
      children
    }));
  });

  async function handleDelete() {
    if (!currentScriptName) return;

    try {
      await deleteScriptById(currentScriptName);

      delete scriptState.scripts[currentScriptName];
      currentScriptName = Object.keys(scriptState.scripts).at(-1) || '';
    } catch (error) {
      console.error('Failed to delete script:', error);
    }
  }
</script>

<div class="script-container">
  <Card style="height: 100%; overflow: hidden;">
    <div class="script-sidebar">
      <div class="tree-viewport">
        <Tree items={treeItems} activeId={currentScriptName} onSelect={(node) => (currentScriptName = node.id)} />
      </div>

      <div class="sidebar-actions">
        <Button fullWidth={true} variant="success" onclick={createScript}>Add Script</Button>
      </div>
    </div>
  </Card>

  <Card style=" height: 100%;padding: var(--space-2); overflow: hidden;">
    {#if currentScript}
      <div class="editor-layout">
        <div class="editor-toolbar">
          <label for="event-select" class="toolbar-label">Select Event Stream Target:</label>
          <Select
            id="event-select"
            bind:value={currentScript.event}
            entries={scriptState.filteredEvents.map((key) => ({ display: key, value: key }))}
          />

          <Button variant="error" icon={Trash} onclick={handleDelete}>Delete</Button>
          <Button variant="success" icon={Check} onclick={saveCurrentScript}>Save</Button>
          {#if currentScript.isEnabled}
            <Button variant="error" icon={Trash} onclick={() => (currentScript.isEnabled = false)}>Disable</Button>
          {:else}
            <Button variant="success" icon={Check} onclick={() => (currentScript.isEnabled = true)}>Enable</Button>
          {/if}
          {#if currentScript.event == EventKey.ManualInvoke}
            <Button variant="accent" onclick={async () => await invokeScriptTest()}>Test Script</Button>
          {/if}
        </div>

        {#if currentScript.source && currentScript.event}
          <MonacoEditor bind:userScript={currentScript.source} currentKey={currentScript.event} />
        {/if}
      </div>
    {:else}
      <div class="empty-state">
        <p>No active scripts. Create or select a script from the sidebar panel to begin editing.</p>
      </div>
    {/if}
  </Card>
</div>

<style>
  .script-container {
    display: grid;
    grid-template-columns: [sidebar] 240px [content] minmax(0, 1fr);
    gap: var(--space-2);
    height: 100%;
  }

  .script-sidebar {
    display: flex;
    flex-direction: column;
    gap: var(--space-3);
    height: 100%;
    padding: var(--space-2);
  }

  .tree-viewport {
    flex-grow: 1;
    overflow-y: auto;
  }

  .sidebar-actions {
    padding-top: var(--space-2);
    margin-top: auto;
    border-top: 1px solid light-dark(var(--neutral-200), var(--neutral-800));
  }

  /* Extracted Layout Classes */
  .editor-layout {
    display: grid;
    grid-template-rows: [header] auto [content] 1fr;
    height: 100%;
  }

  .editor-toolbar {
    display: flex;
    flex-wrap: wrap;
    gap: var(--space-2);
    align-items: center;
    margin-bottom: var(--space-2);
  }

  .toolbar-label {
    font-weight: bold;
    white-space: nowrap;
  }

  .empty-state {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: center;
    height: 100%;
    font-size: 0.9rem;
    color: light-dark(var(--neutral-500), var(--neutral-400-tint));
  }
</style>
