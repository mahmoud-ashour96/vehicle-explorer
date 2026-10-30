import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/** Turns a failed request into a sentence a user can act on. Prefers the API's ProblemDetails `detail`. */
export function describeError(error: unknown): string {
  const http = asHttpError(error);

  if (http?.status === 0) {
    return "Can't reach the server. Check your connection and try again.";
  }

  const detail = (http?.error as { detail?: unknown } | null | undefined)?.detail;
  return typeof detail === 'string' ? detail : 'Something went wrong while loading vehicle data.';
}

// Resources may wrap a failure that isn't an Error subclass, so also look at `cause`.
function asHttpError(error: unknown): HttpErrorResponse | undefined {
  if (error instanceof HttpErrorResponse) {
    return error;
  }
  const cause = (error as { cause?: unknown } | undefined)?.cause;
  return cause instanceof HttpErrorResponse ? cause : undefined;
}

@Component({
  selector: 'app-error-banner',
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="banner" role="alert">
      <mat-icon aria-hidden="true">error</mat-icon>
      <span class="message">{{ message() }}</span>
      <button mat-button type="button" (click)="retry.emit()">Try again</button>
    </div>
  `,
  styles: `
    .banner {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 8px 8px 8px 16px;
      border-radius: var(--mat-sys-corner-medium);
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }
    .message {
      flex: 1;
    }
  `,
})
export class ErrorBanner {
  readonly error = input.required<unknown>();
  readonly retry = output<void>();

  protected readonly message = computed(() => describeError(this.error()));
}
