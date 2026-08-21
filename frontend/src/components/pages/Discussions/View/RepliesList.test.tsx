import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { buildComment } from '@src/test/factories';
import RepliesList from './RepliesList';

describe('RepliesList', () => {
  it('shows each reply with its author', () => {
    render(
      <RepliesList
        replies={[
          buildComment({ id: '1', body: 'At least once.' }),
          buildComment({ id: '2', body: 'Deduplicate on the delivery identifier.' }),
        ]}
      />,
    );

    expect(screen.getAllByRole('listitem')).toHaveLength(2);
    expect(screen.getByText('At least once.')).toBeInTheDocument();
    expect(screen.getAllByText('carol')).toHaveLength(2);
  });

  it('marks a reply that was edited', () => {
    render(<RepliesList replies={[buildComment({ updatedAt: '2026-08-21T09:00:00+00:00' })]} />);

    expect(screen.getByText('edited')).toBeInTheDocument();
  });

  it('invites the first answer when there are none', () => {
    render(<RepliesList replies={[]} />);

    expect(screen.getByText('No replies yet')).toBeInTheDocument();
  });
});
