import { cn } from '../../lib/cn';

const palettes = [
  'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-200',
  'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-200',
  'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-200',
  'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-200',
  'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-200',
];

function initials(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const letters =
    words.length > 1 ? words[0].charAt(0) + words[words.length - 1].charAt(0) : name.charAt(0);
  return letters.toUpperCase();
}

// The same person always gets the same colour.
function paletteFor(id: string): string {
  let hash = 0;
  for (const char of id) hash = (hash * 31 + char.charCodeAt(0)) >>> 0;
  return palettes[hash % palettes.length];
}

interface AvatarProps {
  id: string;
  name: string;
  size?: 'sm' | 'md';
  className?: string;
}

export function Avatar({ id, name, size = 'md', className }: AvatarProps) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        'inline-grid shrink-0 place-items-center rounded-full font-semibold',
        size === 'sm' ? 'size-7 text-[11px]' : 'size-9 text-xs',
        paletteFor(id),
        className,
      )}
    >
      {initials(name)}
    </span>
  );
}
