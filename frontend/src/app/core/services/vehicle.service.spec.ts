import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { VehicleService } from './vehicle.service';

describe('VehicleService', () => {
  let service: VehicleService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(VehicleService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets the makes', () => {
    service.getMakes().subscribe((makes) => expect(makes).toEqual([{ id: 474, name: 'HONDA' }]));

    http.expectOne('/api/makes').flush([{ id: 474, name: 'HONDA' }]);
  });

  it('gets the vehicle types for a make', () => {
    service.getVehicleTypes(474).subscribe();

    http.expectOne('/api/makes/474/vehicle-types').flush([]);
  });

  it('sends only the year when no vehicle type is chosen', () => {
    service.getModels(474, 2015).subscribe();

    const request = http.expectOne((r) => r.url === '/api/makes/474/models');
    expect(request.request.params.get('year')).toBe('2015');
    expect(request.request.params.has('vehicleType')).toBe(false);
    request.flush([]);
  });

  it('sends the vehicle type when one is chosen', () => {
    service.getModels(474, 2015, 'Passenger Car').subscribe();

    const request = http.expectOne((r) => r.url === '/api/makes/474/models');
    expect(request.request.params.get('vehicleType')).toBe('Passenger Car');
    request.flush([]);
  });
});
