import { Events } from "@wailsio/runtime";
import { EventKey, Platform } from "../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";
import { tick } from "svelte";
import { StartChatStream } from "../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/chatservice";
import { FetchConcurrentViewers, GetActiveBroadcastVideoIDs } from "../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/metricsservice";
import { profiles } from "./auth.svelte";

export const streamState = {
    activeStreamVideoIds: $state<Record<string, Platform>>({}),
    messages: $state<any[]>([]),
    concurrentViewersMap: $state<Record<string, number>>({}),
}

export async function InitStreamState() {
    Events.On(EventKey.EventKeyStreamChatMessage, async (payload) => {
        streamState.messages.push(payload.data.data);
    })

    await initializeDashboard()

    setTimeout(refreshAllMetrics, 5 * 1000 * 60)
}

async function initializeDashboard() {
    try {
        streamState.activeStreamVideoIds = {}
        Object.entries(profiles).forEach(async ([platform, profile]) => {
            if (!profile) {
                return;
            }
            const liveIds = await GetActiveBroadcastVideoIDs(platform, false) ?? []
            liveIds.forEach((id) => {
                StartChatStream(platform, id)
                streamState.activeStreamVideoIds[id] = platform as Platform
            })
        })
    } catch (err) {
        console.error("Failed to complete launch stream scan sequence:", err);
    }
}

async function refreshAllMetrics() {
    Object.entries(streamState.activeStreamVideoIds).forEach(async ([streamId, platform]) => {
      try {
        const count = await FetchConcurrentViewers(platform, streamId);
        streamState.concurrentViewersMap[streamId] = count;
      } catch (err) {
        console.error(
          `Failed to refresh viewer telemetry metrics for ${streamId}:`,
          err,
        );
      }
    })
  }