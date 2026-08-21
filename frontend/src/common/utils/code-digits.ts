/***** Constants *****/

/** How many boxes a sign-in code is spread across. */
export const CODE_LENGTH = 6;

/***** Functions *****/

/** Six empty boxes. An empty box is an empty string, never a space. */
export function emptyCode(): string[] {
  return Array.from({ length: CODE_LENGTH }, () => '');
}

/**
 * The code the boxes spell, or null while any box is still empty. Callers get something they can
 * send or nothing at all, rather than a string they have to inspect for gaps: padding the boxes
 * into one string made an emptied middle box look complete by length alone.
 */
export function readCode(digits: string[]): string | null {
  const code = digits.join('');

  return digits.length === CODE_LENGTH && /^\d{6}$/.test(code) ? code : null;
}
