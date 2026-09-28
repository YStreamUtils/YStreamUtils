import {getStreams, Platform, type StreamEventEnvelopeOfStreamChatMessageEvent} from '$lib/api';
import { getContext, onMount, setContext } from 'svelte';
import { SvelteMap } from 'svelte/reactivity';

export class StreamState {
	messages = $state<Array<StreamEventEnvelopeOfStreamChatMessageEvent>>([]);
	activeStreamVideoIds = $state<Map<string, Platform>>(new SvelteMap<string, Platform>());
	activeStreamChatIds = $state<Map<string, Platform>>(new SvelteMap<string, Platform>());
	concurrentViewersMap = $state<Map<string, number>>(new SvelteMap<string, number>());
	constructor() {
		onMount(async () => {
			// let events = getEvent
		});
	}

	async initialize() {
		let streams = await getStreams();
		switch (streams.status){
			case 200:
				for (let stream of streams.data.values()) {
					// @ts-ignore
					this.activeStreamVideoIds[stream.videoId] = Platform.YouTube;
					// @ts-ignore
					this.activeStreamChatIds[stream.liveChatId] = Platform.YouTube;
				}
				break;
		}
	}

	async getAllMetrics() {
		
	}
}

const STREAM_KEY = Symbol('STREAM');

export function setStreamState(): StreamState {
	return setContext(STREAM_KEY, new StreamState());
}

export function getStreamState(): StreamState {
	return getContext(STREAM_KEY);
}
