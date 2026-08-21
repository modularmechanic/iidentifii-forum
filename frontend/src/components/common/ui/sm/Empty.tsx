import type { ReactNode } from 'react';

/***** Types *****/

interface IProps {
  title: string;
  children?: ReactNode;
}

/***** Components *****/

/** Default component: what a list shows when it has nothing to show. */
function Empty(props: IProps) {
  const { title, children } = props;

  return (
    <div className="px-4 py-8 text-center">
      <p className="font-medium">{title}</p>
      {children && <p className="mt-1 text-sm text-muted">{children}</p>}
    </div>
  );
}

/***** Export default *****/

export default Empty;
