import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { VehicleExplorer } from './vehicle-explorer';

describe('VehicleExplorer', () => {
  let fixture: ComponentFixture<VehicleExplorer>;
  let component: VehicleExplorer;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VehicleExplorer],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(VehicleExplorer);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);

    fixture.detectChanges(); // runs ngOnInit
    http.expectOne('/api/makes').flush([{ id: 474, name: 'HONDA' }]);
    http.expectOne('/api/model-years').flush([2016, 2015]);
  });

  afterEach(() => http.verify());

  it('loads makes and years on start', () => {
    expect(component.makes()).toEqual([{ id: 474, name: 'HONDA' }]);
    expect(component.years()).toEqual([2016, 2015]);
  });

  it('finds the make by name (any case) and loads its vehicle types', () => {
    component.onMakeChange('honda');

    expect(component.selectedMake()).toEqual({ id: 474, name: 'HONDA' });
    http.expectOne('/api/makes/474/vehicle-types').flush([{ id: 2, name: 'Passenger Car' }]);
    expect(component.vehicleTypes()).toEqual([{ id: 2, name: 'Passenger Car' }]);
  });

  it('loads and shows models once a make and a year are chosen', async () => {
    component.onMakeChange('HONDA');
    http.expectOne('/api/makes/474/vehicle-types').flush([]);

    component.onYearChange('2015');
    const request = http.expectOne((r) => r.url === '/api/makes/474/models');
    expect(request.request.params.get('year')).toBe('2015');
    request.flush([
      { id: 1861, name: 'Accord' },
      { id: 1863, name: 'Civic' },
    ]);

    fixture.detectChanges();
    await fixture.whenStable();
    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('.results li');
    expect(items.length).toBe(2);
  });

  it('shows the API error message when loading models fails', () => {
    component.onMakeChange('HONDA');
    http.expectOne('/api/makes/474/vehicle-types').flush([]);

    component.onYearChange('2015');
    http
      .expectOne((r) => r.url === '/api/makes/474/models')
      .flush({ detail: 'vPIC is down.' }, { status: 502, statusText: 'Bad Gateway' });

    expect(component.error()).toBe('vPIC is down.');
    expect(component.loading()).toBe(false);
  });
});
