import {
    EventKey,
    Platform,
    type StreamChatMessageEvent,
    type StreamDiscoveredEvent,
    type StreamMetricsEvent,
    type StreamRemovedEvent
} from '$lib/api';
import {getContext, setContext} from 'svelte';
import {SvelteMap} from 'svelte/reactivity';
import {getEventStreamState} from "$lib/state/event.svelte";

export class StreamState {
    messages = $state<Array<StreamChatMessageEvent>>([]);
    activeStreamVideoIds = $state<Map<string, Platform>>(new SvelteMap<string, Platform>());
    concurrentViewersMap = $state<Map<string, string>>(new SvelteMap<string, string>());

    constructor() {
        try {
            let events = getEventStreamState();

            events.on<StreamChatMessageEvent>(EventKey["stream:chat_message"], event => {
                if (this.messages.length >= 1000) this.messages.shift();
                if (event.data == null) return;
                this.messages.push(event);
            })

            events.on<StreamMetricsEvent>(EventKey["stream:metrics"], event => {
                let data = event.data;
                if (data.videoId != undefined && data.viewers != undefined) {
                    this.concurrentViewersMap.set(data.videoId, data.viewers);
                }
            })

            events.on<StreamDiscoveredEvent>(EventKey["stream:discovered"], event => {
                let streamInfo = event.data.streamInfo
                if (streamInfo != undefined) {
                    if (streamInfo.streamId != undefined && streamInfo.platform != undefined) {
                        this.activeStreamVideoIds.set(streamInfo.streamId, <Platform>streamInfo.platform);
                    }
                }
            })

            events.on<StreamRemovedEvent>(EventKey["stream:removed"], event => {
                let streamInfo = event.data.streamInfo
                if (streamInfo != undefined) {
                    if (streamInfo.streamId != undefined) {
                        this.activeStreamVideoIds.delete(streamInfo.streamId);
                    }
                }
            })
        } catch (e){
            console.error(e);
        }
    }

    async initialize() {
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
