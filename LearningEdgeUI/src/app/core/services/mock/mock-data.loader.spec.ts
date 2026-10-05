import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MockDataLoader } from './mock-data.loader';
import { MOCK_DATA_URL } from './mock-data.model';
import { MockDb } from './mock-db';

describe('MockDataLoader', () => {
  let loader: MockDataLoader;
  let http: HttpTestingController;
  let db: MockDb;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    loader = TestBed.inject(MockDataLoader);
    http = TestBed.inject(HttpTestingController);
    db = TestBed.inject(MockDb);
  });

  afterEach(() => http.verify());

  it('starts empty so nothing is seeded until the file arrives', () => {
    expect(db.isSeeded).toBe(false);
    expect(db.organizations).toEqual([]);
  });

  it('seeds the database from the fetched file', () => {
    loader.load().subscribe();

    http.expectOne(MOCK_DATA_URL).flush({
      organizations: [{ id: 'org-a', name: 'Northwind', description: 'Logistics' }],
    });

    expect(db.isSeeded).toBe(true);
    expect(db.organizations).toEqual([{ id: 'org-a', name: 'Northwind', description: 'Logistics' }]);
  });

  it('leaves the app usable and empty when the file is missing', () => {
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    let emitted = false;

    loader.load().subscribe(() => (emitted = true));
    http.expectOne(MOCK_DATA_URL).flush('nope', { status: 404, statusText: 'Not Found' });

    expect(emitted).toBe(true);
    expect(db.isSeeded).toBe(false);
    expect(logged).toHaveBeenCalledOnce();
    logged.mockRestore();
  });

  it('reports the offending field when the file is malformed', () => {
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);

    loader.load().subscribe();
    http.expectOne(MOCK_DATA_URL).flush({ users: [{ id: 'user-a1' }] });

    expect(db.isSeeded).toBe(false);
    expect(logged.mock.calls[0][0]).toContain('users[0]');
    logged.mockRestore();
  });
});
