import type { UserProfile } from "../../bindings/github.com/ystreamutils/YStreamUtils/internal/models";

export const profiles = $state<Record<string, UserProfile | null>>({})