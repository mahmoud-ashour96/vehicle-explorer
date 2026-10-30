import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('renders the page heading and the initial empty state', async () => {
    const fixture = TestBed.createComponent(App);
    // Not whenStable(): pending resource requests keep the app unstable until they are flushed.
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/makes').flush([]);
    http.expectOne('/api/model-years').flush([2026]);
    await fixture.whenStable();

    const page = fixture.nativeElement as HTMLElement;
    expect(page.querySelector('h1')?.textContent).toContain('Vehicle Explorer');
    expect(page.textContent).toContain('Pick a make and a model year');
    http.verify();
  });
});
