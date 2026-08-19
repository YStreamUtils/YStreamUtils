import Home from "./pages/Home.svelte";
import Settings from "./pages/Settings.svelte";

export const pages = {
  home: Home,
  settings: Settings
};

export const router = $state({
  current: "home" as keyof typeof pages
});

export const sidebar = $state({
  isOpen: false
});