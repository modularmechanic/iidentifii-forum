/***** Types *****/

interface IProps {
  count: number;
  isLiked: boolean;
  /** Why the control cannot be used, or undefined when it can. */
  disabledReason?: string;
  isPending: boolean;
  onToggle: () => void;
}

/***** Components *****/

/**
 * Default component: the like count, and the control that changes it. Reading is open to
 * everyone, so an anonymous visitor still sees the number; only the action needs an account.
 */
function LikeButton(props: IProps) {
  const { count, isLiked, disabledReason, isPending, onToggle } = props;

  const isDisabled = disabledReason !== undefined || isPending;
  const action = isLiked ? 'Remove your like' : 'Like this discussion';

  return (
    <button
      // The label states the whole control, because it replaces the text inside rather than
      // adding to it: without the count and the reason here, neither is ever announced.
      aria-label={`${_asSentence(disabledReason ?? action)} ${count} ${count === 1 ? 'like' : 'likes'}`}
      aria-pressed={isLiked}
      className={`inline-flex w-12 shrink-0 flex-col items-center gap-0.5 self-center rounded-sm py-1 ${
        isLiked ? 'text-accent' : 'text-muted'
      } ${isDisabled ? 'cursor-not-allowed opacity-60' : 'hover:bg-subtle hover:text-accent'}`}
      disabled={isDisabled}
      onClick={onToggle}
      title={disabledReason ?? action}
      type="button"
    >
      <svg
        aria-hidden="true"
        className="size-4"
        fill={isLiked ? 'currentColor' : 'none'}
        stroke="currentColor"
        strokeWidth="1.8"
        viewBox="0 0 24 24"
      >
        <path d="M12 21s-7-4.6-9.5-9A5.5 5.5 0 0 1 12 6a5.5 5.5 0 0 1 9.5 6c-2.5 4.4-9.5 9-9.5 9z" />
      </svg>
      <span className="font-mono text-sm tabular-nums">{count}</span>
    </button>
  );
}

/***** Functions *****/

/** Ends the phrase with a single full stop, whether or not it arrived with one. */
function _asSentence(phrase: string): string {
  return phrase.endsWith('.') ? phrase : `${phrase}.`;
}

/***** Export default *****/

export default LikeButton;
