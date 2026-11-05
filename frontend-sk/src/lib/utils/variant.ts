import type { SvelteHTMLElements } from 'svelte/elements';

export type DesignSystemColor = 'accent' | 'surface' | 'success' | 'warning' | 'error' | 'transparent' | 'custom';
export type VariantAlign = 'left' | 'right' | 'center';
export type DesignSystemVariant = 'solid' | 'outline' | 'glass';

export interface BaseVisuals {
  variant?: DesignSystemVariant;
  color?: DesignSystemColor;
  customColor?: string;
}

export interface ButtonVisuals extends BaseVisuals {
  
  align?: VariantAlign;
  fullWidth?: boolean;
  active?: boolean;
}

export interface CardVisuals extends BaseVisuals {
  elevation?: 'none' | 'low' | 'high';
  fullWidth?: boolean;
}

export interface DesignSystemRegistry {
  button: ButtonVisuals;
  a: ButtonVisuals;
  div: CardVisuals;
}

export type VariantProps<T extends keyof SvelteHTMLElements> =
    (T extends keyof DesignSystemRegistry ? DesignSystemRegistry[T] : Record<string, never>) &
    SvelteHTMLElements[T];

export type ButtonProps = VariantProps<'button'>;
export type LinkProps = VariantProps<'a'>;
export type CardProps = VariantProps<'div'>;
