import { EventKey } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/models';
import type { Script } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/services';
import { GetManifest } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/services/scriptloader';

type ScriptState = {
  scriptSource: string;
  boundEvent: EventKey;
  isEnabled: boolean;
};

export const defaultScriptState: ScriptState = {
  scriptSource: '// Try typing "eventData." or "plugins." here!\n\nhost.log("info", "Hello from Goja!");\n',
  boundEvent: EventKey.EventKeyManualInvoke,
  isEnabled: true
};

export const scriptState = $state<Record<string, ScriptState>>({
  test_script: defaultScriptState
});

export const filteredEvents = Object.entries(EventKey).filter((entry): entry is [keyof typeof EventKey, EventKey] => {
  const [_, userScript] = entry;
  return userScript !== EventKey.$zero;
});

export async function InitScriptState(): Promise<void> {
  const scripts = await GetManifest();
  console.log(scripts);

  if (scripts == null || Object.keys(scripts).length === 0) {
    return;
  }
  delete scriptState['test_script'];
  for (const [name, value] of Object.entries(scripts)) {
    if (value == null) continue;

    console.log(value);
    scriptState[name] = {
      scriptSource: value.source,
      boundEvent: value.eventKey,
      isEnabled: true
    };
  }
}
