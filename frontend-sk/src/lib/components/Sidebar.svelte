<script lang="ts">
	import Button from '$lib/components/base/Button.svelte';
	import { House, Tv, Settings, FileTerminal, Puzzle } from '@lucide/svelte';
	import { sidebar } from '$lib/state/navigation.svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import type { RouteId } from '$app/types';
</script>

<div id="sidebar" class="sidebar">
	<nav class="top-nav">
		{@render navButton(House, '/', 'Home')}
		{@render navButton(Tv, '/stream', 'Stream')}
		{@render navButton(FileTerminal, '/script', 'Script')}
	</nav>

	<div class="bottom-nav">
		{@render navButton(Puzzle, '/plugin', 'Plugins')}
		{@render navButton(Settings, '/settings', 'Settings')}
	</div>
</div>

{#snippet navButton(icon: typeof House, path: RouteId, title: string)}
	{@const resolvedPath = resolve(path)}

	<Button
		variant="transparent"
		align="left"
		{icon}
		fullWidth={true}
		active={page.url.pathname === resolvedPath}
		onclick={() => goto(resolvedPath)}
	>
		<p class:hidden={!sidebar.isOpen}>{title}</p>
	</Button>
{/snippet}

<style>
	.sidebar {
		box-sizing: border-box;
		display: flex;
		flex-direction: column;
		height: 100%;
		padding: var(--space-1_5);
		overflow: hidden;
		color: var(--color-text);
		background: light-dark(var(--neutral-100-tint), var(--neutral-900-tint));
		border: none;
		box-shadow: 0 0 10px rgb(0 0 0 / 20%);
	}

	.top-nav,
	.bottom-nav {
		display: flex;
		flex-direction: column;
		gap: var(--space-1_5);
		overflow-x: hidden;
	}

	.top-nav {
		flex-grow: 1;
		min-height: 0;
		overflow-y: auto;
	}

	.bottom-nav {
		margin-top: auto;
	}
</style>
