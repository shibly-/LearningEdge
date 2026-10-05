import { ApiFailure } from '../http/api-result';

/** Enough latency to make loading skeletons visible in dev. */
export const MOCK_LATENCY_MS = 350;

/**
 * The API exposes only Create and GetById for user and organization. Any list,
 * update, or delete call against the live backend is a programming error, not a
 * runtime condition — fail loudly rather than returning an empty array.
 */
export function noListEndpoint(resource: string): ApiFailure {
  return new ApiFailure(
    'client',
    `No list endpoint exists for ${resource}. Set useMockApi: true, or add the endpoint to the API first.`,
  );
}

export function notImplementedUpstream(operation: string): ApiFailure {
  return new ApiFailure(
    'client',
    `${operation} has no backend endpoint yet. Set useMockApi: true to use the in-memory adapter.`,
  );
}
