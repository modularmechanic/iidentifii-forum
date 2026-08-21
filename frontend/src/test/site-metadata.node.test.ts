import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

/***** Constants *****/

const INDEX_HTML = readFileSync('index.html', 'utf8');
const ROBOTS = readFileSync('public/robots.txt', 'utf8');
const LLMS = readFileSync('public/llms.txt', 'utf8');

/***** Tests *****/

describe('page metadata', () => {
  it('describes the forum for search results', () => {
    expect(INDEX_HTML).toContain('<title>');
    expect(INDEX_HTML).toMatch(/<meta\s+name="description"/);
    expect(INDEX_HTML).toMatch(/<link rel="canonical"/);
  });

  it('describes the forum for a shared link', () => {
    for (const property of ['og:type', 'og:title', 'og:description', 'og:url']) {
      expect(INDEX_HTML).toContain(`property="${property}"`);
    }
    expect(INDEX_HTML).toContain('name="twitter:card"');
  });

  it('carries a colour for both themes', () => {
    expect(INDEX_HTML).toMatch(/theme-color.*prefers-color-scheme: dark/);
    expect(INDEX_HTML).toMatch(/theme-color.*prefers-color-scheme: light/);
  });
});

describe('robots.txt', () => {
  it('welcomes crawlers to the discussions', () => {
    expect(ROBOTS).toMatch(/User-agent: \*/);
    expect(ROBOTS).toMatch(/Allow: \/discussions/);
  });

  /** These carry one-time tokens, and none of them is worth indexing. */
  it('keeps crawlers away from the account pages and the API', () => {
    for (const path of ['/api/', '/verify-email', '/reset-password', '/login', '/register']) {
      expect(ROBOTS).toContain(`Disallow: ${path}`);
    }
  });
});

describe('llms.txt', () => {
  it('says what the forum is and points at the API', () => {
    expect(LLMS).toMatch(/^# iiDENTIFii Forum/);
    expect(LLMS).toContain('/api/v1');
    expect(LLMS).toContain('api-endpoints.md');
  });

  it('warns that content is written by members and may be flagged', () => {
    expect(LLMS).toMatch(/misleading or false/);
    expect(LLMS).toMatch(/429/);
  });
});
