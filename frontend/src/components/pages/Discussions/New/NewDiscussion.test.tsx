import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import PostService from '@src/domains/posts/PostService';
import { buildPost, signInAs } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import { HttpError } from '@src/infra/http';
import NewDiscussion from './NewDiscussion';

describe('NewDiscussion', () => {
  afterEach(() => vi.restoreAllMocks());

  it('sends what was written', async () => {
    signInAs();
    const create = vi.spyOn(PostService, 'create').mockResolvedValue(buildPost());

    renderWithProviders(<NewDiscussion />);

    await userEvent.type(screen.getByLabelText('Title'), 'Webhook retries');
    await userEvent.type(screen.getByLabelText('Body'), 'How many times does it retry?');
    await userEvent.click(screen.getByRole('button', { name: 'Post discussion' }));

    await waitFor(() =>
      expect(create).toHaveBeenCalledWith({
        title: 'Webhook retries',
        body: 'How many times does it retry?',
      }),
    );
  });

  it('will not send an empty discussion', () => {
    signInAs();

    renderWithProviders(<NewDiscussion />);

    expect(screen.getByRole('button', { name: 'Post discussion' })).toBeDisabled();
  });

  it('repeats what the API said was wrong with a field', async () => {
    signInAs();
    vi.spyOn(PostService, 'create').mockRejectedValue(
      new HttpError(400, 'Bad request', { Title: ['A title is required.'] }),
    );

    renderWithProviders(<NewDiscussion />);

    await userEvent.type(screen.getByLabelText('Title'), 'x');
    await userEvent.type(screen.getByLabelText('Body'), 'y');
    await userEvent.click(screen.getByRole('button', { name: 'Post discussion' }));

    expect(await screen.findByText('A title is required.')).toBeInTheDocument();
  });
});
