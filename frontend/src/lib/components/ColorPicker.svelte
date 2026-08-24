<script lang="ts">
  import { Paintbrush } from "@lucide/svelte";

  interface Props {
    value?: string;
    class?: string;
  }

  let { value = $bindable("#9900ff"), class: className = "" }: Props = $props();

  const id = $props.id();

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

<button
  class="btn btn-center picker-trigger {className}"
  style="anchor-name: --picker-anchor-{id};"
  title="Open color picker"
  popovertarget={id}
>
  <div class="swatch preview" style:background-color={value}></div>
  <span class="hex">{value}</span>
</button>

<div
  bind:this={popoverElement}
  {id}
  popover
  class="panel"
  style="position-anchor: --picker-anchor-{id};"
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

<style>
.picker-trigger {
  gap: var(--space-2);
  padding: var(--space-1_5) var(--space-3);
  font-size: 0.75rem;
  text-transform: uppercase;
  border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
  border-radius: var(--space-1);
}

.swatch {
  aspect-ratio: 1;
  cursor: pointer;
  background: transparent;
  border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
  border-radius: var(--space-1);
}

.preview {
  width: var(--space-4);
  height: var(--space-4);
  border-radius: 50%;
}

.panel {
  position: absolute;
  inset: auto;
  top: anchor(bottom);
  right: anchor(right);
  flex-direction: column;
  gap: var(--space-2_5);
  width: 10rem;
  padding: var(--space-3);
  margin: 0;
  margin-top: var(--space-1_5);
  background-color: light-dark(var(--neutral-050), var(--neutral-900-tint));
  border: 1px solid light-dark(var(--neutral-200), var(--neutral-800-tint));
  border-radius: var(--space-1_5);
}

@media (prefers-reduced-motion: no-preference) {
  .panel {
    opacity: 0;
    transform: scale(0.95);
    transition:
      opacity 0.15s ease,
      transform 0.15s cubic-bezier(0.4, 0, 0.2, 1),
      display 0.15s allow-discrete;
  }

  @media(hover: hover){
    .swatch:hover {
      transform: scale(1.1);
    }
  }
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
  grid-template-columns: repeat(4, [content] minmax(0, 1fr));
  gap: var(--space-2);
  padding: var(--space-1);
}

.swatch.active {
  outline: 2px solid var(--color-brand);
  outline-offset: 1px;
}

.custom-btn {
  gap: var(--space-1_5);
  width: 100%;
  padding: var(--space-1_5);
  font-size: 0.75rem;
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
  white-space: nowrap;
  border: 0;
  clip-path: none;
}
</style>
