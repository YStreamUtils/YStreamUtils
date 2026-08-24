import type { Platform, UserProfile } from "../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";

export const profiles = $state<Partial<Record<Platform, UserProfile | null>>>({})