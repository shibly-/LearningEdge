import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { retryInterceptor } from './retry.interceptor';

const URL = 'https://localhost:7176/api/v1/user/abc';

/** Long enough to clear the largest backoff plus jitter. */
const SETTLE_MS = environment.rateLimitRetry.maxDelayMs + environment.rateLimitRetry.baseDelayMs;

describe('retryInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([retryInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    vi.useRealTimers();
    TestBed.resetTestingModule();
  });

  /**
   * The fixed-window limiter rejects with 503, not 429, because
   * RejectionStatusCode is left at the ASP.NET Core default.
   */
  it('retries a 503 and succeeds on the second attempt', async () => {
    const received: unknown[] = [];
    http.get(URL).subscribe((value) => received.push(value));

    controller.expectOne(URL).flush('busy', { status: 503, statusText: 'Service Unavailable' });

    await vi.advanceTimersByTimeAsync(SETTLE_MS);

    controller.expectOne(URL).flush({ success: true, error: '', data: { id: 'abc' } });
    expect(received).toEqual([{ success: true, error: '', data: { id: 'abc' } }]);
  });

  it('gives up after the configured attempt budget', async () => {
    let failure: unknown = null;
    http.get(URL).subscribe({ error: (error: unknown) => (failure = error) });

    // The original request plus maxAttempts retries, all rejected.
    for (let attempt = 0; attempt <= environment.rateLimitRetry.maxAttempts; attempt++) {
      controller.expectOne(URL).flush('busy', { status: 503, statusText: 'Service Unavailable' });
      await vi.advanceTimersByTimeAsync(SETTLE_MS);
    }

    expect(failure).not.toBeNull();
    controller.verify();
  });

  it('does not retry a 500 — only 503 means rate limited', async () => {
    let failure: unknown = null;
    http.get(URL).subscribe({ error: (error: unknown) => (failure = error) });

    controller.expectOne(URL).flush('boom', { status: 500, statusText: 'Server Error' });
    await vi.advanceTimersByTimeAsync(SETTLE_MS);

    expect(failure).not.toBeNull();
    controller.verify();
  });

  it('leaves mutations alone so a POST is never replayed', async () => {
    let failure: unknown = null;
    http.post(URL, { name: 'x' }).subscribe({ error: (error: unknown) => (failure = error) });

    controller.expectOne(URL).flush('busy', { status: 503, statusText: 'Service Unavailable' });
    await vi.advanceTimersByTimeAsync(SETTLE_MS);

    expect(failure).not.toBeNull();
    controller.verify();
  });
});
