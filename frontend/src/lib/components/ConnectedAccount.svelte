<script lang="ts">
  interface Props {
    platform: string;
    isLoadingProfile: boolean;
    profile?: any;
    isLoggingIn: boolean;
    onConnect: () => Promise<void>;
  }

  let { 
    platform,
    isLoadingProfile,
    profile,
    isLoggingIn,
    onConnect
  }: Props = $props();

  const handleConnect = async () => {
    try {
      await onConnect();
    } catch (err) {
      console.error(`Failed to connect ${platform}:`, err);
    }
  };
</script>

{#if isLoadingProfile}
    <p class="placeholder-content">Checking authentication metrics...</p>
  {:else if profile}
    <div class="account-connected">
      <img
        src={profile.avatarUrl}
        alt="Avatar"
        class="account-avatar"
      />
      <span class="account-status">Connected</span>
    </div>
  {:else}
    <button
      onclick={handleConnect}
      disabled={isLoggingIn}
      class="btn btn-connect"
    >
      {isLoggingIn ? "Connecting Browser..." : "Link Account"}
    </button>
  {/if}

<style>
  .account-connected {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .account-avatar {
    width: 36px;
    height: 36px;
    border-radius: 50%;
    object-fit: cover;
    border: 1px solid #333;
  }

  .account-status {
    font-size: 12px;
    color: #4caf50;
    font-weight: bold;
  }

  .placeholder-content {
    font-size: 0.875rem;
    color: var(--color-text);
    opacity: 0.8;
  }

  .btn-connect {
    background: #ff4a4a;
    color: white;
    border: none;
    padding: 8px 16px;
    border-radius: 4px;
    font-weight: bold;
    cursor: pointer;
  }
</style>