import { LOCALE } from './money';

const dayFormatter = new Intl.DateTimeFormat(LOCALE, {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  timeZone: 'UTC',
});

const relativeFormatter = new Intl.RelativeTimeFormat(LOCALE, { numeric: 'auto' });

/** Formats a date-only value like "2026-10-12" as "12 Oct 2026". */
export function formatDate(isoDate: string): string {
  return dayFormatter.format(new Date(`${isoDate}T00:00:00Z`));
}

/** Splits a date-only value into the short month and day shown in expense lists. */
export function dateParts(isoDate: string): { month: string; day: string } {
  const date = new Date(`${isoDate}T00:00:00Z`);
  return {
    month: date.toLocaleString(LOCALE, { month: 'short', timeZone: 'UTC' }),
    day: String(date.getUTCDate()),
  };
}

/** Today's date in the user's time zone as "YYYY-MM-DD". */
export function todayIso(): string {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
}

const units: Array<[Intl.RelativeTimeFormatUnit, number]> = [
  ['year', 365 * 24 * 3600],
  ['month', 30 * 24 * 3600],
  ['week', 7 * 24 * 3600],
  ['day', 24 * 3600],
  ['hour', 3600],
  ['minute', 60],
];

/** "5 minutes ago", "yesterday", "3 weeks ago"... */
export function timeAgo(isoDateTime: string, now: Date = new Date()): string {
  const seconds = (new Date(isoDateTime).getTime() - now.getTime()) / 1000;
  for (const [unit, size] of units) {
    if (Math.abs(seconds) >= size) {
      return relativeFormatter.format(Math.round(seconds / size), unit);
    }
  }
  return 'just now';
}
