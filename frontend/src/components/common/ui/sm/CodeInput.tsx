import { useRef } from 'react';
import { CODE_LENGTH, emptyCode } from '@src/common/utils/code-digits';

/***** Types *****/

interface IProps {
  /** One entry per box. An empty entry is an empty string, never a space. */
  digits: string[];
  onChange: (digits: string[]) => void;
}

/***** Components *****/

/**
 * Default component: six digits, one box each. Typing moves forward, backspace moves back, and a
 * pasted code fills every box at once, because most people paste it out of the email.
 *
 * The boxes are kept as six separate values rather than one string. Padding a string to six
 * characters made an emptied middle box indistinguishable from a full code by length alone, and
 * the code that was then submitted carried a space where a digit belonged.
 */
function CodeInput(props: IProps) {
  const { digits, onChange } = props;

  const boxes = useRef<(HTMLInputElement | null)[]>([]);

  const write = (index: number, written: string) => {
    const next = [...digits];

    for (let offset = 0; offset < written.length && index + offset < CODE_LENGTH; offset++) {
      next[index + offset] = written[offset];
    }

    onChange(next);
  };

  return (
    <div className="flex justify-center gap-2" role="group" aria-label="Six-digit code">
      {Array.from({ length: CODE_LENGTH }, (_unused, index) => (
        <input
          aria-label={`Digit ${index + 1}`}
          autoComplete={index === 0 ? 'one-time-code' : 'off'}
          className="size-11 rounded-sm border border-line bg-surface text-center font-mono text-lg"
          inputMode="numeric"
          key={index}
          maxLength={1}
          onChange={(event) => {
            const written = event.target.value.replace(/\D/g, '');

            if (written === '') {
              return;
            }

            write(index, written);
            _focus(boxes.current, Math.min(index + written.length, CODE_LENGTH - 1));
          }}
          onKeyDown={(event) => {
            if (event.key === 'Backspace') {
              event.preventDefault();

              const next = [...digits];
              next[index] = '';
              onChange(next);

              _focus(boxes.current, Math.max(index - 1, 0));
            }

            if (event.key === 'ArrowLeft') {
              event.preventDefault();
              _focus(boxes.current, Math.max(index - 1, 0));
            }

            if (event.key === 'ArrowRight') {
              event.preventDefault();
              _focus(boxes.current, Math.min(index + 1, CODE_LENGTH - 1));
            }
          }}
          onPaste={(event) => {
            event.preventDefault();

            const pasted = event.clipboardData
              .getData('text')
              .replace(/\D/g, '')
              .slice(0, CODE_LENGTH);
            const next = emptyCode();

            for (let offset = 0; offset < pasted.length; offset++) {
              next[offset] = pasted[offset];
            }

            onChange(next);
            _focus(boxes.current, Math.min(pasted.length, CODE_LENGTH - 1));
          }}
          ref={(element) => {
            boxes.current[index] = element;
          }}
          type="text"
          value={digits[index] ?? ''}
        />
      ))}
    </div>
  );
}

/***** Functions *****/

function _focus(boxes: (HTMLInputElement | null)[], index: number): void {
  boxes[index]?.focus();
}

/***** Export default *****/

export default CodeInput;
