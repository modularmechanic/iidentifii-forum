import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import CommentService from '@src/domains/comments/CommentService';
import PostService from '@src/domains/posts/PostService';
import { buildComment, buildPost, signInAs } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import PostsContainer from './PostsContainer';

/***** Functions *****/

function listOf(post = buildPost()) {
  return {
    items: [post],
    page: 1,
    pageSize: 10,
    totalCount: 1,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false,
  };
}

/***** Tests *****/

describe('replies in the list', () => {
  afterEach(() => vi.restoreAllMocks());

  it('does not fetch replies until a reader asks for them', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf());
    const fetchReplies = vi.spyOn(CommentService, 'fetchPage');

    renderWithProviders(<PostsContainer />);
    await screen.findByRole('link', { name: 'Liveness webhook retries' });

    expect(fetchReplies).not.toHaveBeenCalled();
  });

  it('expands the replies when the count is pressed', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf());
    const fetchReplies = vi.spyOn(CommentService, 'fetchPage').mockResolvedValue({
      items: [buildComment({ body: 'At least once.' })],
      page: 1,
      pageSize: 3,
      totalCount: 7,
      totalPages: 3,
      hasPrevious: false,
      hasNext: true,
    });

    renderWithProviders(<PostsContainer />);

    const toggle = await screen.findByRole('button', { name: '7 replies' });
    expect(toggle).toHaveAttribute('aria-expanded', 'false');

    await userEvent.click(toggle);

    expect(await screen.findByText('At least once.')).toBeInTheDocument();
    expect(toggle).toHaveAttribute('aria-expanded', 'true');
    await waitFor(() => expect(fetchReplies).toHaveBeenCalledOnce());
  });

  it('points at the discussion when there are more replies than it shows', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf());
    vi.spyOn(CommentService, 'fetchPage').mockResolvedValue({
      items: [buildComment()],
      page: 1,
      pageSize: 3,
      totalCount: 7,
      totalPages: 3,
      hasPrevious: false,
      hasNext: true,
    });

    renderWithProviders(<PostsContainer />);
    await userEvent.click(await screen.findByRole('button', { name: '7 replies' }));

    expect(await screen.findByRole('link', { name: 'See all 7 replies' })).toBeInTheDocument();
  });

  it('closes again when the count is pressed a second time', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf());
    vi.spyOn(CommentService, 'fetchPage').mockResolvedValue({
      items: [buildComment({ body: 'At least once.' })],
      page: 1,
      pageSize: 3,
      totalCount: 7,
      totalPages: 3,
      hasPrevious: false,
      hasNext: true,
    });

    renderWithProviders(<PostsContainer />);
    const toggle = await screen.findByRole('button', { name: '7 replies' });

    await userEvent.click(toggle);
    await screen.findByText('At least once.');
    await userEvent.click(toggle);

    expect(screen.queryByText('At least once.')).not.toBeInTheDocument();
  });

  it('lets a member answer without leaving the list', async () => {
    signInAs();
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf());
    vi.spyOn(CommentService, 'fetchPage').mockResolvedValue({
      items: [buildComment()],
      page: 1,
      pageSize: 3,
      totalCount: 7,
      totalPages: 3,
      hasPrevious: false,
      hasNext: true,
    });
    const create = vi.spyOn(CommentService, 'create').mockResolvedValue(buildComment());

    renderWithProviders(<PostsContainer />);
    await userEvent.click(await screen.findByRole('button', { name: '7 replies' }));

    await userEvent.type(await screen.findByLabelText('Your reply'), 'Answered from the list.');
    await userEvent.click(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() =>
      expect(create).toHaveBeenCalledWith(buildPost().id, 'Answered from the list.'),
    );
  });

  it('asks an anonymous reader to log in instead of offering a box', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf());
    vi.spyOn(CommentService, 'fetchPage').mockResolvedValue({
      items: [buildComment()],
      page: 1,
      pageSize: 3,
      totalCount: 7,
      totalPages: 3,
      hasPrevious: false,
      hasNext: true,
    });

    renderWithProviders(<PostsContainer />);
    await userEvent.click(await screen.findByRole('button', { name: '7 replies' }));

    expect(await screen.findByRole('link', { name: 'Log in' })).toBeInTheDocument();
    expect(screen.queryByLabelText('Your reply')).not.toBeInTheDocument();
  });

  it('offers nothing to expand when there are no replies', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf(buildPost({ commentCount: 0 })));

    renderWithProviders(<PostsContainer />);

    expect(await screen.findByText('No replies yet')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /replies/ })).not.toBeInTheDocument();
  });
});
