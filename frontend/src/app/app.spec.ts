import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  it('renders the header and the explorer page', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);

    // Pending requests keep the app "unstable", so flush them before awaiting whenStable().
    fixture.detectChanges();
    http.expectOne('/api/makes').flush([]);
    http.expectOne('/api/model-years').flush([2026]);
    await fixture.whenStable();

    const page = fixture.nativeElement as HTMLElement;
    expect(page.querySelector('.brand-name')?.textContent).toContain('Vehicle Explorer');
    expect(page.querySelector('app-vehicle-explorer')).not.toBeNull();
    http.verify();
  });
});
