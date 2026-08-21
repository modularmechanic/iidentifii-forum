import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import CommentService from '@src/domains/comments/CommentService';
import { buildComment, signInAs } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import ReplyActions from './ReplyActions';

const AUTHOR_ID = '01a02518-cb9d-7fca-9a45-73d299654100';

describe('ReplyActions', () => {
  afterEach(() => vi.restoreAllMocks());

  it('shows nothing to somebody who did not write the reply', () => {
    signInAs();

    const { container } = renderWithProviders(<ReplyActions reply={buildComment()} />);

    expect(container).toBeEmptyDOMElement();
  });

  it('edits in place rather than sending the author to another page', async () => {
    signInAs('carol', AUTHOR_ID);
    const update = vi.spyOn(CommentService, 'update').mockResolvedValue(buildComment());

    renderWithProviders(<ReplyActions reply={buildComment()} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Edit' }));

    const box = screen.getByLabelText('Edit your reply');
    expect(box).toHaveValue('At least once.');

    await userEvent.clear(box);
    await userEvent.type(box, 'At least once, deduplicated.');
    await userEvent.click(screen.getByRole('button', { name: 'Save' }));

    await waitFor(() =>
      expect(update).toHaveBeenCalledWith(buildComment().id, 'At least once, deduplicated.'),
    );
  });

  it('asks before deleting a reply', async () => {
    signInAs('carol', AUTHOR_ID);
    const remove = vi.spyOn(CommentService, 'remove').mockResolvedValue();

    renderWithProviders(<ReplyActions reply={buildComment()} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Delete' }));
    expect(remove).not.toHaveBeenCalled();

    await userEvent.click(screen.getByRole('button', { name: 'Yes' }));

    await waitFor(() => expect(remove).toHaveBeenCalledWith(buildComment().id));
  });

  it('will not save an empty reply', async () => {
    signInAs('carol', AUTHOR_ID);

    renderWithProviders(<ReplyActions reply={buildComment()} />);

    await userEvent.click(await screen.findByRole('button', { name: 'Edit' }));
    await userEvent.clear(screen.getByLabelText('Edit your reply'));

    expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled();
  });
});
