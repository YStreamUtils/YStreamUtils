import Plugins from "../pages/Plugins.svelte";

export const sidebar = $state({
  isOpen: JSON.parse(sessionStorage.getItem("sidebarOpen") || "true"),
});
