import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { buildFlag } from '@src/test/factories';
import FlagBanner from './FlagBanner';

describe('FlagBanner', () => {
  it('names the moderator and the reason', () => {
    render(<FlagBanner tags={[buildFlag('mod')]} />);

    expect(screen.getByRole('note')).toHaveTextContent('Flagged as misleading or false');
    expect(screen.getByRole('note')).toHaveTextContent('Flagged by mod');
  });

  it('renders nothing when the discussion is not flagged', () => {
    const { container } = render(<FlagBanner tags={[]} />);

    expect(container).toBeEmptyDOMElement();
  });
});
