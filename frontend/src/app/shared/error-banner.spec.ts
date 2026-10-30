import { HttpErrorResponse } from '@angular/common/http';
import { describeError } from './error-banner';

describe('describeError', () => {
  it("uses the API's ProblemDetails detail", () => {
    const error = new HttpErrorResponse({ status: 502, error: { detail: 'vPIC is down.' } });

    expect(describeError(error)).toBe('vPIC is down.');
  });

  it('finds the HTTP error when it is wrapped as a cause', () => {
    const error = new HttpErrorResponse({ status: 502, error: { detail: 'vPIC is down.' } });

    expect(describeError(new Error('wrapped', { cause: error }))).toBe('vPIC is down.');
  });

  it('explains network failures (status 0)', () => {
    expect(describeError(new HttpErrorResponse({ status: 0 }))).toContain("Can't reach the server");
  });

  it('falls back to a generic message', () => {
    expect(describeError(new Error('boom'))).toBe(
      'Something went wrong while loading vehicle data.',
    );
  });
});
