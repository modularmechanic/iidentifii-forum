/***** Types *****/

interface IProps {
  count: number;
}

/***** Components *****/

/**
 * Default component: how many people liked a discussion. Reading is open to everyone, so this
 * shows the number without offering an action; the control arrives with signing in.
 */
function LikeCount(props: IProps) {
  const { count } = props;

  return (
    <span
      className="inline-flex w-12 shrink-0 flex-col items-center gap-0.5 text-muted"
      title="Log in to like"
    >
      <svg
        aria-hidden="true"
        className="size-4"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.8"
        viewBox="0 0 24 24"
      >
        <path d="M12 21s-7-4.6-9.5-9A5.5 5.5 0 0 1 12 6a5.5 5.5 0 0 1 9.5 6c-2.5 4.4-9.5 9-9.5 9z" />
      </svg>
      <span className="font-mono text-sm tabular-nums">{count}</span>
      <span className="sr-only">likes</span>
    </span>
  );
}

/***** Export default *****/

export default LikeCount;
