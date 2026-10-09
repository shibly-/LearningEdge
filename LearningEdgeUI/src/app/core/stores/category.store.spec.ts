import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import type { Category } from '../models/category';
import { ApiFailure } from '../http/api-result';
import { CategoryRepository } from '../services/category.repository';
import { OrganizationContextService } from '../services/organization-context.service';
import { TenantResetBus } from '../services/tenant-reset-bus';
import { ToastService } from '../services/toast.service';
import { CategoryStore } from './category.store';

const ORG_A = 'dcd9946b-908d-459e-85c2-66b590c50ad2';

const sample: Category = {
  id: 'cat-1',
  organizationId: ORG_A,
  name: 'Workplace Safety',
  description: '',
  isActive: true,
};

function setup(activeOrganizationId: string | null = ORG_A) {
  const source = new Subject<readonly Category[]>();

  TestBed.configureTestingModule({
    providers: [
      {
        provide: CategoryRepository,
        useValue: { listByOrganization: () => source.asObservable() },
      },
      {
        provide: OrganizationContextService,
        useValue: { activeOrganizationId: () => activeOrganizationId },
      },
      { provide: ToastService, useValue: { success: () => undefined, error: () => undefined } },
    ],
  });

  return { store: TestBed.inject(CategoryStore), source };
}

describe('CategoryStore', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('starts idle with nothing loaded', () => {
    const { store } = setup();

    expect(store.status()).toBe('idle');
    expect(store.items()).toEqual([]);
    expect(store.isEmpty()).toBe(false);
  });

  it('moves idle -> loading -> success and exposes the items', () => {
    const { store, source } = setup();

    store.loadForActiveOrganization();
    expect(store.status()).toBe('loading');
    expect(store.isLoading()).toBe(true);

    source.next([sample]);

    expect(store.status()).toBe('success');
    expect(store.count()).toBe(1);
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  // isEmpty must distinguish "loaded nothing" from "not loaded yet".
  it('reports empty only after a successful load returned no rows', () => {
    const { store, source } = setup();

    store.loadForActiveOrganization();
    expect(store.isEmpty()).toBe(false);

    source.next([]);

    expect(store.isEmpty()).toBe(true);
    expect(store.status()).toBe('success');
  });

  it('moves loading -> error and surfaces the failure message', () => {
    const { store, source } = setup();

    store.loadForActiveOrganization();
    source.error(new ApiFailure('client', 'No list endpoint exists for category.'));

    expect(store.status()).toBe('error');
    expect(store.hasError()).toBe(true);
    expect(store.error()).toBe('No list endpoint exists for category.');
  });

  it('falls back to a generic message for a non-ApiFailure error', () => {
    const { store, source } = setup();

    store.loadForActiveOrganization();
    source.error({ weird: true });

    expect(store.error()).toBe('Something went wrong. Please try again.');
  });

  it('resets instead of loading when no organization is active', () => {
    const { store } = setup(null);

    store.loadForActiveOrganization();

    expect(store.status()).toBe('idle');
    expect(store.items()).toEqual([]);
  });

  it('clears itself when the tenant reset bus fires', () => {
    const { store, source } = setup();

    store.loadForActiveOrganization();
    source.next([sample]);
    expect(store.count()).toBe(1);

    TestBed.inject(TenantResetBus).resetAll();

    expect(store.count()).toBe(0);
    expect(store.status()).toBe('idle');
    expect(store.error()).toBeNull();
  });

  it('sorts by name', () => {
    const { store, source } = setup();

    store.loadForActiveOrganization();
    source.next([
      { ...sample, id: 'b', name: 'Zebra' },
      { ...sample, id: 'a', name: 'Alpha' },
    ]);

    expect(store.sortedByName().map((category) => category.name)).toEqual(['Alpha', 'Zebra']);
  });
});
