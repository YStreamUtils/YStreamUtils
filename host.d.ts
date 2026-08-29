/**
 * The standard response returned by YouTube messaging operations.
 */
interface YoutubeReplyResponse {
  /**
   * The final operational status.
   */
  status: "success" | "error";
  /**
   * A descriptive status message or error detail.
   */
  message: string;
}

/**
 * Interface for interacting with the host's YouTube API context.
 *
 * **Plugin Permission:** `youtube` (Only required when running as a plugin)
 */
interface YoutubeContext {
  /**
   * Sends a plain message directly to a YouTube Live Chat stream.
   *
   * @param liveChatID - The unique ID of the target Live Chat room.
   * @param message - The raw text message content to transmit.
   */
  sendMessage(liveChatID: string, message: string): void;

  /**
   * Sends a targeted reply to a specific user within a YouTube Live Chat stream.
   *
   * @param liveChatID - The unique ID of the target Live Chat room.
   * @param authorID - The unique ID of the user being replied to.
   * @param text - The raw text message content to transmit.
   * @returns An object containing the operational status and response message.
   */
  replyToMessage(
    liveChatID: string,
    authorID: string,
    text: string,
  ): YoutubeReplyResponse;
}

/**
 * Interface for interacting with the host's network layer.
 *
 * **Plugin Permission:** `network` (Only required when running as a plugin)
 */
interface HostNetwork {
  /**
   * Executes a network request using the host environment's networking stack.
   *
   * @param url - The fully-qualified destination URL endpoint.
   * @param options - Configuration overrides (e.g., headers, body, method).
   * @returns The raw network response payload.
   */
  fetch(url: string, options?: any): any;
}

interface HostCache {
  /**
   * Gets an object from cache (Untyped)
   * @param key The key of the object you want to get from cache
   * @returns The value if it is found, null if it is not.
   */
  get(key: string): any | null;

  /**
   * Gets an object from cache (Typed)
   * @param key The key of the object you want to get from cache
   * @returns The value (as T) if it is found, null if it is not
   */
  get<T>(key: string): T | null;

  /**
   * Adds an object to the cache
   * @param key The key of the object to cache
   * @param value The value of the object to cache
   */
  set(key: string, value: any): void;

  /**
   * Deletes an object from the cache
   * @param key The key of the item to delete
   */
  delete(key: string): void;

  /**
   * Deletes the whole cache
   */
  clear(): void;
}

/**
 * The global object exposed by the script/plugin runtime environment.
 */
declare namespace host {
  /**
   * Writes to the host logs.
   *
   * @param level - The severity threshold tier (`"debug"`, `"info"`, `"warn"`, `"error"`).
   * @param msg - The core log description message text.
   */
  function log(level: "debug" | "info" | "warn" | "error", msg: string): void;

  /**
   * Shared networking capabilities proxy.
   */
  const network: HostNetwork;

  /**
   * Shared YouTube streaming features context proxy.
   */
  const youtube: YoutubeContext;

  /**
   * Scoped Cache (based on plugin namespace or script name)
   */
  const cache: HostCache;
}
