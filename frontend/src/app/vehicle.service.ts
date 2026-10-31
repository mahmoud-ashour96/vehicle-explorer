import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Make, VehicleModel, VehicleType } from './vehicle.models';

@Injectable({ providedIn: 'root' })
export class VehicleService {
  private readonly http = inject(HttpClient);

  getMakes(): Observable<Make[]> {
    return this.http.get<Make[]>('/api/makes');
  }

  getModelYears(): Observable<number[]> {
    return this.http.get<number[]>('/api/model-years');
  }

  getVehicleTypes(makeId: number): Observable<VehicleType[]> {
    return this.http.get<VehicleType[]>(`/api/makes/${makeId}/vehicle-types`);
  }

  getModels(makeId: number, year: number, vehicleType: string): Observable<VehicleModel[]> {
    let params = new HttpParams().set('year', year);
    if (vehicleType) {
      params = params.set('vehicleType', vehicleType);
    }

    return this.http.get<VehicleModel[]>(`/api/makes/${makeId}/models`, { params });
  }
}
