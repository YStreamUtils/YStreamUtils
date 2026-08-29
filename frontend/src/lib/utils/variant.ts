import type { SvelteHTMLElements } from 'svelte/elements';

export type VariantColor = 'accent' | 'surface' | 'success' | 'warning' | 'error' | 'transparent';
export type VariantAlign = 'left' | 'right' | 'center';

export type VariantProps<T extends SvelteHTMLElements[keyof SvelteHTMLElements]> = T & {
  variant?: VariantColor;
  align?: VariantAlign;
  fullWidth?: boolean;
};
