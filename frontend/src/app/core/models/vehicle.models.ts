// Match the DTOs returned by the API (backend/src/VehicleExplorer.Application/Dtos).

export interface Make {
  id: number;
  name: string;
}

export interface VehicleType {
  id: number;
  name: string;
}

export interface VehicleModel {
  id: number;
  name: string;
}
