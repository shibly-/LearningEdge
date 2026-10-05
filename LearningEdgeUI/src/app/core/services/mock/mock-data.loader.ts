import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { SKIP_API_CONCERNS } from '../../http/http-context';
import { MOCK_DATA_URL, type MockDataset } from './mock-data.model';
import { MockDataError, parseMockDataset } from './mock-data.parser';
import { MockDb } from './mock-db';

/**
 * Fetches public/mock-data/tms-sample-data.json over HTTP and seeds MockDb.
 *
 * Deliberately an HTTP fetch rather than a bundled import: the repositories
 * already speak in Observables, so pointing them at real REST endpoints later
 * is a URL change rather than a restructure.
 */
@Injectable({ providedIn: 'root' })
export class MockDataLoader {
  private readonly http = inject(HttpClient);
  private readonly db = inject(MockDb);

  load(): Observable<MockDataset> {
    return this.http
      .get<unknown>(MOCK_DATA_URL, {
        context: new HttpContext().set(SKIP_API_CONCERNS, true),
      })
      .pipe(
        map((raw) => parseMockDataset(raw)),
        tap((dataset) => this.db.seed(dataset)),
        catchError((error: unknown) => {
          // Never block bootstrap: log precisely and let every screen render
          // its empty state instead of hanging on a white page.
          console.error(describeLoadFailure(error));
          return of(this.db.snapshot());
        }),
      );
  }
}

function describeLoadFailure(error: unknown): string {
  if (error instanceof MockDataError) {
    return `Sample data is malformed, so the app started empty. ${error.message}`;
  }
  return `Could not load ${MOCK_DATA_URL}, so the app started empty. Confirm the file exists under public/mock-data/.`;
}
