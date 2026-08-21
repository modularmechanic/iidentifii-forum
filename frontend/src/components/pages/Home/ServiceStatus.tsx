import Spinner from '@src/components/common/ui/sm/Spinner';
import ErrorMessage from '@src/components/common/ui/sm/ErrorMessage';
import type { IHealth } from '@src/domains/system/Health';

/***** Types *****/

interface IProps {
  health?: IHealth;
  isLoading: boolean;
  errorMessage?: string;
  onRetry: () => void;
}

/***** Components *****/

/** Default component: reports whether the API answered, without knowing how it was fetched. */
function ServiceStatus(props: IProps) {
  const { health, isLoading, errorMessage, onRetry } = props;

  if (isLoading) {
    return <Spinner label="Checking the API" />;
  }

  if (errorMessage !== undefined) {
    return <ErrorMessage message={errorMessage} onRetry={onRetry} />;
  }

  return (
    <p className="text-sm text-muted">
      API: <span className="font-medium text-success">{health?.status}</span>
      <span className="mx-2 text-line">|</span>
      <span className="font-mono">{health?.environment}</span>
    </p>
  );
}

/***** Export default *****/

export default ServiceStatus;
