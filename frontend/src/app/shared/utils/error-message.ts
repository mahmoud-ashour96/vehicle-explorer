import { HttpErrorResponse } from '@angular/common/http';

/** Turns a failed request into a message for the user. Uses the API's ProblemDetails `detail` when present. */
export function getErrorMessage(error: unknown): string {
  const httpError = findHttpError(error);

  if (httpError?.status === 0) {
    return "Can't reach the server. Check your connection and try again.";
  }

  const detail = (httpError?.error as { detail?: unknown } | null | undefined)?.detail;
  return typeof detail === 'string' ? detail : 'Something went wrong while loading vehicle data.';
}

// Angular resources can wrap the original error, so also look at `cause`.
function findHttpError(error: unknown): HttpErrorResponse | undefined {
  if (error instanceof HttpErrorResponse) {
    return error;
  }

  const cause = (error as { cause?: unknown } | undefined)?.cause;
  return cause instanceof HttpErrorResponse ? cause : undefined;
}
