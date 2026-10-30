import { Component, inject } from '@angular/core';
import { MatChipListboxChange, MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { ErrorBanner } from '../../shared/error-banner';
import { MakeAutocomplete } from '../make-autocomplete/make-autocomplete';
import { VehicleExplorerStore } from '../vehicle-explorer.store';

@Component({
  selector: 'app-vehicle-explorer',
  imports: [
    ErrorBanner,
    MakeAutocomplete,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressBarModule,
    MatSelectModule,
  ],
  providers: [VehicleExplorerStore],
  templateUrl: './vehicle-explorer.html',
  styleUrl: './vehicle-explorer.scss',
})
export class VehicleExplorer {
  protected readonly store = inject(VehicleExplorerStore);

  protected onVehicleTypeChange(change: MatChipListboxChange): void {
    // Clicking the active chip deselects it. Treat that as "All types" and re-select that chip.
    if (change.value === undefined) {
      change.source.value = '';
    }
    this.store.selectVehicleType(change.value || undefined);
  }
}
