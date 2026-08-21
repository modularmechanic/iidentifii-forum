import type { ReactNode } from 'react';

/***** Constants *****/

const TONES = {
  info: 'border-line text-ink',
  error: 'border-danger text-danger',
  success: 'border-success text-success',
} as const;

/***** Types *****/

interface IProps {
  tone: keyof typeof TONES;
  children: ReactNode;
}

/***** Components *****/

/** Default component: says something about the page as a whole, rather than about one field. */
function Banner(props: IProps) {
  const { tone, children } = props;

  return (
    <p
      className={`rounded-sm border bg-surface px-3 py-2 text-sm ${TONES[tone]}`}
      role={tone === 'error' ? 'alert' : 'status'}
    >
      {children}
    </p>
  );
}

/***** Export default *****/

export default Banner;
