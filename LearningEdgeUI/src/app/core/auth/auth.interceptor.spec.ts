import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SKIP_API_CONCERNS } from '../http/http-context';
import { OrganizationContextService } from '../services/organization-context.service';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

const ORG_ID = 'dcd9946b-908d-459e-85c2-66b590c50ad2';

function setup(token: string | null, organizationId: string | null) {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      { provide: AuthService, useValue: { accessToken: () => token } },
      {
        provide: OrganizationContextService,
        useValue: { activeOrganizationId: () => organizationId },
      },
    ],
  });
  return {
    http: TestBed.inject(HttpClient),
    controller: TestBed.inject(HttpTestingController),
  };
}

describe('authInterceptor', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('attaches the Keycloak bearer token and the active tenant', () => {
    const { http, controller } = setup('jwt-value', ORG_ID);

    http.get('/api/v1/organization').subscribe();
    const request = controller.expectOne('/api/v1/organization');

    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-value');
    expect(request.request.headers.get('X-Organization-Id')).toBe(ORG_ID);
    request.flush({});
  });

  it('sends no auth headers before anyone has signed in', () => {
    const { http, controller } = setup(null, null);

    http.get('/api/v1/organization').subscribe();
    const request = controller.expectOne('/api/v1/organization');

    expect(request.request.headers.has('Authorization')).toBe(false);
    expect(request.request.headers.has('X-Organization-Id')).toBe(false);
    request.flush({});
  });

  it('leaves requests marked SKIP_API_CONCERNS untouched', () => {
    const { http, controller } = setup('jwt-value', ORG_ID);

    http
      .get('/mock-data/tms-sample-data.json', {
        context: new HttpContext().set(SKIP_API_CONCERNS, true),
      })
      .subscribe();
    const request = controller.expectOne('/mock-data/tms-sample-data.json');

    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });
});
