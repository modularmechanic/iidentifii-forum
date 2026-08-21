import type { ReactNode } from 'react';

/***** Types *****/

interface IProps {
  title: string;
  description?: string;
  children: ReactNode;
}

/***** Components *****/

/** Default component: the frame every account page shares, so they read as one journey. */
function AuthCard(props: IProps) {
  const { title, description, children } = props;

  return (
    <section className="mx-auto flex max-w-sm flex-col gap-4 py-8">
      <div className="text-center">
        <h1 className="text-lg font-semibold">{title}</h1>
        {description && <p className="mt-1 text-sm text-muted">{description}</p>}
      </div>
      {children}
    </section>
  );
}

/***** Export default *****/

export default AuthCard;
