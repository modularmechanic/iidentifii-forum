/***** Types *****/

interface IProps {
  label?: string;
}

/***** Components *****/

/** Default component: an inline activity indicator with an accessible label. */
function Spinner(props: IProps) {
  const { label = 'Loading' } = props;

  return (
    <span className="inline-flex items-center gap-2 text-muted text-sm" role="status">
      <span
        aria-hidden="true"
        className="size-4 animate-spin rounded-full border-2 border-line border-t-accent motion-reduce:animate-none"
      />
      {label}
    </span>
  );
}

/***** Export default *****/

export default Spinner;
