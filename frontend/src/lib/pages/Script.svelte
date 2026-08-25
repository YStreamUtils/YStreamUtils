<script lang="ts">
  import { EventKey } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";
  import Card from "../components/Card.svelte";
  import MonacoEditor from "../components/MonacoEditor.svelte";

  let currentKey = $state<EventKey>(EventKey.EventKeyStreamChatMessage);
  let currentTheme = $state("goja-dark");
  let userScript = $state(
    '// Try typing "payload.data." or "plugins." here!\n\nhost.log("info", "Hello from Goja!");\n',
  );
</script>

<Card style=" height: 100%;padding: var(--space-4);">
  <div style=" display: flex; gap: 20px;margin-bottom: 15px;">
    <div>
      <label for="event-select" style="margin-right: 10px; font-weight: bold;"
        >Select Event Stream Target:</label
      >
      <select
        id="event-select"
        bind:value={currentKey}
        style="padding: 6px; color: #fff; background: #222; border: 1px solid #444; border-radius: 4px;"
      >
        <option value={EventKey.EventKeyStreamChatMessage}
          >Stream Chat Message (all)</option
        >
        <option value={EventKey.EventKeyYoutubeSuperchat}
          >YouTube SuperChat</option
        >
      </select>
      <MonacoEditor bind:value={userScript} eventKey={currentKey} />

      <div style="margin-top: 20px;">
        <h4>Preview:</h4>
        <pre
          style=" padding: 10px; overflow-x: auto; color: #0ad63d;background: #1e1e1e; border: 1px solid #333;">{userScript}</pre>
      </div>
    </div>
  </div></Card
>
