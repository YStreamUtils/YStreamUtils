import { getContext, onDestroy, setContext } from 'svelte';
import { EventKey } from '$lib/api';
import { SvelteMap } from 'svelte/reactivity';

export class GlobalEventStreamWrapper {
	#eventSource: EventSource;

	constructor() {
		this.#eventSource = new EventSource('/api/events/listen');

		this.#eventSource.onerror = (err) => {
			console.error('Global EventBus stream connection lost or socket closed.', err);
		};

		try {
			onDestroy(() => {
				console.log('[EventBus Stream] Severing client connection line safely.');
				this.#eventSource.close();
			});
		} catch {}
	}

	on<T>(targetKey: EventKey, callback: (envelope: T) => void): () => void {
		const handler = (msgEvent: Event) => {
			try {
				const envelope: T = JSON.parse((msgEvent as MessageEvent).data);
				callback(envelope);
			} catch (err) {
				console.error(`Failed to parse incoming SSE frame for key [${targetKey}]:`, err);
			}
		};

		this.#eventSource.addEventListener(targetKey, handler);

		const unsubscribe = () => {
			this.#eventSource.removeEventListener(targetKey, handler);
		};

		try {
			onDestroy(unsubscribe);
		} catch {

		}

		return unsubscribe;
	}

	close() {
		console.log('[EventBus Stream] Manually closing the streaming connection.');
		this.#eventSource.close();
	}
}

const EVENT_STREAM_KEY = Symbol('GLOBAL_EVENT_STREAM');

export function setEventStreamState(): GlobalEventStreamWrapper {
	return setContext(EVENT_STREAM_KEY, new GlobalEventStreamWrapper());
}

export function getEventStreamState(): GlobalEventStreamWrapper {
	return getContext(EVENT_STREAM_KEY) as GlobalEventStreamWrapper;
}

export const EventDisplayNames: Record<EventKey, string> = {
	[EventKey["app:manual"]]: 'Manual Invoke',
	[EventKey["stream:chat_message"]]: 'Chat Message (all)',
	[EventKey["stream:youtube:superchat"]]: 'YouTube SuperChat',
	[EventKey["app:log"]]: 'Application Logs',
	[EventKey["stream:discovered"]]: 'Stream Discovered',
	[EventKey["stream:removed"]]: 'Stream Removed',
	[EventKey["stream:metrics"]]: 'Stream Metrics',
	[EventKey["app:all"]]: 'All',
}


