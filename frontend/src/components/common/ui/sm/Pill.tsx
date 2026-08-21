/***** Types *****/

interface IProps {
  label: string;
}

/***** Components *****/

/** Default component: a moderation flag, outlined so it reads as a warning without shouting. */
function Pill(props: IProps) {
  const { label } = props;

  return (
    <span className="inline-flex items-center rounded-sm border border-danger px-2 py-0.5 text-xs font-medium text-danger">
      {label}
    </span>
  );
}

/***** Export default *****/

export default Pill;
