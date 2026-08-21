import { describe, expect, it } from 'vitest';
import { getRelativeTime, getShortDate } from './format-date';

describe('getShortDate', () => {
  it('renders a readable day, month and year', () => {
    expect(getShortDate('2026-08-20T14:02:00+00:00')).toBe('20 Aug 2026');
  });
});

describe('getRelativeTime', () => {
  const now = new Date('2026-08-21T12:00:00+00:00');

  it('counts minutes within the hour', () => {
    expect(getRelativeTime('2026-08-21T11:30:00+00:00', now)).toBe('30m ago');
  });

  it('counts hours within the day', () => {
    expect(getRelativeTime('2026-08-21T06:00:00+00:00', now)).toBe('6h ago');
  });

  it('counts days within the week', () => {
    expect(getRelativeTime('2026-08-18T12:00:00+00:00', now)).toBe('3d ago');
  });

  it('falls back to a date beyond a week', () => {
    expect(getRelativeTime('2026-08-01T12:00:00+00:00', now)).toBe('1 Aug 2026');
  });

  it('does not report a future timestamp as being in the past', () => {
    expect(getRelativeTime('2026-08-21T12:05:00+00:00', now)).toBe('just now');
  });
});
