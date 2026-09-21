import { Platform, type StreamChatMessageEvent } from '$lib/api';
import { getContext, onMount, setContext } from 'svelte';
import { SvelteMap } from 'svelte/reactivity';

export class StreamState {
	messages = $state<Array<StreamChatMessageEvent>>([]);
	activeStreamVideoIds = $state<Map<string, Platform>>(new SvelteMap<string, Platform>());
	concurrentViewersMap = $state<Map<string, number>>(new SvelteMap<string, number>());
	constructor() {
		onMount(async () => {});
	}

	async initialize() {
		let streams = await get 
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
