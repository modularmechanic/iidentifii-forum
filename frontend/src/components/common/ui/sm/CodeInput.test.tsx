import { useState } from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import CodeInput from './CodeInput';
import { emptyCode, readCode } from '@src/common/utils/code-digits';

/***** Components *****/

/** Holds the boxes, the way the page that uses it does. */
function Harness() {
  const [digits, setDigits] = useState(emptyCode);
  const code = readCode(digits);

  return (
    <>
      <CodeInput digits={digits} onChange={setDigits} />
      {/* What the page would send: nothing at all until every box holds a digit. */}
      <output>{code ?? 'incomplete'}</output>
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

    expect(boxes.map((box) => (box as HTMLInputElement).value)).toEqual([
      '4',
      '8',
      '2',
      '',
      '',
      '',
    ]);
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
    const boxes = screen.getAllByRole('textbox');

    await userEvent.type(boxes[0], 'x');

    expect((boxes[0] as HTMLInputElement).value).toBe('');
  });
});

describe('a code with a gap in it', () => {
  it('is not offered as a code when a middle box is cleared', async () => {
    render(<Harness />);

    const boxes = screen.getAllByRole('textbox');
    await userEvent.type(boxes[0], '123456');
    expect(screen.getByRole('status', { hidden: true })).toHaveTextContent('123456');

    await userEvent.click(boxes[2]);
    await userEvent.keyboard('{Backspace}');

    // Padding the boxes into one string used to leave "12 456", which is six characters long and
    // was therefore treated as ready to send.
    expect(screen.getByRole('status', { hidden: true })).toHaveTextContent('incomplete');
  });

  it('is not offered as a code when only the last box is filled', async () => {
    render(<Harness />);

    const boxes = screen.getAllByRole('textbox');
    await userEvent.type(boxes[5], '7');

    expect(screen.getByRole('status', { hidden: true })).toHaveTextContent('incomplete');
  });

  it('is offered once every box holds a digit, whatever order they were filled in', async () => {
    render(<Harness />);

    const boxes = screen.getAllByRole('textbox');
    await userEvent.type(boxes[5], '6');
    await userEvent.type(boxes[0], '1');
    await userEvent.type(boxes[1], '2');
    await userEvent.type(boxes[2], '3');
    await userEvent.type(boxes[3], '4');
    await userEvent.type(boxes[4], '5');

    expect(screen.getByRole('status', { hidden: true })).toHaveTextContent('123456');
  });
});
