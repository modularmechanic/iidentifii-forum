/**
 * The single place the browser talks to the API. Same-origin only: the dev server and the
 * production nginx both proxy /api, so no cross-origin configuration is needed.
 */

/***** Constants *****/

const API_BASE = '/api/v1';

/***** Types *****/

interface IProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

/***** Classes *****/

/** An unsuccessful response, carrying whatever the API explained about it. */
export class HttpError extends Error {
  public readonly status: number;
  public readonly fieldErrors: Record<string, string[]>;

  public constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'HttpError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

/***** Functions *****/

/** Performs a request against the versioned API and returns the parsed body. */
export async function fetchJson<T>(path: string, init?: RequestInit): Promise<T> {
  return await fetchRootJson<T>(`${API_BASE}${path}`, init);
}

/** Performs a request against a path outside the versioned API, such as the health probe. */
export async function fetchRootJson<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }

  const response = await fetch(path, { ...init, headers });

  if (!response.ok) {
    throw await _toHttpError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/** Reads the problem details a failed response carries, falling back to the status text. */
async function _toHttpError(response: Response): Promise<HttpError> {
  try {
    const problem = (await response.json()) as IProblemDetails;
    return new HttpError(
      response.status,
      problem.detail ?? problem.title ?? response.statusText,
      problem.errors ?? {},
    );
  } catch {
    return new HttpError(response.status, response.statusText);
  }
}
