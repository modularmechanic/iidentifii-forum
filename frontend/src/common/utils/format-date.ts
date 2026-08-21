/***** Constants *****/

const DATE_FORMAT = new Intl.DateTimeFormat('en-GB', {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

const MINUTE = 60_000;
const HOUR = 60 * MINUTE;
const DAY = 24 * HOUR;

/***** Functions *****/

/** Renders a timestamp as a short date, for example "20 Aug 2026". */
export function getShortDate(isoTimestamp: string): string {
  return DATE_FORMAT.format(new Date(isoTimestamp));
}

/** Renders how long ago something happened, falling back to a date after a week. */
export function getRelativeTime(isoTimestamp: string, now: Date = new Date()): string {
  const elapsed = now.getTime() - new Date(isoTimestamp).getTime();

  if (elapsed < HOUR) {
    const minutes = Math.max(1, Math.floor(elapsed / MINUTE));
    return `${minutes}m ago`;
  }

  if (elapsed < DAY) {
    return `${Math.floor(elapsed / HOUR)}h ago`;
  }

  if (elapsed < 7 * DAY) {
    return `${Math.floor(elapsed / DAY)}d ago`;
  }

  return getShortDate(isoTimestamp);
}
