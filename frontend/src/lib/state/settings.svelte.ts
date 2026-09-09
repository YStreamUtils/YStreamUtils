import { getContext, setContext } from 'svelte';
import { getSettings, saveSettings, type Settings } from '$lib/api';

export class SettingsState {
  settings = $state<Settings>({
    ui: { color: '', theme: 'dark', fullyCloseSidebar: false }
  } as unknown as Settings);

  #isInitialLoad = true;

  constructor() {
    this.loadSettings();

    $effect(() => {
      const currentSettings = this.settings;
      if (!currentSettings || !currentSettings.ui?.theme) return;

      JSON.stringify(currentSettings);

      if (this.#isInitialLoad) {
        this.#isInitialLoad = false;
        return;
      }

      const timer = setTimeout(() => {
        const rawData = $state.snapshot(currentSettings);
        // console.log('Auto-saving configuration state:', rawData);
        saveSettings(rawData);
      }, 1000);

      return () => {
        clearTimeout(timer);
      };
    });
  }

  private async loadSettings() {
    try {
      const loaded = await getSettings();
      if (loaded?.data) {
        this.settings = loaded.data;
      }
    } catch (err) {
      console.error('Failed to load settings:', err);
    }
  }
}

const SETTINGS_KEY = Symbol('SETTINGS');

export function setSettingsState(): SettingsState {
  return setContext(SETTINGS_KEY, new SettingsState());
}

export function getSettingsState(): SettingsState {
  const context = getContext<SettingsState>(SETTINGS_KEY);
  if (!context) {
    throw new Error('getSettingsState must be consumed within a child component provider.');
  }
  return context;
}
