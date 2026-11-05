export const sidebar = $state({
  isOpen: JSON.parse(sessionStorage.getItem("sidebarOpen") || "true"),
});
