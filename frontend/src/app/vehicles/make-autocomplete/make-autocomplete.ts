import { Component, computed, input, model, signal } from '@angular/core';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Make } from '../vehicle.models';
import { searchMakes } from './make-search';

@Component({
  selector: 'app-make-autocomplete',
  imports: [
    MatAutocompleteModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  templateUrl: './make-autocomplete.html',
  styleUrl: './make-autocomplete.scss',
})
export class MakeAutocomplete {
  readonly makes = input.required<readonly Make[]>();
  readonly loading = input(false);
  readonly selected = model<Make | undefined>();

  protected readonly query = signal('');
  protected readonly search = computed(() => searchMakes(this.makes(), this.query()));
  protected readonly hiddenCount = computed(
    () => this.search().total - this.search().matches.length,
  );

  protected readonly displayMake = (make: Make | string | null): string =>
    typeof make === 'string' ? make : (make?.name ?? '');

  protected onInput(event: Event): void {
    const text = (event.target as HTMLInputElement).value;
    this.query.set(text);

    // Editing the text after picking a make un-picks it, so the page never shows stale results.
    if (this.selected() && text !== this.selected()?.name) {
      this.selected.set(undefined);
    }
  }

  protected onOptionSelected(make: Make): void {
    this.query.set(make.name);
    this.selected.set(make);
  }

  protected clear(): void {
    this.query.set('');
    this.selected.set(undefined);
  }
}
