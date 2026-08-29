import { EventKey } from '$bindings/github.com/ystreamutils/YStreamUtils/internal/models';

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
