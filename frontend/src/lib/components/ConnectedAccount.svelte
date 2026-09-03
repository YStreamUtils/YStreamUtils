<script lang="ts">
  import Button from "./base/Button.svelte";

  interface Props {
    platform: string;
    isLoadingProfile: boolean;
    profile?: any;
    isLoggingIn: boolean;
    onConnect: () => Promise<void>;
  }

  let { platform, isLoadingProfile, profile, isLoggingIn, onConnect }: Props =
    $props();

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
    <img src={profile.avatarUrl} alt="Avatar" class="account-avatar" />
    <span class="account-status">Connected</span>
  </div>
{:else}
  <Button onclick={handleConnect} disabled={isLoggingIn}>
    {isLoggingIn ? "Connecting Browser..." : "Link Account"}
  </Button>
{/if}

<style>
  .account-connected {
    display: flex;
    flex-wrap: wrap;
    gap: 12px;
    align-items: center;
  }

  .account-avatar {
    width: 36px;
    height: 36px;
    object-fit: cover;
    border: 1px solid #333;
    border-radius: 50%;
  }

  .account-status {
    font-size: 12px;
    font-weight: bold;
    color: #4caf50;
  }

  .placeholder-content {
    font-size: 0.875rem;
    color: var(--color-text);
    opacity: 0.8;
  }
</style>
