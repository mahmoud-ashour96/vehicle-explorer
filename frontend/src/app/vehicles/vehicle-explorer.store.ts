import { httpResource } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Make, VehicleModel, VehicleType } from './vehicle.models';

/**
 * State for the explorer page. Each piece of server data is an httpResource whose request is
 * derived from the selection signals: changing a selection re-fetches automatically and cancels
 * the request that was in flight for the previous one. Provided per page, not in root.
 */
@Injectable()
export class VehicleExplorerStore {
  private readonly selectedMake = signal<Make | undefined>(undefined);
  private readonly selectedYear = signal<number | undefined>(undefined);
  /** undefined means "all vehicle types". */
  private readonly selectedVehicleType = signal<string | undefined>(undefined);

  readonly make = this.selectedMake.asReadonly();
  readonly year = this.selectedYear.asReadonly();
  readonly vehicleType = this.selectedVehicleType.asReadonly();

  readonly makes = httpResource<Make[]>(() => '/api/makes', { defaultValue: [] });

  readonly modelYears = httpResource<number[]>(() => '/api/model-years', { defaultValue: [] });

  readonly vehicleTypes = httpResource<VehicleType[]>(
    () => {
      const make = this.selectedMake();
      return make ? `/api/makes/${make.id}/vehicle-types` : undefined;
    },
    { defaultValue: [] },
  );

  readonly models = httpResource<VehicleModel[]>(
    () => {
      const make = this.selectedMake();
      const year = this.selectedYear();
      if (!make || !year) {
        return undefined;
      }

      const params: Record<string, string | number> = { year };
      const vehicleType = this.selectedVehicleType();
      if (vehicleType) {
        params['vehicleType'] = vehicleType;
      }
      return { url: `/api/makes/${make.id}/models`, params };
    },
    { defaultValue: [] },
  );

  // A resource's value() throws while it is in the error state, so the template reads these instead.
  readonly makeList = computed(() => (this.makes.hasValue() ? this.makes.value() : []));
  readonly yearList = computed(() => (this.modelYears.hasValue() ? this.modelYears.value() : []));

  readonly canShowModels = computed(() => !!this.selectedMake() && !!this.selectedYear());

  selectMake(make: Make | undefined): void {
    this.selectedMake.set(make);
    // Vehicle types differ per make, so a type picked for the previous make no longer applies.
    this.selectedVehicleType.set(undefined);
  }

  selectYear(year: number | undefined): void {
    this.selectedYear.set(year);
  }

  selectVehicleType(vehicleType: string | undefined): void {
    this.selectedVehicleType.set(vehicleType);
  }
}
