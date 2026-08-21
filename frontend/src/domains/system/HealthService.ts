import { fetchRootJson } from '@src/infra/http';
import type { IHealth } from './Health';

/** Reads service status. Components reach the API through services, never directly. */
const HealthService = {
  async fetchHealth(): Promise<IHealth> {
    return await fetchRootJson<IHealth>('/health');
  },
} as const;

export default HealthService;
