import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Make, VehicleModel, VehicleType } from '../models/vehicle.models';

/** Calls the Vehicle Explorer API. */
@Injectable({ providedIn: 'root' })
export class VehicleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  getMakes(): Observable<Make[]> {
    return this.http.get<Make[]>(`${this.baseUrl}/makes`);
  }

  getModelYears(): Observable<number[]> {
    return this.http.get<number[]>(`${this.baseUrl}/model-years`);
  }

  getVehicleTypes(makeId: number): Observable<VehicleType[]> {
    return this.http.get<VehicleType[]>(`${this.baseUrl}/makes/${makeId}/vehicle-types`);
  }

  getModels(makeId: number, year: number, vehicleType?: string): Observable<VehicleModel[]> {
    let params = new HttpParams().set('year', year);
    if (vehicleType) {
      params = params.set('vehicleType', vehicleType);
    }

    return this.http.get<VehicleModel[]>(`${this.baseUrl}/makes/${makeId}/models`, { params });
  }
}
