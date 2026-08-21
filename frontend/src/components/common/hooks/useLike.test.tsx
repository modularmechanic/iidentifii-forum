import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import PostService from '@src/domains/posts/PostService';
import { buildPost, signInAs } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import PostsContainer from './../../pages/Home/PostsContainer';

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

describe('liking from the list', () => {
  afterEach(() => vi.restoreAllMocks());

  it('tells an anonymous reader to log in, and does not offer the action', async () => {
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf());
    const like = vi.spyOn(PostService, 'like');

    renderWithProviders(<PostsContainer />);

    const button = await screen.findByRole('button', { name: 'Like this discussion' });
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute('title', 'Log in to like');

    await userEvent.click(button);
    expect(like).not.toHaveBeenCalled();
  });

  it('refuses to let a member like their own discussion', async () => {
    const author = signInAs('bob', '01a02518-cb7b-7e87-b96d-c25b67ec74f0');
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(
      listOf(buildPost({ author: { id: author.id, username: author.username } })),
    );

    renderWithProviders(<PostsContainer />);

    const button = await screen.findByRole('button', { name: 'Like this discussion' });
    await waitFor(() => expect(button).toBeDisabled());
    expect(button).toHaveAttribute('title', 'You cannot like your own discussion');
  });

  it('likes somebody elses discussion', async () => {
    signInAs();
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf());
    const like = vi.spyOn(PostService, 'like').mockResolvedValue();

    renderWithProviders(<PostsContainer />);

    const button = await screen.findByRole('button', { name: 'Like this discussion' });
    await waitFor(() => expect(button).toBeEnabled());
    await userEvent.click(button);

    await waitFor(() => expect(like).toHaveBeenCalledWith(buildPost().id));
  });

  it('takes a like back when it has already been given', async () => {
    signInAs();
    vi.spyOn(PostService, 'fetchPage').mockResolvedValue(listOf(buildPost({ likedByMe: true })));
    const unlike = vi.spyOn(PostService, 'unlike').mockResolvedValue();

    renderWithProviders(<PostsContainer />);

    await userEvent.click(await screen.findByRole('button', { name: 'Remove your like' }));

    await waitFor(() => expect(unlike).toHaveBeenCalledWith(buildPost().id));
  });
});
