import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import CommentService from '@src/domains/comments/CommentService';
import { buildComment, signInAs } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import ReplyComposer from './ReplyComposer';

const POST_ID = '01a02518-cbf8-7b23-8b12-613e5bf64a39';

describe('ReplyComposer', () => {
  afterEach(() => vi.restoreAllMocks());

  it('asks an anonymous reader to log in rather than offering a box that will be refused', () => {
    renderWithProviders(<ReplyComposer postId={POST_ID} />);

    expect(screen.getByRole('link', { name: 'Log in' })).toBeInTheDocument();
    expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
  });

  it('posts a reply for a member', async () => {
    signInAs();
    const create = vi.spyOn(CommentService, 'create').mockResolvedValue(buildComment());

    renderWithProviders(<ReplyComposer postId={POST_ID} />);

    await userEvent.type(screen.getByLabelText('Your reply'), 'At least once.');
    await userEvent.click(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() => expect(create).toHaveBeenCalledWith(POST_ID, 'At least once.'));
  });

  it('empties the box once the reply is in', async () => {
    signInAs();
    vi.spyOn(CommentService, 'create').mockResolvedValue(buildComment());

    renderWithProviders(<ReplyComposer postId={POST_ID} />);

    const box = screen.getByLabelText('Your reply');
    await userEvent.type(box, 'Posted.');
    await userEvent.click(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() => expect(box).toHaveValue(''));
  });

  it('will not post an empty reply', () => {
    signInAs();

    renderWithProviders(<ReplyComposer postId={POST_ID} />);

    expect(screen.getByRole('button', { name: 'Post reply' })).toBeDisabled();
  });

  it('says so when the reply could not be posted', async () => {
    signInAs();
    vi.spyOn(CommentService, 'create').mockRejectedValue(new Error('offline'));

    renderWithProviders(<ReplyComposer postId={POST_ID} />);

    await userEvent.type(screen.getByLabelText('Your reply'), 'Trying.');
    await userEvent.click(screen.getByRole('button', { name: 'Post reply' }));

    expect(await screen.findByText(/Could not post the reply/)).toBeInTheDocument();
  });
});
