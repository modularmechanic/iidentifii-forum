/***** Types *****/

interface IProps {
  message: string;
  onRetry?: () => void;
}

/***** Components *****/

/** Default component: a failure notice with an optional retry. */
function ErrorMessage(props: IProps) {
  const { message, onRetry } = props;

  return (
    <div
      className="flex items-center justify-between gap-3 rounded border border-danger bg-danger-soft px-3 py-2 text-sm text-danger"
      role="alert"
    >
      <span>{message}</span>
      {onRetry && (
        <button
          className="rounded border border-danger px-2 py-1 text-xs font-medium"
          onClick={onRetry}
          type="button"
        >
          Retry
        </button>
      )}
    </div>
  );
}

/***** Export default *****/

export default ErrorMessage;
