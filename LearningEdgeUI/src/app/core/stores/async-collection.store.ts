import { DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { Observable } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import { TenantResetBus } from '../services/tenant-reset-bus';

export type LoadStatus = 'idle' | 'loading' | 'success' | 'error';

/**
 * Async state is modelled explicitly so every screen can render the four
 * required states: loading, empty, error, populated (spec 5, spec 7).
 *
 * Registers itself with TenantResetBus, so switching organization clears it.
 */
export abstract class AsyncCollectionStore<T> {
  protected readonly destroyRef = inject(DestroyRef);

  private readonly itemsState = signal<readonly T[]>([]);
  private readonly statusState = signal<LoadStatus>('idle');
  private readonly errorState = signal<string | null>(null);

  readonly items = this.itemsState.asReadonly();
  readonly status = this.statusState.asReadonly();
  readonly error = this.errorState.asReadonly();

  readonly isLoading = computed(() => this.statusState() === 'loading');
  readonly hasError = computed(() => this.statusState() === 'error');
  readonly isEmpty = computed(
    () => this.statusState() === 'success' && this.itemsState().length === 0,
  );
  readonly count = computed(() => this.itemsState().length);

  constructor() {
    inject(TenantResetBus).register(() => this.reset());
  }

  reset(): void {
    this.itemsState.set([]);
    this.statusState.set('idle');
    this.errorState.set(null);
  }

  protected replace(items: readonly T[]): void {
    this.itemsState.set(items);
    this.statusState.set('success');
    this.errorState.set(null);
  }

  protected load(source: Observable<readonly T[]>): void {
    this.statusState.set('loading');
    this.errorState.set(null);

    source.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (items) => {
        this.itemsState.set(items);
        this.statusState.set('success');
      },
      error: (error: unknown) => {
        this.errorState.set(describeError(error));
        this.statusState.set('error');
      },
    });
  }
}

export function describeError(error: unknown): string {
  if (error instanceof ApiFailure) {
    return error.message;
  }
  if (error instanceof Error && error.message.length > 0) {
    return error.message;
  }
  return 'Something went wrong. Please try again.';
}
