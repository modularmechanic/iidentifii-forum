/** Liveness payload returned by the API. */
export interface IHealth {
  status: string;
  environment: string;
  checkedAt: string;
}
