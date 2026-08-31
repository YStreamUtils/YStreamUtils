import { EventKey, type ScriptRecord } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/models';
import { FindAllScripts } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/services/databaseservice';
import { getContext, onMount, setContext } from 'svelte';

export interface Script {
  source: string;
  event: EventKey;
}
export class ScriptState {
  scripts = $state<Record<string, Script>>({
    default_script: this.defaultScriptState()
  });

  static filteredEvents = Object.entries(EventKey).filter(([, userScript]) => userScript !== EventKey.$zero) as [
    keyof typeof EventKey,
    EventKey
  ][];

  constructor() {
    onMount(async () => {
      const loaded = await FindAllScripts();
      console.log(loaded);

      if (loaded == null || Object.keys(loaded).length === 0) {
        return;
      }
      delete this.scripts['test_script'];
      for (const value of Object.values(loaded)) {
        if (value == null) continue;

        this.scripts[value.ID] = {
          source: value.SourceCode,
          event: value.EventKey
        };
      }
    });
  }

  defaultScriptState(): Script {
    return {
      source: '// Try typing "eventData." or "plugins." here!\n\nhost.log("info", "Hello from Goja!");\n',
      event: EventKey.EventKeyManualInvoke
    };
  }
}

const SCRIPT_KEY = Symbol('SCRIPT');

export function setScriptState(): ScriptState {
  return setContext(SCRIPT_KEY, new ScriptState());
}

export function getScriptState(): ReturnType<typeof setScriptState> {
  return getContext(SCRIPT_KEY);
}
