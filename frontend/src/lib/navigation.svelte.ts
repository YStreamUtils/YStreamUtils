import type { Component } from "svelte";
import Home from "./pages/Home.svelte";
import Settings from "./pages/Settings.svelte";
import Stream from "./pages/Stream.svelte";
import Script from "./pages/Script.svelte";
import Plugins from "./pages/Plugins.svelte";

export const pages: Record<string, Component> = {
  home: Home,
  settings: Settings,
  stream: Stream,
  script: Script,
  plugins: Plugins,
};

export const router = $state<{ current: keyof typeof pages }>({
  current: sessionStorage.getItem("route") || "home",
});

export function navigateTo(route: string) {
  router.current = route;
}

export const sidebar = $state({
  isOpen: JSON.parse(sessionStorage.getItem("sidebarOpen") || "true"),
});
