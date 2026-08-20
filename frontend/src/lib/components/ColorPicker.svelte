<script lang="ts">
  import { Paintbrush } from "@lucide/svelte";

  interface Props {
    value?: string;
  }

  let { value = $bindable("#9900ff") }: Props = $props();

  const presets = [
    { value: "#9900ff", label: "Purple" },
    { value: "#00ff99", label: "Green" },
    { value: "#ff9900", label: "Orange" },
    { value: "#ff0000", label: "Red" },
    { value: "#0000ff", label: "Blue" },
    { value: "#00ffff", label: "Cyan" },
    { value: "#ff00ff", label: "Magenta" },
  ];

  let nativePickerInput: HTMLInputElement;
  let popoverElement: HTMLDivElement;

  function closePopover() {
    popoverElement?.hidePopover();
  }
</script>

<div class="setting-row">
  <span>Theme Accent</span>

  <button
    class="btn btn-center picker-trigger"
    title="Open color picker"
    popovertarget="color-picker-popover"
  >
    <div class="swatch preview" style:background-color={value}></div>
    <span class="hex">{value}</span>
  </button>

  <div
    bind:this={popoverElement}
    id="color-picker-popover"
    popover="auto"
    class="panel"
  >
    <div class="grid">
      {#each presets as preset}
        <button
          type="button"
          class="swatch"
          class:active={value === preset.value}
          style:background-color={preset.value}
          title="Select {preset.label}"
          aria-label="Select {preset.label}"
          onclick={() => {
            value = preset.value;
            closePopover();
          }}
        ></button>
      {/each}
    </div>

    <button
      type="button"
      class="btn btn-center custom-btn"
      onclick={() => nativePickerInput.click()}
    >
      <Paintbrush size={14} /> Custom color
    </button>

    <input
      bind:this={nativePickerInput}
      type="color"
      class="sr-only"
      bind:value
      aria-label="Custom color picker canvas"
      onchange={closePopover}
    />
  </div>
</div>

<style>
  .setting-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    font-size: 0.875rem;
    color: var(--color-text);
  }
  .picker-trigger {
    font-size: 0.75rem;
    gap: var(--space-2);
    padding: var(--space-1_5) var(--space-3);
    border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
    border-radius: var(--space-1);
    text-transform: uppercase;
    anchor-name: --picker-anchor;
  }
  .swatch {
    aspect-ratio: 1;
    border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
    border-radius: var(--space-1);
    cursor: pointer;
    background: transparent;
  }
  .preview {
    width: var(--space-4);
    height: var(--space-4);
    border-radius: 50%;
  }

  .panel {
    border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
    background-color: light-dark(var(--neutral-050), var(--neutral-900-tint));
    border-radius: var(--space-1_5);
    padding: var(--space-3);
    width: 10rem;
    display: flex;
    flex-direction: column;
    gap: var(--space-2_5);
    position: absolute;
    margin: 0;
    inset: auto;
    position-anchor: --picker-anchor;
    top: anchor(bottom);
    right: anchor(right);
    margin-top: var(--space-1_5);
    opacity: 0;
    transform: scale(0.95);
    transition:
      opacity 0.15s ease,
      transform 0.15s cubic-bezier(0.4, 0, 0.2, 1),
      display 0.15s allow-discrete;
  }

  .panel:popover-open {
    opacity: 1;
    transform: scale(1);
  }
  @starting-style {
    .panel:popover-open {
      opacity: 0;
      transform: scale(0.95);
    }
  }

  .grid {
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    gap: var(--space-2);
    padding: var(--space-1);
  }
  .swatch:hover {
    transform: scale(1.1);
  }
  .swatch.active {
    outline: 2px solid var(--color-brand);
    outline-offset: 1px;
  }
  .custom-btn {
    font-size: 0.75rem;
    gap: var(--space-1_5);
    width: 100%;
    padding: var(--space-1_5);
    background-color: light-dark(var(--neutral-100), var(--neutral-800-tint));
    border: 1px solid light-dark(var(--neutral-200), var(--neutral-700-tint));
    border-radius: var(--space-1);
  }
  .sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    border: 0;
  }
</style>
