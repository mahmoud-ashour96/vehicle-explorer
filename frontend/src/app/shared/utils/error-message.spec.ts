import { HttpErrorResponse } from '@angular/common/http';
import { getErrorMessage } from './error-message';

describe('getErrorMessage', () => {
  it("uses the API's ProblemDetails detail", () => {
    const error = new HttpErrorResponse({ status: 502, error: { detail: 'vPIC is down.' } });

    expect(getErrorMessage(error)).toBe('vPIC is down.');
  });

  it('finds the HTTP error when it is wrapped as a cause', () => {
    const error = new HttpErrorResponse({ status: 502, error: { detail: 'vPIC is down.' } });

    expect(getErrorMessage(new Error('wrapped', { cause: error }))).toBe('vPIC is down.');
  });

  it('explains network failures', () => {
    expect(getErrorMessage(new HttpErrorResponse({ status: 0 }))).toContain(
      "Can't reach the server",
    );
  });

  it('falls back to a generic message', () => {
    expect(getErrorMessage(new Error('boom'))).toBe(
      'Something went wrong while loading vehicle data.',
    );
  });
});
