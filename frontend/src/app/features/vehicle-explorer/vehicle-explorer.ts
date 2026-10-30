import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Make } from '../../core/models/vehicle.models';
import { VehicleService } from '../../core/services/vehicle.service';
import { ErrorAlert } from '../../shared/components/error-alert';
import { searchMakes } from './make-search';

@Component({
  selector: 'app-vehicle-explorer',
  imports: [
    ErrorAlert,
    MatAutocompleteModule,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './vehicle-explorer.html',
  styleUrl: './vehicle-explorer.scss',
})
export class VehicleExplorer {
  private readonly vehicleService = inject(VehicleService);

  // What the user has picked. An empty vehicle type means "all types".
  protected readonly selectedMake = signal<Make | undefined>(undefined);
  protected readonly selectedYear = signal<number | undefined>(undefined);
  protected readonly selectedVehicleType = signal('');
  protected readonly makeSearchText = signal('');

  // Data from the API. Each resource reloads when the signals it reads change,
  // and cancels the previous request, so a slow old response can't overwrite a newer one.
  protected readonly makes = rxResource({
    stream: () => this.vehicleService.getMakes(),
    defaultValue: [],
  });

  protected readonly modelYears = rxResource({
    stream: () => this.vehicleService.getModelYears(),
    defaultValue: [],
  });

  protected readonly vehicleTypes = rxResource({
    params: () => this.selectedMake()?.id,
    stream: ({ params: makeId }) => this.vehicleService.getVehicleTypes(makeId),
    defaultValue: [],
  });

  protected readonly models = rxResource({
    params: () => {
      const make = this.selectedMake();
      const year = this.selectedYear();
      return make && year
        ? { makeId: make.id, year, vehicleType: this.selectedVehicleType() }
        : undefined;
    },
    stream: ({ params }) =>
      this.vehicleService.getModels(params.makeId, params.year, params.vehicleType),
    defaultValue: [],
  });

  // A resource's value() throws while it is in an error state, so read the lists through these.
  private readonly makeList = computed(() => (this.makes.hasValue() ? this.makes.value() : []));
  protected readonly yearList = computed(() =>
    this.modelYears.hasValue() ? this.modelYears.value() : [],
  );

  protected readonly makeSuggestions = computed(() =>
    searchMakes(this.makeList(), this.makeSearchText()),
  );
  protected readonly popularMakes = computed(() => searchMakes(this.makeList(), '').matches);

  protected readonly displayMake = (make: Make | null): string => make?.name ?? '';

  protected onMakeTyped(text: string): void {
    this.makeSearchText.set(text);

    // Editing the text after choosing a make clears the choice, so old results never stay on screen.
    if (this.selectedMake() && text !== this.selectedMake()?.name) {
      this.selectedMake.set(undefined);
    }
  }

  protected selectMake(make: Make | undefined): void {
    this.selectedMake.set(make);
    this.makeSearchText.set(make?.name ?? '');
    // Vehicle types differ per make, so reset the filter.
    this.selectedVehicleType.set('');
  }
}
