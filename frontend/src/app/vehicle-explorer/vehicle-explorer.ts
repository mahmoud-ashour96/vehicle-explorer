import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { Make, VehicleModel, VehicleType } from '../vehicle.models';
import { VehicleService } from '../vehicle.service';

@Component({
  selector: 'app-vehicle-explorer',
  templateUrl: './vehicle-explorer.html',
  styleUrl: './vehicle-explorer.scss',
})
export class VehicleExplorer implements OnInit {
  private readonly vehicleService = inject(VehicleService);
  private modelsRequest?: Subscription;

  // Data from the API
  makes = signal<Make[]>([]);
  years = signal<number[]>([]);
  vehicleTypes = signal<VehicleType[]>([]);
  models = signal<VehicleModel[]>([]);

  // What the user picked. An empty vehicle type means "all types".
  selectedMake = signal<Make | null>(null);
  selectedYear = signal<number | null>(null);
  selectedVehicleType = signal('');

  loading = signal(false);
  error = signal('');

  ngOnInit(): void {
    this.loadMakesAndYears();
  }

  loadMakesAndYears(): void {
    this.vehicleService.getMakes().subscribe({
      next: (makes) => this.makes.set(makes),
      error: (err) => this.error.set(this.getErrorMessage(err)),
    });

    this.vehicleService.getModelYears().subscribe({
      next: (years) => this.years.set(years),
      error: (err) => this.error.set(this.getErrorMessage(err)),
    });
  }

  onMakeChange(text: string): void {
    // The make box is a text input with suggestions, so look the make up by its name.
    const name = text.trim().toLowerCase();
    const make = this.makes().find((m) => m.name.toLowerCase() === name) ?? null;

    if (make?.id === this.selectedMake()?.id) {
      return;
    }

    this.selectedMake.set(make);
    this.selectedVehicleType.set('');
    this.vehicleTypes.set([]);

    if (make) {
      this.vehicleService.getVehicleTypes(make.id).subscribe({
        next: (types) => this.vehicleTypes.set(types),
        error: (err) => this.error.set(this.getErrorMessage(err)),
      });
    }

    this.loadModels();
  }

  onYearChange(value: string): void {
    this.selectedYear.set(value ? Number(value) : null);
    this.loadModels();
  }

  onVehicleTypeChange(value: string): void {
    this.selectedVehicleType.set(value);
    this.loadModels();
  }

  loadModels(): void {
    // Cancel the previous request so an old, slow response can't replace a newer one.
    this.modelsRequest?.unsubscribe();
    this.models.set([]);
    this.error.set('');

    const make = this.selectedMake();
    const year = this.selectedYear();
    if (!make || !year) {
      this.loading.set(false);
      return;
    }

    this.loading.set(true);
    this.modelsRequest = this.vehicleService
      .getModels(make.id, year, this.selectedVehicleType())
      .subscribe({
        next: (models) => {
          this.models.set(models);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(this.getErrorMessage(err));
          this.loading.set(false);
        },
      });
  }

  retry(): void {
    if (this.makes().length === 0 || this.years().length === 0) {
      this.loadMakesAndYears();
    }
    this.loadModels();
  }

  private getErrorMessage(err: HttpErrorResponse): string {
    // The API returns ProblemDetails; show its "detail" when there is one.
    return err.error?.detail ?? 'Something went wrong. Please try again.';
  }
}
