<script lang="ts">
  let color = $state<string>("#9900ff");
  $effect(() => {
    document.documentElement.style.setProperty("--color-brand", color);
  });
  let theme = $state<"light" | "dark" | "system">("light");
  $effect(() => {
    let value = theme === "system" ? "light dark" : theme;
    document.documentElement.style.setProperty("color-scheme", value);
  });
</script>

<p>Welcome to YStreamUtils!</p>
<p>Use the navigation menu to access different features.</p>
<input type="color" bind:value={color} list="color-options" />
<datalist id="color-options">
  <option value="#9900ff">Purple</option>
  <option value="#00ff99">Green</option>
  <option value="#ff9900">Orange</option>
  <option value="#ff0000">Red</option>
  <option value="#0000ff">Blue</option>
  <option value="#00ffff">Cyan</option>
  <option value="#ff00ff">Magenta</option>
</datalist>

<select bind:value={theme}>
  <option value="light">Light</option>
  <option value="dark">Dark</option>
  <option value="system">System</option>
</select>
