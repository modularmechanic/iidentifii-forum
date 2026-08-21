import { useRef } from 'react';

/***** Constants *****/

const LENGTH = 6;

/***** Types *****/

interface IProps {
  value: string;
  onChange: (value: string) => void;
}

/***** Components *****/

/**
 * Default component: six digits, one box each. Typing moves forward, backspace moves back, and a
 * pasted code fills every box at once, because most people paste it out of the email.
 */
function CodeInput(props: IProps) {
  const { value, onChange } = props;

  const boxes = useRef<(HTMLInputElement | null)[]>([]);

  return (
    <div className="flex justify-center gap-2" role="group" aria-label="Six-digit code">
      {Array.from({ length: LENGTH }, (_unused, index) => (
        <input
          aria-label={`Digit ${index + 1}`}
          autoComplete={index === 0 ? 'one-time-code' : 'off'}
          className="size-11 rounded-sm border border-line bg-surface text-center font-mono text-lg"
          inputMode="numeric"
          key={index}
          maxLength={1}
          onChange={(event) => {
            const digits = event.target.value.replace(/\D/g, '');

            if (digits === '') {
              return;
            }

            const next = _replaceFrom(value, index, digits);
            onChange(next);
            _focus(boxes.current, Math.min(index + digits.length, LENGTH - 1));
          }}
          onKeyDown={(event) => {
            if (event.key === 'Backspace') {
              event.preventDefault();
              onChange(_replaceAt(value, index, ''));
              _focus(boxes.current, Math.max(index - 1, 0));
            }
          }}
          onPaste={(event) => {
            event.preventDefault();
            const pasted = event.clipboardData.getData('text').replace(/\D/g, '').slice(0, LENGTH);
            onChange(pasted);
            _focus(boxes.current, Math.min(pasted.length, LENGTH - 1));
          }}
          ref={(element) => {
            boxes.current[index] = element;
          }}
          type="text"
          value={value[index] ?? ''}
        />
      ))}
    </div>
  );
}

/***** Functions *****/

function _replaceAt(value: string, index: number, digit: string): string {
  const characters = value.padEnd(LENGTH, ' ').split('');
  characters[index] = digit === '' ? ' ' : digit;
  return characters.join('').trimEnd();
}

/** Writing several digits at once, which happens when a box already holding one is typed into. */
function _replaceFrom(value: string, index: number, digits: string): string {
  let next = value;
  for (let offset = 0; offset < digits.length && index + offset < LENGTH; offset++) {
    next = _replaceAt(next, index + offset, digits[offset]);
  }
  return next;
}

function _focus(boxes: (HTMLInputElement | null)[], index: number): void {
  boxes[index]?.focus();
}

/***** Export default *****/

export default CodeInput;
