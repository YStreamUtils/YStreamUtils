import { EventKey, getAllScripts, type EventKey as EventKeyType } from '$lib/api';
import { getContext, onMount, setContext } from 'svelte';

export interface Script {
	source: string;
	event: EventKeyType;
	isEnabled?: boolean;
}

export class ScriptState {
	scripts = $state<Record<string, Script>>({
		default_script: this.defaultScriptState()
	});

	filteredEvents: EventKey[] = Object.values(EventKey);

	constructor() {
		onMount(async () => {
			const loaded = await getAllScripts();

			if (loaded == null || Object.keys(loaded.data.values()).length === 0) {
				return;
			}

			delete this.scripts['default_script'];

			const incomingScripts: Record<string, Script> = {};

			for (const value of loaded.data.values()) {
				if (value == null) continue;

				incomingScripts[value.scriptId!] = {
					source: value.source!,
					event: value.topic!
				};
			}

			this.scripts = incomingScripts;
		});
	}

	defaultScriptState(): Script {
		return {
			source:
				'// Try typing "eventData." or "plugins." here!\n\nhost.log("info", "Hello from Goja!");\n',
			event: EventKey["app:manual"]
		};
	}
}

const SCRIPT_KEY = Symbol('SCRIPT');

export function setScriptState(): ScriptState {
	return setContext(SCRIPT_KEY, new ScriptState());
}

export function getScriptState(): ScriptState {
	return getContext(SCRIPT_KEY);
}
