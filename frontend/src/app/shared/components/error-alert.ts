import { Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { getErrorMessage } from '../utils/error-message';

@Component({
  selector: 'app-error-alert',
  imports: [MatButtonModule, MatIconModule],
  template: `
    <div class="alert" role="alert">
      <mat-icon aria-hidden="true">error</mat-icon>
      <span class="message">{{ message() }}</span>
      <button mat-button type="button" (click)="retry.emit()">Try again</button>
    </div>
  `,
  styles: `
    .alert {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 6px 6px 6px 16px;
      border-radius: 12px;
      background: var(--mat-sys-error-container);
      color: var(--mat-sys-on-error-container);
    }

    .message {
      flex: 1;
    }
  `,
})
export class ErrorAlert {
  readonly error = input.required<unknown>();
  readonly retry = output<void>();

  protected readonly message = computed(() => getErrorMessage(this.error()));
}
