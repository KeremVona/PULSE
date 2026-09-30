import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function makeClassName(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

export const cn = makeClassName;
