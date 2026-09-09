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
      // console.log(loaded);

      if (loaded == null || Object.keys(loaded).length === 0) {
        return;
      }

      delete this.scripts['default_script'];

      const incomingScripts: Record<string, Script> = {};

      for (const value of loaded.data) {
        if (value == null) continue;

        incomingScripts[value.scriptId!] = {
          source: value.rawJsString!,
          event: value.topic!
        };
      }

      this.scripts = incomingScripts;
    });
  }

  defaultScriptState(): Script {
    return {
      source: '// Try typing "eventData." or "plugins." here!\n\nhost.log("info", "Hello from Goja!");\n',
      event: EventKey.ManualInvoke
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
