import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { VehicleExplorer } from './vehicle-explorer';

describe('VehicleExplorer', () => {
  let fixture: ComponentFixture<VehicleExplorer>;
  let http: HttpTestingController;

  const page = () => fixture.nativeElement as HTMLElement;

  // Pending requests keep the app "unstable", so always flush them before awaiting whenStable().
  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function clickButton(text: string): void {
    const button = Array.from(page().querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === text,
    );
    button!.click();
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VehicleExplorer],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(VehicleExplorer);
    http = TestBed.inject(HttpTestingController);

    fixture.detectChanges();
    http.expectOne('/api/makes').flush([
      { id: 474, name: 'HONDA' },
      { id: 448, name: 'TOYOTA' },
    ]);
    http.expectOne('/api/model-years').flush([2016, 2015]);
    await render();
  });

  afterEach(() => http.verify());

  it('suggests popular makes before anything is chosen', () => {
    expect(page().textContent).toContain('Find a vehicle');
    expect(page().querySelector('.quick-picks')?.textContent).toContain('TOYOTA');
  });

  it('loads vehicle types for the chosen make and asks for a year', async () => {
    clickButton('HONDA');
    http.expectOne('/api/makes/474/vehicle-types').flush([{ id: 2, name: 'Passenger Car' }]);
    await render();

    expect(page().textContent).toContain('Now pick a model year');
    expect(page().querySelector('mat-chip-listbox')?.textContent).toContain('Passenger Car');
  });

  it('shows the models for the chosen make and year, and filters by vehicle type', async () => {
    clickButton('HONDA');
    http.expectOne('/api/makes/474/vehicle-types').flush([{ id: 2, name: 'Passenger Car' }]);
    await render();

    // Picking from mat-select through the DOM needs an overlay, so set the year directly.
    fixture.componentInstance['selectedYear'].set(2015);
    fixture.detectChanges();
    http
      .expectOne((r) => r.url === '/api/makes/474/models')
      .flush([
        { id: 1861, name: 'Accord' },
        { id: 1863, name: 'Civic' },
      ]);
    await render();

    expect(page().querySelectorAll('.model-name').length).toBe(2);
    expect(page().querySelector('.count')?.textContent).toContain('2 models');

    (page().querySelector('mat-chip-option:nth-of-type(2) button') as HTMLElement).click();
    fixture.detectChanges();
    const filtered = http.expectOne((r) => r.url === '/api/makes/474/models');
    expect(filtered.request.params.get('vehicleType')).toBe('Passenger Car');
    filtered.flush([{ id: 1861, name: 'Accord' }]);
    await render();

    expect(page().querySelector('.count')?.textContent).toContain('1 model');
  });

  it('shows an error with a retry button when models fail to load', async () => {
    clickButton('HONDA');
    http.expectOne('/api/makes/474/vehicle-types').flush([]);
    await render();

    fixture.componentInstance['selectedYear'].set(2015);
    fixture.detectChanges();
    http
      .expectOne((r) => r.url === '/api/makes/474/models')
      .flush({ detail: 'vPIC is down.' }, { status: 502, statusText: 'Bad Gateway' });
    await render();

    expect(page().querySelector('[role="alert"]')?.textContent).toContain('vPIC is down.');
  });
});
