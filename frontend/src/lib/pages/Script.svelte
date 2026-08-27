<script lang="ts">
  import { Check, Save } from "@lucide/svelte";
  import { EventKey } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";
  import Button from "../components/Button.svelte";
  import Card from "../components/Card.svelte";
  import MonacoEditor from "../components/MonacoEditor.svelte";
  import { RegisterScriptAndBindToBus } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/scriptsservice";
  import { Events } from "@wailsio/runtime";

  let currentKey = $state<EventKey>(EventKey.EventKeyStreamChatMessage);
  let currentTheme = $state("goja-dark");
  let userScript = $state(
    '// Try typing "payload.data." or "plugins." here!\n\nhost.log("info", "Hello from Goja!");\n',
  );

  const filteredEvents = Object.entries(EventKey).filter(
    (entry): entry is [keyof typeof EventKey, EventKey] => {
      const [_, value] = entry;
      return value !== EventKey.$zero;
    },
  );

  async function handleSave() {
    try {
      RegisterScriptAndBindToBus(currentKey, "script_name", userScript);
    } catch (error) {
      console.error(error);
    } finally {
      alert(`Script ${"script_name"} saved and bound to ${currentKey}`);
    }
  }

  async function testButton() {
    Events.Emit(EventKey.EventKeyManualInvoke);
  }
</script>

<Card style="width: 100%; height: 100%;padding: var(--space-4);">
  <div
    style="display: grid; grid-template-rows: [header] auto [content] 1fr; height: 100%"
  >
    <div>
      <label
        for="event-select"
        style=" ;margin-right: 10px; font-weight: bold;"
      >
        Select Event Stream Target:
      </label>
      <select
        id="event-select"
        bind:value={currentKey}
        style="padding: 6px; color: #fff; background: #222; border: 1px solid #444; border-radius: 4px;"
      >
        {#each filteredEvents as [eventKey, displayName], index (eventKey)}
          <option value={EventKey[eventKey]}>
            {displayName}
          </option>
        {/each}
      </select>
      <Button color="success" icon={Save} onclick={handleSave}>Save</Button>
      <Button color="warning" icon={Check} onclick={testButton}>Test</Button>
    </div>

    <MonacoEditor bind:value={userScript} eventKey={currentKey} />
  </div>
</Card>
