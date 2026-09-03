<script lang="ts">
  import { Events } from "@wailsio/runtime";
  import { onMount, tick } from "svelte";
  import { EventKey } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";
  import Card from "../components/base/Card.svelte";
  import Button from "../components/base/Button.svelte";
  import { initializeDashboard, refreshAllMetrics, streamState } from "../state/streamState.svelte";

  let scrollContainer: HTMLDivElement;

  onMount(() => {
    Events.On(EventKey.EventKeyStreamChatMessage, async (payload) => {
      await tick();
      if (scrollContainer) {
        scrollContainer.scrollTo({
          top: scrollContainer.scrollHeight,
          behavior: "smooth",
        });
      }
    });
  });

  $effect(() => {
    console.log($state.snapshot(streamState));
  });
</script>

<div class="stream-grid">
  <div class="chat-column">
    <Card>
      <div class="chat-header">Chat</div>

      <div class="chat-viewport" bind:this={scrollContainer}>
        {#each streamState.messages as msg}
          <div class="chat-row">
            <span class="chat-author" style="color: {msg.authorColor};">{msg.author}:</span>
            <span class="chat-text">{msg.message}</span>
          </div>
        {/each}
      </div>
    </Card>
  </div>

  <div class="grid-right player-column">
    <Button onclick={initializeDashboard}>Refresh Streams</Button>
    <Button onclick={refreshAllMetrics}>Refresh Metrics</Button>
    {#each Object.entries(streamState.activeStreamVideoIds) as [videoId, platform]}
      <div class="player-card">
        <Card>
          {@render youtubeIframe(videoId)}

          <div class="player-footer">
            Live Stream ID: {videoId}
            <span class="footer-viewers">
              👁️ {streamState.concurrentViewersMap[videoId] ?? 0} viewers
            </span>
          </div>
        </Card>
      </div>
    {/each}
  </div>
</div>

{#snippet youtubeIframe(videoId: string)}
  <iframe
    title="Youtube Stream - {videoId}"
    src="https://youtube.com/embed/{videoId}?autoplay=1&mute=1&controls=0&modestbranding=1&rel=0"
    width="100%"
    frameborder="0"
    allow="autoplay; encrypted-media; picture-in-picture"
    referrerpolicy="strict-origin-when-cross-origin"
    allowfullscreen
    class="player-frame"
  ></iframe>
{/snippet}

<style>
  .stream-grid {
    display: grid;
    grid-template-columns: [sidebar] 600px [content] minmax(0, 1fr);
    width: 100%;
    height: 100%;
    border: 1px solid transparent;
    border-radius: var(--space-4);
  }

  .chat-column {
    display: grid;
    height: 100%;
    overflow: hidden;
  }

  .chat-header {
    padding: var(--space-3, 12px);
    font-weight: bold;
    border-bottom: 1px solid var(--neutral-700, #333);
  }

  .chat-viewport {
    display: flex;
    flex: 1;
    flex-direction: column;
    gap: var(--space-2, 8px);
    padding: var(--space-3, 12px);
    overflow-y: auto;
  }

  .chat-row {
    font-size: 13px;
    line-height: 1.4;
  }

  .chat-author {
    margin-right: var(--space-1, 4px);
    font-weight: bold;
    color: var(--accent, #ff4a4a);
  }

  .player-column {
    display: flex;
    flex-direction: column;
    gap: var(--space-4, 16px);
    height: 100%;
    padding: var(--space-4, 16px);
    overflow-y: auto;
  }

  .player-card {
    display: flex;
    flex-direction: column;
    width: 100%;
    overflow: hidden;
    background: var(--neutral-900, #111);
    border-radius: 4px;
  }

  .player-frame {
    aspect-ratio: calc(16 / 9);
    margin-top: var(--space-2);
  }

  .player-footer {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    padding: var(--space-2, 8px) var(--space-3, 12px);
    font-size: 12px;
    color: var(--text-muted, #888);
  }

  .footer-viewers {
    padding: 2px 8px;
    font-weight: bold;
    color: var(--text-normal, #e0e0e0);
    background: var(--neutral-800, #222);
    border-radius: 4px;
  }
</style>
