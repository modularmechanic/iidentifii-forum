import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import PostService from '@src/domains/posts/PostService';
import { buildFlag, buildPost, signInAs, signInAsModerator } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import ModeratorTools from './ModeratorTools';

describe('ModeratorTools', () => {
  afterEach(() => vi.restoreAllMocks());

  it('shows nothing to somebody who is not signed in', () => {
    const { container } = renderWithProviders(<ModeratorTools post={buildPost()} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('shows nothing to an ordinary member', () => {
    signInAs();

    const { container } = renderWithProviders(<ModeratorTools post={buildPost()} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('offers a moderator the flag', async () => {
    signInAsModerator();
    const flag = vi.spyOn(PostService, 'flag').mockResolvedValue();

    renderWithProviders(<ModeratorTools post={buildPost()} />);

    await userEvent.click(
      await screen.findByRole('button', { name: 'Flag as misleading or false' }),
    );

    await waitFor(() => expect(flag).toHaveBeenCalledWith(buildPost().id, 'MisleadingOrFalse'));
  });

  it('offers to take an existing flag off', async () => {
    signInAsModerator();
    const unflag = vi.spyOn(PostService, 'unflag').mockResolvedValue();

    renderWithProviders(<ModeratorTools post={buildPost({ tags: [buildFlag()] })} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Remove the flag' }));

    await waitFor(() => expect(unflag).toHaveBeenCalledWith(buildPost().id, 'MisleadingOrFalse'));
  });

  it('says so when the flag could not be changed', async () => {
    signInAsModerator();
    vi.spyOn(PostService, 'flag').mockRejectedValue(new Error('offline'));

    renderWithProviders(<ModeratorTools post={buildPost()} />);

    await userEvent.click(
      await screen.findByRole('button', { name: 'Flag as misleading or false' }),
    );

    expect(await screen.findByText(/Could not change the flag/)).toBeInTheDocument();
  });
});
