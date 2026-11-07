<script lang="ts">
    import type {Component, Snippet} from 'svelte';
    import type {HTMLSelectAttributes} from 'svelte/elements';
    import type {VariantProps} from '$lib/utils/variant';
    import Dropdown from './Dropdown.svelte';
    import Button from './Button.svelte';
    import type {LucideProps} from "@lucide/svelte";

    export interface SelectEntry {
        display: string;
        value: string;
        Icon: Component<LucideProps, {}, ''> | null;
    }

    interface Props extends VariantProps<'button'> {
        id: string;
        entries: SelectEntry[];
        value: string;
        active?: boolean;
    }

    let {
        color = 'surface',
        variant = 'solid',
        align = 'center',
        fullWidth = false,
        value = $bindable(''),
        class: className = '',
        active = false,
        id,
        entries,
        style,
        ...restProps
    }: Props = $props();

    let selectedEntry = $derived(entries.find((e) => e.value === value));
</script>

<Dropdown {id} width={fullWidth ? '100%' : '12rem'}>
    {#snippet trigger()}
        <Button popovertarget={id} class={className} {variant} {align} {fullWidth} {active} {style} type="button">
            <div class="select-trigger-content">
                {#if selectedEntry?.Icon}
                    {const Icon = selectedEntry.Icon}
                    <span class="entry-icon-wrapper">
                        <Icon size="16"/>
                    </span>
                {/if}

                <span class="select-text">
          {selectedEntry ? selectedEntry.display : 'Select an option...'}
        </span>

                <span class="chevron" aria-hidden="true">▼</span>
            </div>
        </Button>
    {/snippet}

    {#snippet content(close)}
        <div class="select-menu-list">
            {#each entries as entry}
                <Button
                        color="transparent"
                        align="left"
                        fullWidth
                        active={value === entry.value}
                        type="button"
                        onclick={() => {
            value = entry.value;
            close();
          }}>
                    <div class="menu-item-content">
                        {#if entry.Icon}
                            {const Icon = entry.Icon}
                            <span class="entry-icon-wrapper">
                <Icon size="16"/>
              </span>
                        {/if}
                        <span>{entry.display}</span>
                    </div>
                </Button>
            {/each}
        </div>
    {/snippet}
</Dropdown>

<select {value} {...restProps} class="visually-hidden" aria-hidden="true" tabindex="-1">
    {#each entries as entry}
        <option value={entry.value}>{entry.display}</option>
    {/each}
</select>

<style>
    .select-trigger-content,
    .menu-item-content {
        display: flex;
        flex-wrap: nowrap;
        gap: var(--space-2, 8px);
        align-items: center;
        width: 100%;
    }

    .select-trigger-content {
        justify-content: space-between;
    }

    .select-text {
        flex-grow: 1;
        overflow: hidden;
        text-overflow: ellipsis;
        text-align: left;
        white-space: nowrap;
    }

    .chevron {
        margin-left: auto;
        font-size: 0.7em;
        opacity: 0.7;
    }

    .select-menu-list {
        display: flex;
        flex-direction: column;
        gap: var(--space-0_5, 2px);
    }

    .entry-icon-wrapper {
        display: inline-flex;
        flex-shrink: 0;
        flex-wrap: nowrap;
        align-items: center;
        justify-content: center;
    }

    .visually-hidden {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        white-space: nowrap;
        border: 0;
    }
</style>
