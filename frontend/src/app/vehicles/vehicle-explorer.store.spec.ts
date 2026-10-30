import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { VehicleExplorerStore } from './vehicle-explorer.store';

const honda = { id: 474, name: 'HONDA' };
const toyota = { id: 448, name: 'TOYOTA' };

describe('VehicleExplorerStore', () => {
  let store: VehicleExplorerStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [VehicleExplorerStore, provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(VehicleExplorerStore);
    http = TestBed.inject(HttpTestingController);

    TestBed.tick();
    http.expectOne('/api/makes').flush([honda, toyota]);
    http.expectOne('/api/model-years').flush([2016, 2015]);
  });

  afterEach(() => http.verify());

  it('loads vehicle types as soon as a make is picked, but waits for a year before loading models', () => {
    store.selectMake(honda);
    TestBed.tick();

    http.expectOne('/api/makes/474/vehicle-types').flush([]);
    http.expectNone((req) => req.url.endsWith('/models'));
  });

  it('loads models for the selected make and year', async () => {
    store.selectMake(honda);
    store.selectYear(2015);
    TestBed.tick();

    http.expectOne('/api/makes/474/vehicle-types').flush([]);
    const req = http.expectOne((r) => r.url === '/api/makes/474/models');
    expect(req.request.params.get('year')).toBe('2015');
    expect(req.request.params.has('vehicleType')).toBe(false);

    req.flush([{ id: 1861, name: 'Accord' }]);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.models.value()).toEqual([{ id: 1861, name: 'Accord' }]);
  });

  it('sends the vehicle type filter, and clears it when the make changes', () => {
    store.selectMake(honda);
    store.selectYear(2015);
    store.selectVehicleType('Passenger Car');
    TestBed.tick();

    http.expectOne('/api/makes/474/vehicle-types').flush([]);
    const filtered = http.expectOne((r) => r.url === '/api/makes/474/models');
    expect(filtered.request.params.get('vehicleType')).toBe('Passenger Car');
    filtered.flush([]);

    store.selectMake(toyota);
    TestBed.tick();

    expect(store.vehicleType()).toBeUndefined();
    http.expectOne('/api/makes/448/vehicle-types').flush([]);
    const unfiltered = http.expectOne((r) => r.url === '/api/makes/448/models');
    expect(unfiltered.request.params.has('vehicleType')).toBe(false);
    unfiltered.flush([]);
  });

  it('exposes an empty make list instead of throwing when makes fail to load', async () => {
    store.makes.reload();
    TestBed.tick();
    http
      .expectOne('/api/makes')
      .flush({ detail: 'vPIC is down' }, { status: 502, statusText: 'Bad Gateway' });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.makes.error()).toBeDefined();
    expect(store.makeList()).toEqual([]);
  });
});
