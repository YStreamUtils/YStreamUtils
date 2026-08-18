<script lang="ts">
  import { Minus, Square, X } from "@lucide/svelte";
  import { Window } from "@wailsio/runtime";
  let color = $state<string>("#9900ff");
  $effect(() => {
    document.documentElement.style.setProperty("--color-brand", color);
  });
</script>

<div class="modern-titlebar">
  <div class="title">
    <p>YStream<span>Utils</span></p>
  </div>
  <div class="controls">
    <button class="minimize" onclick={() => Window.Minimise()}>
      <Minus size={14} />
    </button>
    <button class="maximize" onclick={() => Window.ToggleMaximise()}>
      <Square size={14} />
    </button>
    <button class="close" onclick={() => Window.Close()}>
      <X size={14} />
    </button>
  </div>
</div>

<main>
  <p>Welcome to YStreamUtils!</p>
  <p>Use the navigation menu to access different features.</p>
  <input type="color" bind:value={color} list="color-options" />
  <datalist id="color-options">
    <option value="#9900ff">Purple</option>
    <option value="#00ff99">Green</option>
    <option value="#ff9900">Orange</option>
  </datalist>
</main>

<style>
  main {
    width: stretch;
    height: stretch;
    padding: 8px;
  }
  .modern-titlebar {
    -webkit-app-region: drag;
    display: flex;
    align-items: center;
    height: 40px;
    background: light-dark(var(--neutral-200-tint), var(--neutral-900-tint));
    border-bottom: 1px solid #1a1a1a;
    padding: 0 16px;
  }

  .title {
    flex: 1;
    text-align: center;
    color: var(--color-text);
    user-select: none;
    margin-left: 140px; /* Adjust for control buttons */
  }

  .title span {
    color: var(--color-brand);
    font-weight: 700;
  }

  .controls {
    display: flex;
    gap: 1px;
  }

  .controls button {
    -webkit-app-region: no-drag;
    width: 46px;
    height: 32px;
    border: none;
    background: transparent;
    color: var(--color-text);
    font-size: 14px;
    cursor: pointer;
    transition: background 0.2s;
  }

  .controls button.minimize {
    --wails-non-client-region: minimize;
  }

  .controls button.maximize {
    --wails-non-client-region: maximize;
  }

  .controls button.close {
    --wails-non-client-region: close;
  }

  .controls button:hover {
    background: rgba(255, 255, 255, 0.1);
  }

  .controls .close:hover {
    background: #e81123;
    color: white;
  }
</style>
