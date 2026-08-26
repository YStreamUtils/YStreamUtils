<script lang="ts">
  import { Events } from "@wailsio/runtime";
  import { onMount, tick } from "svelte";
  import { profiles } from "../auth.svelte";
  import {
    GetActiveBroadcastVideoIDs,
    FetchConcurrentViewers,
  } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/metricsservice";
  import { StartChatStream } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/chatservice";

  let activeStreamVideoIds = $state<string[]>([]);
  let messages = $state<any[]>([]);
  let concurrentViewersMap = $state<Record<string, number>>({});
  let isAccountLinked = $state(false);
  let isLoadingStreams = $state(true);

  let scrollContainer: HTMLDivElement;

  onMount(() => {
    Events.On("stream:chat_message", async (payload: any) => {
      console.log(payload.data);
      messages = [...messages, payload.data.data];
      await tick();
      if (scrollContainer) {
        scrollContainer.scrollTo({
          top: scrollContainer.scrollHeight,
          behavior: "smooth",
        });
      }
    });

    let interval: any;

    async function initializeDashboard() {
      try {
        console.log(profiles["youtube"]);
        if (profiles["youtube"]) {
          isAccountLinked = true;

          const liveIds = await GetActiveBroadcastVideoIDs("youtube", false);
          console.log(liveIds);
          if (liveIds) {
            activeStreamVideoIds = liveIds;
            console.log(liveIds);
          }

          await refreshAllMetrics();
          liveIds?.forEach((s) => {
            StartChatStream("youtube", s);
          });

          interval = setInterval(refreshAllMetrics, 30000);
        } else {
          isAccountLinked = false;
          isLoadingStreams = false;
        }
      } catch (err) {
        console.error("Failed to complete launch stream scan sequence:", err);
        isLoadingStreams = false;
      }
    }

    initializeDashboard();

    return () => {
      if (interval) clearInterval(interval);
    };
  });

  async function refreshAllMetrics() {
    for (const videoId of activeStreamVideoIds) {
      try {
        const count = await FetchConcurrentViewers("youtube", videoId);
        concurrentViewersMap[videoId] = count;
      } catch (err) {
        console.error(
          `Failed to refresh viewer telemetry metrics for ${videoId}:`,
          err,
        );
      }
    }
    isLoadingStreams = false;
  }
</script>

<div class="stream-grid">
  <!-- Left Column: Unified Custom Live Chat Feed -->
  <div class="grid-left chat-column">
    <div class="chat-header">Chat</div>

    <div class="chat-viewport" bind:this={scrollContainer}>
      {#each messages as msg}
        <div class="chat-row">
          <span class="chat-author">{msg.author}:</span>
          <span class="chat-text">{msg.message}</span>
        </div>
      {/each}
    </div>
  </div>

  <!-- Right Column: Container displaying an embed player for each active stream item -->
  <div class="grid-right player-column">
    {#each activeStreamVideoIds as videoId}
      <div class="player-card">
        <iframe
          title="Youtube Stream - {videoId}"
          src="https://youtube.com/embed/{videoId}?autoplay=1&mute=1&controls=0"
          width="100%"
          height="450px"
          frameborder="0"
          allow="autoplay; encrypted-media; picture-in-picture"
          referrerpolicy="strict-origin-when-cross-origin"
          allowfullscreen
          class="player-frame"
        >
        </iframe>
        <div class="player-footer">
          Live Stream ID: {videoId}
          <span class="footer-viewers">
            👁️ {concurrentViewersMap[videoId] ?? 0} viewers
          </span>
        </div>
      </div>
    {/each}
  </div>
</div>

<style>
  .stream-grid {
    display: grid;
    grid-template-columns: [sidebar] 600px [content] minmax(0, 1fr);
    width: 100%;
    height: 100%;
    border: 1px solid transparent;
    border-radius: var(--space-4);
  }

  .grid-left {
    background: var(--neutral-800-tint);
  }

  .grid-right {
    background: var(--neutral-800);
  }

  .chat-column {
    display: flex;
    flex-direction: column;
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
