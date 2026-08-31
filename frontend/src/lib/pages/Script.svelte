<script lang="ts">
  import { EventKey } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/models';
  import {
    DeleteScript,
    SaveScript
  } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/services/databaseservice';
  import { RegisterScriptAndBindToBus } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/services/scriptsservice';
  import Button from '$lib/components/Button.svelte';
  import Card from '$lib/components/Card.svelte';
  import MonacoEditor from '$lib/components/MonacoEditor.svelte';
  import Select from '$lib/components/Select.svelte';
  import { getScriptState, ScriptState } from '$lib/state/scriptState.svelte';
  import { Check, Save, Trash } from '@lucide/svelte';
  import { Events } from '@wailsio/runtime';

  const scriptState = getScriptState();

  let currentScriptName = $state(Object.keys(scriptState.scripts)[0]);
  let currentScript = $derived(scriptState.scripts[currentScriptName]);
  function createScript() {
    const name = crypto.randomUUID().substring(0, 12);
    scriptState.scripts[name] = scriptState.defaultScriptState();
    console.log(scriptState.scripts);
    currentScriptName = name;
  }

  $effect(() => {
    SaveScript(currentScriptName, currentScript.source, currentScript.event);
  });

  async function handleSave() {
    await RegisterScriptAndBindToBus(currentScript.event, currentScriptName, currentScript.source);
  }

  async function handleDelete(event: MouseEvent & { currentTarget: EventTarget & HTMLButtonElement }) {
    await DeleteScript(currentScriptName);
    delete scriptState.scripts[currentScriptName];
    currentScript.source = '';
    currentScriptName = Object.keys(scriptState.scripts).at(-1) || '';
  }
</script>

<div class="script-container">
  <Card style="height: 100%; overflow: hidden;">
    <div class="script-sidebar">
      {#each Object.entries(scriptState.scripts) as [scriptName, state]}
        <Button
          fullWidth={true}
          onclick={() => (currentScriptName = scriptName)}
          active={currentScriptName === scriptName}>{scriptName}</Button
        >
      {/each}
      <Button fullWidth={true} variant="success" onclick={createScript}>Add Script</Button>
    </div>
  </Card>
  <Card style="padding: var(--space-2);">
    <div style="display: grid; grid-template-rows: [header] auto [content] 1fr; height: 100%">
      <div>
        <label for="event-select" style=" ;margin-right: 10px; font-weight: bold;"> Select Event Stream Target: </label>
        <Select
          id="event-select"
          bind:value={currentScript.event}
          style="padding: 6px; color: #fff; background: #222; border: 1px solid #444; border-radius: 4px;"
        >
          {#each ScriptState.filteredEvents as [eventKey, displayName], index (eventKey)}
            <option value={EventKey[eventKey]}>
              {displayName}
            </option>
          {/each}
        </Select>
        <Button variant="success" icon={Save} onclick={handleSave}>Save</Button>
        <Button variant="error" icon={Trash} onclick={handleDelete}>Delete</Button>
        <Button variant="warning" icon={Check} onclick={() => Events.Emit(EventKey.EventKeyManualInvoke)}>Test</Button>
      </div>
      {#if currentScript.source && currentScript.event}
        <MonacoEditor bind:userScript={currentScript.source} currentKey={currentScript.event} />
      {/if}
    </div>
  </Card>
</div>

<style>
  .script-container {
    display: grid;
    grid-template-columns: [sidebar] 200px [content] minmax(0, 1fr);
    gap: var(--space-2);
    height: 100%;
  }

  .script-sidebar {
    display: flex;
    flex-grow: 0;
    flex-flow: column nowrap;
    gap: var(--space-1);
    height: 100%;
    overflow-y: auto;
  }
</style>
