import { useState } from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import CodeInput from './CodeInput';

/***** Components *****/

/** Holds the value, the way the page that uses it does. */
function Harness() {
  const [code, setCode] = useState('');
  return (
    <>
      <CodeInput onChange={setCode} value={code} />
      <output>{code}</output>
    </>
  );
}

/***** Tests *****/

describe('CodeInput', () => {
  it('offers one box per digit', () => {
    render(<Harness />);

    expect(screen.getAllByRole('textbox')).toHaveLength(6);
  });

  it('collects the digits as they are typed', async () => {
    render(<Harness />);
    const boxes = screen.getAllByRole('textbox');

    for (const [index, digit] of [...'482'].entries()) {
      await userEvent.type(boxes[index], digit);
    }

    expect(screen.getByRole('status')).toHaveTextContent('482');
  });

  /** Most people paste the code out of the email rather than typing it. */
  it('fills every box from a pasted code', async () => {
    render(<Harness />);
    const boxes = screen.getAllByRole('textbox');

    boxes[0].focus();
    await userEvent.paste('948480');

    expect(screen.getByRole('status')).toHaveTextContent('948480');
  });

  it('ignores anything that is not a digit', async () => {
    render(<Harness />);

    await userEvent.type(screen.getAllByRole('textbox')[0], 'x');

    expect(screen.getByRole('status')).toBeEmptyDOMElement();
  });
});
