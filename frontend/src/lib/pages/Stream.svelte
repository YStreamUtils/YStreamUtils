<script lang="ts">
</script>

<div class="stream-grid">
    <div class="grid-left"></div>
    <div class="grid-right"></div>
</div>

<style>
    .stream-grid {
        width: 100%;
        height: 100%;
        display: grid;
        grid-template-columns: 250px 1fr;
        border: 1px solid transparent;
        border-radius: var(--space-4);
    }

    .grid-left {
        background: var(--neutral-800-tint);
    }

    .grid-right {
        background: var(--neutral-800);
    }
</style>
