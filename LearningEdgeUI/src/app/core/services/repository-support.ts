import { ApiFailure } from '../http/api-result';

/** Enough latency to make loading skeletons visible in dev. */
export const MOCK_LATENCY_MS = 350;

export function notImplementedUpstream(operation: string): ApiFailure {
  return new ApiFailure(
    'client',
    `${operation} has no backend endpoint yet. Set useMockApi: true to use the in-memory adapter.`,
  );
}
