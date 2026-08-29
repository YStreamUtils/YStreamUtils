<script lang="ts">
  import { EventKey } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/models';
  import { RegisterScriptAndBindToBus } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/services/scriptsservice';
  import Button from '$lib/components/Button.svelte';
  import Card from '$lib/components/Card.svelte';
  import MonacoEditor from '$lib/components/MonacoEditor.svelte';
  import Select from '$lib/components/Select.svelte';
  import { scriptState, defaultScriptState, filteredEvents } from '$lib/state/scriptState.svelte';
  import { Check, Save } from '@lucide/svelte';
  import { Events } from '@wailsio/runtime';

  let currentScript = $state('test_script');
  let currentState = $derived(scriptState[currentScript]);
  function createScript() {
    const name = crypto.randomUUID().substring(0, 12);
    scriptState[name] = structuredClone(defaultScriptState);
    currentScript = name;
  }
</script>

<div class="script-container">
  <Card>
    <div class="script-sidebar">
      {#each Object.entries(scriptState) as [scriptName, state]}
        <Button fullWidth={true} onclick={() => (currentScript = scriptName)} active={currentScript === scriptName}
          >{scriptName}</Button
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
          bind:value={currentState.boundEvent}
          style="padding: 6px; color: #fff; background: #222; border: 1px solid #444; border-radius: 4px;"
        >
          {#each filteredEvents as [eventKey, displayName], index (eventKey)}
            <option value={EventKey[eventKey]}>
              {displayName}
            </option>
          {/each}
        </Select>
        <Button
          color="success"
          icon={Save}
          onclick={async () =>
            await RegisterScriptAndBindToBus(currentState.boundEvent, currentScript, currentState.scriptSource)}
          >Save</Button
        >
        <Button color="warning" icon={Check} onclick={() => Events.Emit(EventKey.EventKeyManualInvoke)}>Test</Button>
      </div>
      <MonacoEditor bind:userScript={currentState.scriptSource} currentKey={currentState.boundEvent} />
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
    flex-flow: column nowrap;
    gap: var(--space-1);
  }
</style>
