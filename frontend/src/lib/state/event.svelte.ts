import { getContext, onMount, setContext } from 'svelte';
import { EventKey, type Platform } from '$lib/api';

export interface StreamEventEnvelope<T> {
  event: EventKey;
  platform: Platform;
  timestamp?: string;
  data: T;
}

export class GlobalEventStreamWrapper {
  #rawEvent = $state<StreamEventEnvelope<unknown> | null>(null);

  constructor() {
    onMount(() => {
      const eventSource = new EventSource('/api/events/listen');

      eventSource.onmessage = (msg) => {
        try {
          const envelope: StreamEventEnvelope<unknown> = JSON.parse(msg.data);
          this.#rawEvent = envelope;
        } catch (err) {
          console.error('Failed to parse incoming EventBus SSE frame payload:', err);
        }
      };

      eventSource.onerror = (err) => {
        console.error('Global EventBus stream connection lost or socket closed.', err);
      };

      return () => {
        console.log('[EventBus Stream] Severing client listener connection line safely.');
        eventSource.close();
      };
    });
  }

  on(targetKey: EventKey, callback: (envelope: StreamEventEnvelope<any>) => void) {
    $effect(() => {
      const current = this.#rawEvent;

      if (!current) return;

      if (current.event === targetKey) {
        callback(current);
      }
    });
  }
}

const EVENT_STREAM_KEY = Symbol('GLOBAL_EVENT_STREAM');

export function setEventStreamState(): GlobalEventStreamWrapper {
  return setContext(EVENT_STREAM_KEY, new GlobalEventStreamWrapper());
}

export function getEventStreamState(): GlobalEventStreamWrapper {
  return getContext(EVENT_STREAM_KEY) as GlobalEventStreamWrapper;
}

export const EventDisplayNames = new Map<EventKey, string>([
  [EventKey.ManualInvoke, 'Manual Invoke'],
  [EventKey.StreamChatMessage, 'Stream Chat Message (all)'],
  [EventKey.YoutubeSuperChat, 'YouTube SuperChat'],
  [EventKey.ApplicationLog, 'Application Logs']
]);
