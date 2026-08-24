<script lang="ts">
  import { Menu, Minus, Square, X } from "@lucide/svelte";
  import { Window } from "@wailsio/runtime";
  import { sidebar } from "../navigation.svelte";

  const { onMenuClicked } = $props<{ onMenuClicked: () => void }>();
</script>

<div class="modern-titlebar">
  <button
    class="btn btn-center sidebar-button"
    onclick={() => {
      onMenuClicked();
    }}
  >
    <Menu size={24} />
  </button>
  <div class="title">
    <p>YStream<span>Utils</span></p>
  </div>
  <div class="controls">
    <button
      class="btn btn-center btn-system btn-minimize"
      onclick={() => Window.Minimise()}
    >
      <Minus size={16} />
    </button>
    <button
      class="btn btn-center btn-system btn-maximize"
      onclick={() => Window.ToggleMaximise()}
    >
      <Square size={16} />
    </button>
    <button
      class="btn btn-center btn-system btn-close"
      onclick={() => Window.Close()}
    >
      <X size={16} />
    </button>
  </div>
</div>

<style>
  .modern-titlebar {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    height: var(--space-10);
    padding: 0 var(--space-2);
    padding-left: var(--space-4);
    
    /* stylelint-disable-next-line defensive-css/no-user-select-none */
    user-select: none;
    background: light-dark(var(--neutral-200-tint), var(--neutral-900-tint));
    border-bottom: 1px solid #1a1a1a;
    -webkit-app-region: drag;
  }

  .title {
    flex: 1;
    margin-left: var(--space-4);
    color: var(--color-text);
  }

  .title span {
    font-weight: 700;
    color: var(--color-brand);
  }

  .controls {
    display: flex;
    flex-wrap: wrap;
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

  /* Scoped hover interactions to require standard user motion parameters */
  @media (hover: hover) and (prefers-reduced-motion: no-preference) {
    .controls button:hover,
    .sidebar-button:hover {
      background: rgb(255 255 255 / 10%);
      transition: background 0.15s ease;
    }

    .controls .btn-close:hover {
      color: white;
      background: #e81123;
      transition: background 0.15s ease, color 0.15s ease;
    }
  }

  /* Fallback static styles for users with prefers-reduced-motion enabled */
  @media (hover: hover) and (prefers-reduced-motion: reduce) {
    .controls button:hover,
    .sidebar-button:hover {
      background: rgb(255 255 255 / 10%);
    }

    .controls .btn-close:hover {
      color: white;
      background: #e81123;
    }
  }
</style>
