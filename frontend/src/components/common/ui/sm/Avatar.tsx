/***** Types *****/

interface IProps {
  username: string;
  size?: 'sm' | 'md';
}

/***** Components *****/

/** Default component: a member's initial, standing in for a picture the forum does not store. */
function Avatar(props: IProps) {
  const { username, size = 'sm' } = props;

  const dimensions = size === 'md' ? 'size-9 text-sm' : 'size-6 text-xs';

  return (
    <span
      aria-hidden="true"
      className={`inline-grid shrink-0 place-items-center rounded-sm border border-line bg-subtle font-medium ${dimensions}`}
    >
      {username.charAt(0).toUpperCase()}
    </span>
  );
}

/***** Export default *****/

export default Avatar;
