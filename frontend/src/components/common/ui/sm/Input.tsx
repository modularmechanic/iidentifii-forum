import type { InputHTMLAttributes } from 'react';

/***** Types *****/

interface IProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  hint?: string;
}

/***** Components *****/

/** Default component: a labelled field that says what is wrong with it, when something is. */
function Input(props: IProps) {
  const { label, error, hint, id, ...otherProps } = props;

  const fieldId = id ?? props.name;
  const describedBy = error ? `${fieldId}-error` : hint ? `${fieldId}-hint` : undefined;

  return (
    <div className="flex flex-col gap-1">
      <label className="text-xs font-medium" htmlFor={fieldId}>
        {label}
      </label>
      <input
        aria-describedby={describedBy}
        aria-invalid={error ? true : undefined}
        className={`rounded-sm border bg-surface px-3 py-2 text-sm ${
          error ? 'border-danger' : 'border-line'
        }`}
        id={fieldId}
        {...otherProps}
      />
      {error && (
        <p className="text-xs text-danger" id={`${fieldId}-error`}>
          {error}
        </p>
      )}
      {!error && hint && (
        <p className="text-xs text-muted" id={`${fieldId}-hint`}>
          {hint}
        </p>
      )}
    </div>
  );
}

/***** Export default *****/

export default Input;
