import { Events } from "@wailsio/runtime";
import { profiles } from "$lib/state/auth.svelte";
import { EventKey, type Platform } from "$bindings/github.com/ystreamutils/YStreamUtils/internal/models";
import { StartChatStream } from "$bindings/github.com/ystreamutils/YStreamUtils/internal/services/chatservice";
import { GetActiveBroadcastVideoIDs, FetchConcurrentViewers } from "$bindings/github.com/ystreamutils/YStreamUtils/internal/services/metricsservice";

type StreamState = {
  activeStreamVideoIds: Record<string, Platform>;
  messages: any[];
  concurrentViewersMap: Record<string, number>;
};

const defaultStreamState: StreamState = {
  activeStreamVideoIds: {},
  messages: [],
  concurrentViewersMap: {},
};

export const streamState = $state<StreamState>(defaultStreamState);

export async function InitStreamState() {
  Events.On(EventKey.EventKeyStreamChatMessage, async (payload) => {
    streamState.messages.push(payload.data.data);
  });

  await initializeDashboard();
  await refreshAllMetrics();
  setInterval(refreshAllMetrics, 5 * 60 * 1000);
}

export async function initializeDashboard() {
  try {
    streamState.activeStreamVideoIds = {};
    
    const scanPromises = Object.entries(profiles).map(async ([platform, profile]) => {
      if (!profile) return;
      const liveIds = (await GetActiveBroadcastVideoIDs(platform, false)) ?? [];
      
      liveIds.forEach((id) => {
        StartChatStream(platform, id);
        streamState.activeStreamVideoIds[id] = platform as Platform;
      });
    });

    await Promise.all(scanPromises);
    
  } catch (err) {
    console.error("Failed to complete launch stream scan sequence:", err);
  }
}

export async function refreshAllMetrics() {
  const promises = Object.entries(streamState.activeStreamVideoIds).map(
    async ([streamId, platform]) => {
      try {
        const count = await FetchConcurrentViewers(platform, streamId);
        streamState.concurrentViewersMap[streamId] = count;
      } catch (err) {
        console.error(
          `Failed to refresh viewer telemetry metrics for ${streamId}:`,
          err,
        );
      }
    }
  );

  await Promise.all(promises);
}
