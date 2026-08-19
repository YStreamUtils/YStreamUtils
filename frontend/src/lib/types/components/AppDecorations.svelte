<script lang="ts">
  import { Menu, Minus, Square, X } from "@lucide/svelte";
  import { Window } from "@wailsio/runtime";
</script>

<div class="modern-titlebar">
  <button
    class="btn sidebar-button"
    popovertarget="sidebar"
    popovertargetaction="toggle"
  >
    <Menu size={24} />
  </button>
  <div class="title">
    <p>YStream<span>Utils</span></p>
  </div>
  <div class="controls">
    <button
      class="btn btn-system btn-minimize"
      onclick={() => Window.Minimise()}
    >
      <Minus size={16} />
    </button>
    <button
      class="btn btn-system btn-maximize"
      onclick={() => Window.ToggleMaximise()}
    >
      <Square size={16} />
    </button>
    <button class="btn btn-system btn-close" onclick={() => Window.Close()}>
      <X size={16} />
    </button>
  </div>
</div>

<style>
  .modern-titlebar {
    -webkit-app-region: drag;
    display: flex;
    align-items: center;
    height: var(--space-10);
    background: light-dark(var(--neutral-200-tint), var(--neutral-900-tint));
    border-bottom: 1px solid #1a1a1a;
    padding: 0 var(--space-4);
  }

  .title {
    flex: 1;
    /*text-align: center;*/
    color: var(--color-text);
    user-select: none;
    margin-left: var(--space-4);
  }

  .title span {
    color: var(--color-brand);
    font-weight: 700;
  }

  .controls {
    display: flex;
    gap: var(--space-0_5);
  }

  .btn-system {
    -webkit-app-region: no-drag;
    width: var(--space-12);
    height: var(--space-8);
    background: transparent;
    border: none;
  }
  .sidebar-button {
    -webkit-app-region: no-drag;
    width: var(--space-8);
    height: var(--space-8);
    background: transparent;
    border: none;
  }

  .controls button:hover,
  .sidebar-button:hover {
    background: rgba(255, 255, 255, 0.1);
  }

  .controls .btn-close:hover {
    background: #e81123;
    color: white;
  }
</style>
