import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useNavigate } from 'react-router';
import { afterEach, describe, expect, it, vi } from 'vitest';
import PostService from '@src/domains/posts/PostService';
import type { IPaged } from '@src/domains/common/types/Paged';
import type { IPost } from '@src/domains/posts/Post';
import { buildPost } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import PostsContainer from './PostsContainer';

/***** Functions *****/

function buildPage(overrides: Partial<IPaged<IPost>> = {}): IPaged<IPost> {
  return {
    items: [buildPost()],
    page: 1,
    pageSize: 10,
    totalCount: 25,
    totalPages: 3,
    hasPrevious: false,
    hasNext: true,
    ...overrides,
  };
}

/** Changes the address without remounting the container, the way a link or the back button does. */
function GoToBob() {
  const navigate = useNavigate();

  return (
    <button onClick={() => void navigate('/?author=bob')} type="button">
      Go to bob
    </button>
  );
}

/***** Tests *****/

describe('PostsContainer', () => {
  afterEach(() => vi.restoreAllMocks());

  it('shows progress while the first page loads', () => {
    vi.spyOn(PostService, 'fetchPage').mockReturnValue(new Promise(() => {}));

    renderWithProviders(<PostsContainer />);

    expect(screen.getByRole('status')).toHaveTextContent('Loading discussions');
  });

  it('renders the page and how many discussions there are in total', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(buildPage());

    renderWithProviders(<PostsContainer />);

    expect(await screen.findByText('25 discussions')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Liveness webhook retries' })).toBeInTheDocument();
  });

  it('asks for the next page when the reader moves on', async () => {
    const fetchPage = vi.spyOn(PostService, 'fetchPage').mockResolvedValue(buildPage());

    renderWithProviders(<PostsContainer />);
    await screen.findByText('25 discussions');

    await userEvent.click(screen.getByRole('button', { name: 'Next' }));

    await waitFor(() =>
      expect(fetchPage).toHaveBeenLastCalledWith(
        expect.objectContaining({ page: 2, pageSize: 10 }),
      ),
    );
  });

  it('opens the filter panel already on when the address carries filters', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(buildPage());

    renderWithProviders(<PostsContainer />, '/?author=bob');

    expect(
      await screen.findByRole('switch', { name: 'Filters', checked: true }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText('Author')).toHaveValue('bob');
  });

  it('opens the filter panel when the address gains filters after the first render', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(buildPage());

    renderWithProviders(
      <>
        <GoToBob />
        <PostsContainer />
      </>,
    );
    await screen.findByText('25 discussions');
    expect(screen.queryByLabelText('Author')).not.toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: 'Go to bob' }));

    expect(await screen.findByLabelText('Author')).toHaveValue('bob');
    expect(screen.getByRole('switch', { name: 'Filters' })).toBeChecked();
  });

  it('leaves the filter panel open after clearing, so the empty fields stay in view', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(buildPage());

    renderWithProviders(<PostsContainer />, '/?author=bob');
    await screen.findByDisplayValue('bob');

    await userEvent.click(screen.getByRole('button', { name: 'Clear' }));

    expect(screen.getByLabelText('Author')).toBeInTheDocument();
    expect(screen.getByRole('switch', { name: 'Filters' })).toBeChecked();
  });

  it('turning the filter switch off removes the filters as well as the panel', async () => {
    const fetchPage = vi.spyOn(PostService, 'fetchPage').mockResolvedValue(buildPage());

    renderWithProviders(<PostsContainer />, '/?author=bob');
    await screen.findByRole('switch', { name: 'Filters' });

    await userEvent.click(screen.getByRole('switch', { name: 'Filters' }));

    expect(screen.queryByLabelText('Author')).not.toBeInTheDocument();
    await waitFor(() =>
      expect(fetchPage).toHaveBeenLastCalledWith(expect.objectContaining({ author: undefined })),
    );
  });

  it('clearing empties the fields as well as the filters', async () => {
    const fetchPage = vi.spyOn(PostService, 'fetchPage').mockResolvedValue(buildPage());

    renderWithProviders(<PostsContainer />, '/?author=bob');
    await screen.findByDisplayValue('bob');

    await userEvent.click(screen.getByRole('button', { name: 'Clear' }));

    expect(screen.getByLabelText('Author')).toHaveValue('');
    await waitFor(() =>
      expect(fetchPage).toHaveBeenLastCalledWith(expect.objectContaining({ author: undefined })),
    );
  });

  /** A browser's own wording for an unreachable server used to be shown to the reader. */
  it("offers a retry when the request fails, in the forum's own words", async () => {
    const fetchPage = vi
      .spyOn(PostService, 'fetchPage')
      .mockRejectedValue(new Error('Failed to fetch'));

    renderWithProviders(<PostsContainer />);

    const alert = await screen.findByRole('alert');
    expect(alert).toHaveTextContent('Could not load the discussions.');
    expect(alert).not.toHaveTextContent('Failed to fetch');

    await userEvent.click(screen.getByRole('button', { name: 'Retry' }));

    await waitFor(() => expect(fetchPage).toHaveBeenCalledTimes(2));
  });
});
