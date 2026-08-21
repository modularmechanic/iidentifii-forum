import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { buildFlag, buildPost } from '@src/test/factories';
import { renderWithProviders } from '@src/test/render';
import PostsList from './PostsList';

describe('PostsList', () => {
  it('links each discussion to its own page', () => {
    renderWithProviders(<PostsList posts={[buildPost({ title: 'Webhook retries' })]} />);

    const link = screen.getByRole('link', { name: 'Webhook retries' });
    expect(link).toHaveAttribute('href', '/discussions/01a02518-cbf8-7b23-8b12-613e5bf64a39');
  });

  it('shows the author, like count and reply count', () => {
    renderWithProviders(<PostsList posts={[buildPost({ likeCount: 14, commentCount: 7 })]} />);

    expect(screen.getByText('bob')).toBeInTheDocument();
    expect(screen.getByText('14')).toBeInTheDocument();
    expect(screen.getByText(/7 replies/)).toBeInTheDocument();
  });

  it('uses the singular for a single reply', () => {
    renderWithProviders(<PostsList posts={[buildPost({ commentCount: 1 })]} />);

    expect(screen.getByText('1 reply')).toBeInTheDocument();
  });

  it('says so plainly when a discussion has no replies', () => {
    renderWithProviders(<PostsList posts={[buildPost({ commentCount: 0 })]} />);

    expect(screen.getByText('No replies yet')).toBeInTheDocument();
  });

  it('marks a flagged discussion in the list', () => {
    renderWithProviders(<PostsList posts={[buildPost({ tags: [buildFlag()] })]} />);

    expect(screen.getByText('Misleading or false')).toBeInTheDocument();
  });

  it('shows an empty state rather than a bare list', () => {
    renderWithProviders(<PostsList posts={[]} />);

    expect(screen.getByText('No discussions match these filters')).toBeInTheDocument();
    expect(screen.queryByRole('listitem')).not.toBeInTheDocument();
  });
});
