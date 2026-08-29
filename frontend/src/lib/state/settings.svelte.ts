import type { Settings } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services";
import { GetSettings, SaveSettings } from "../../../bindings/github.com/ystreamutils/YStreamUtils/internal/services/settingsservice";

const baseState = $state<{ settings: Settings | null }>({
  settings: null
});

export const appState = {
  get settings() { return baseState.settings; },
  set settings(value: Settings | null) { baseState.settings = value; }
};

export async function initSettings() {
  const loaded = await GetSettings();
  console.log("Loaded Settings:", loaded);
  if (!loaded) return;

  appState.settings = loaded;

  $effect.root(() => {
    $effect(() => {
      if (appState.settings) {
        const rawData = $state.snapshot(appState.settings);
        console.log("Auto-saving raw configuration state:", rawData);
        SaveSettings(rawData);
      }
    });
  });
}
