import Home from "./pages/Home.svelte";
import Settings from "./pages/Settings.svelte";
import Stream from "./pages/Stream.svelte";

export const pages = {
  home: Home,
  settings: Settings,
  stream: Stream
};

export const router = $state({
  current: "home" as keyof typeof pages
});

export const sidebar = $state({
  isOpen: true
});