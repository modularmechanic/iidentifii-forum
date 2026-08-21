import { act } from 'react';
import { renderHook } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router';
import { describe, expect, it } from 'vitest';
import { useListCriteria } from './useListCriteria';

/***** Functions *****/

function renderCriteria(initialPath = '/') {
  return renderHook(() => ({ ...useListCriteria(), search: useLocation().search }), {
    wrapper: ({ children }) => (
      <MemoryRouter initialEntries={[initialPath]}>{children}</MemoryRouter>
    ),
  });
}

/***** Tests *****/

describe('useListCriteria', () => {
  it('starts on the first page, newest first', () => {
    const { result } = renderCriteria();

    expect(result.current.criteria).toMatchObject({
      page: 1,
      sort: 'CreatedAt',
      order: 'Descending',
    });
    expect(result.current.hasFilters).toBe(false);
  });

  it('reads criteria out of the address', () => {
    const { result } = renderCriteria('/?page=3&sort=LikeCount&order=Ascending&author=alice');

    expect(result.current.criteria).toMatchObject({
      page: 3,
      sort: 'LikeCount',
      order: 'Ascending',
      author: 'alice',
    });
    expect(result.current.hasFilters).toBe(true);
  });

  it('ignores values the API would not recognise', () => {
    const { result } = renderCriteria('/?page=nonsense&sort=Sideways&tag=NotATag');

    expect(result.current.criteria).toMatchObject({ page: 1, sort: 'CreatedAt' });
    expect(result.current.criteria.tag).toBeUndefined();
  });

  it('ignores a date that is not a real calendar date', () => {
    const { result } = renderCriteria('/?from=not-a-date&to=2026-13-45');

    expect(result.current.criteria.from).toBeUndefined();
    expect(result.current.criteria.to).toBeUndefined();
  });

  it('drops a range that ends before it starts', () => {
    const { result } = renderCriteria('/?from=2026-08-20&to=2026-08-01');

    expect(result.current.criteria.from).toBeUndefined();
    expect(result.current.criteria.to).toBeUndefined();
    expect(result.current.hasFilters).toBe(false);
  });

  it('keeps a range that is the right way round', () => {
    const { result } = renderCriteria('/?from=2026-08-01&to=2026-08-20');

    expect(result.current.criteria).toMatchObject({ from: '2026-08-01', to: '2026-08-20' });
  });

  it('ignores an author name no account could have', () => {
    const { result } = renderCriteria(`/?author=${'x'.repeat(33)}`);

    expect(result.current.criteria.author).toBeUndefined();
  });

  it('ignores a page number beyond what the API accepts', () => {
    const { result } = renderCriteria('/?page=2147483647');

    expect(result.current.criteria.page).toBe(1);
  });

  it('writes a change into the address so the view can be shared', () => {
    const { result } = renderCriteria();

    act(() => result.current.update({ author: 'bob' }));

    expect(result.current.search).toContain('author=bob');
    expect(result.current.criteria.author).toBe('bob');
  });

  it('leaves defaults out of the address', () => {
    const { result } = renderCriteria();

    act(() => result.current.update({ sort: 'CreatedAt', order: 'Descending' }));

    expect(result.current.search).not.toContain('sort=');
    expect(result.current.search).not.toContain('page=');
  });

  it('returns to the first page when a filter changes', () => {
    const { result } = renderCriteria('/?page=4');

    act(() => result.current.update({ author: 'carol' }));

    expect(result.current.criteria.page).toBe(1);
  });

  it('keeps the page when only the page changes', () => {
    const { result } = renderCriteria('/?author=carol');

    act(() => result.current.update({ page: 2 }));

    expect(result.current.criteria).toMatchObject({ page: 2, author: 'carol' });
  });

  it('clears every filter at once', () => {
    const { result } = renderCriteria('/?author=carol&tag=MisleadingOrFalse&page=2');

    act(() => result.current.clear());

    expect(result.current.search).toBe('');
    expect(result.current.hasFilters).toBe(false);
  });

  it('keeps the chosen ordering when the filters are cleared', () => {
    const { result } = renderCriteria('/?sort=LikeCount&order=Ascending&author=carol');

    act(() => result.current.clear());

    expect(result.current.criteria).toMatchObject({ sort: 'LikeCount', order: 'Ascending' });
    expect(result.current.criteria.author).toBeUndefined();
    expect(result.current.criteria.page).toBe(1);
  });
});
