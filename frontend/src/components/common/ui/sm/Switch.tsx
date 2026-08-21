/***** Types *****/

interface IProps {
  label: string;
  isOn: boolean;
  onChange: (isOn: boolean) => void;
}

/***** Components *****/

/** Default component: an on/off control for something that is either applied or not. */
function Switch(props: IProps) {
  const { label, isOn, onChange } = props;

  return (
    <button
      aria-checked={isOn}
      className="flex items-center gap-2 py-2 text-sm text-muted hover:text-ink"
      onClick={() => onChange(!isOn)}
      role="switch"
      type="button"
    >
      {label}
      <span
        aria-hidden="true"
        className={`relative h-4 w-7 rounded-full border transition-colors ${
          isOn ? 'border-accent bg-accent' : 'border-line bg-subtle'
        }`}
      >
        <span
          className={`absolute top-0.5 size-2.5 rounded-full transition-all ${
            isOn ? 'left-3.5 bg-canvas' : 'left-0.5 bg-muted'
          }`}
        />
      </span>
    </button>
  );
}

/***** Export default *****/

export default Switch;
