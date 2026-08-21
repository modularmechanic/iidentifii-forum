import { useQuery } from '@tanstack/react-query';
import HealthService from '@src/domains/system/HealthService';
import ServiceStatus from './ServiceStatus';

/***** Components *****/

/** Default component: fetches service status and hands it to the presenter. */
function HomeContainer() {
  const query = useQuery({ queryKey: ['health'], queryFn: HealthService.fetchHealth });

  return (
    <ServiceStatus
      errorMessage={query.isError ? 'Could not reach the API. Is it running?' : undefined}
      health={query.data}
      isLoading={query.isPending}
      onRetry={() => void query.refetch()}
    />
  );
}

/***** Export default *****/

export default HomeContainer;
