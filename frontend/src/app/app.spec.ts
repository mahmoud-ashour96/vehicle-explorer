import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let app: App;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    app = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);

    fixture.detectChanges(); // runs ngOnInit
    http.expectOne('/api/makes').flush([{ id: 474, name: 'HONDA' }]);
    http.expectOne('/api/model-years').flush([2016, 2015]);
  });

  afterEach(() => http.verify());

  it('loads makes and years on start', () => {
    expect(app.makes()).toEqual([{ id: 474, name: 'HONDA' }]);
    expect(app.years()).toEqual([2016, 2015]);
  });

  it('finds the make by name (any case) and loads its vehicle types', () => {
    app.onMakeChange('honda');

    expect(app.selectedMake()).toEqual({ id: 474, name: 'HONDA' });
    http.expectOne('/api/makes/474/vehicle-types').flush([{ id: 2, name: 'Passenger Car' }]);
    expect(app.vehicleTypes()).toEqual([{ id: 2, name: 'Passenger Car' }]);
  });

  it('loads and shows models once a make and a year are chosen', async () => {
    app.onMakeChange('HONDA');
    http.expectOne('/api/makes/474/vehicle-types').flush([]);

    app.onYearChange('2015');
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
    app.onMakeChange('HONDA');
    http.expectOne('/api/makes/474/vehicle-types').flush([]);

    app.onYearChange('2015');
    http
      .expectOne((r) => r.url === '/api/makes/474/models')
      .flush({ detail: 'vPIC is down.' }, { status: 502, statusText: 'Bad Gateway' });

    expect(app.error()).toBe('vPIC is down.');
    expect(app.loading()).toBe(false);
  });
});
