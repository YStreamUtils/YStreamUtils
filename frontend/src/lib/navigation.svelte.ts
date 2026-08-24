import type { Component } from "svelte";
import Home from "./pages/Home.svelte";
import Settings from "./pages/Settings.svelte";
import Stream from "./pages/Stream.svelte";

export const pages: Record<string, Component> = {
  home: Home,
  settings: Settings,
  stream: Stream
};

export const router = $state({
  current: sessionStorage.getItem("route") || "home"
});

export const sidebar = $state({
  isOpen: JSON.parse(sessionStorage.getItem("sidebarOpen") || "true")
});