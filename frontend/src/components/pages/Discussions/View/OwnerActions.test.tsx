import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import PostService from '@src/domains/posts/PostService';
import { buildPost, signInAs, signInAsModerator } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import OwnerActions from './OwnerActions';

const AUTHOR_ID = '01a02518-cb7b-7e87-b96d-c25b67ec74f0';

describe('OwnerActions', () => {
  afterEach(() => vi.restoreAllMocks());

  it('shows nothing to a reader who did not write it', () => {
    signInAs();

    const { container } = renderWithProviders(<OwnerActions post={buildPost()} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('shows nothing to a moderator either: flagging is not editing', () => {
    signInAsModerator();

    const { container } = renderWithProviders(<OwnerActions post={buildPost()} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('offers the author an edit and a delete', async () => {
    signInAs('bob', AUTHOR_ID);

    renderWithProviders(<OwnerActions post={buildPost()} />);

    expect(await screen.findByRole('link', { name: 'Edit' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument();
  });

  it('asks before deleting, because the replies go too', async () => {
    signInAs('bob', AUTHOR_ID);
    const remove = vi.spyOn(PostService, 'remove').mockResolvedValue();

    renderWithProviders(<OwnerActions post={buildPost()} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Delete' }));

    expect(screen.getByText('Delete this and its replies?')).toBeInTheDocument();
    expect(remove).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: 'Yes, delete' }));

    await waitFor(() => expect(remove).toHaveBeenCalledWith(buildPost().id));
  });

  it('hands focus back to Delete when the author changes their mind', async () => {
    signInAs('bob', AUTHOR_ID);

    renderWithProviders(<OwnerActions post={buildPost()} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Delete' }));
    await userEvent.click(screen.getByRole('button', { name: 'Keep it' }));

    // Answering the question replaces the control that asked it; without returning the focus it
    // would be left on nothing, and a keyboard reader would start again from the top.
    expect(screen.getByRole('button', { name: 'Delete' })).toHaveFocus();
  });

  it('lets the author change their mind', async () => {
    signInAs('bob', AUTHOR_ID);
    const remove = vi.spyOn(PostService, 'remove').mockResolvedValue();

    renderWithProviders(<OwnerActions post={buildPost()} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Delete' }));
    await userEvent.click(screen.getByRole('button', { name: 'Keep it' }));

    expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument();
    expect(remove).not.toHaveBeenCalled();
  });
});
