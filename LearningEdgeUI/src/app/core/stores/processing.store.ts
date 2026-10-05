import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import type { ProcessingJob } from '../models/processing-job';
import { OrganizationContextService } from '../services/organization-context.service';
import { ProcessingRepository, validateUpload } from '../services/processing.repository';
import { TenantResetBus } from '../services/tenant-reset-bus';
import { ToastService } from '../services/toast.service';
import { describeError } from './async-collection.store';

const POLL_BASE_MS = 700;
const POLL_MAX_MS = 4000;

/**
 * Jobs live in the store, not the component, so progress survives navigation
 * away from the upload screen (spec 9).
 */
@Injectable({ providedIn: 'root' })
export class ProcessingStore {
  private readonly repository = inject(ProcessingRepository);
  private readonly context = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly jobsState = signal<readonly ProcessingJob[]>([]);
  private readonly timers = new Map<string, ReturnType<typeof setTimeout>>();
  private readonly backoff = new Map<string, number>();

  readonly jobs = this.jobsState.asReadonly();
  readonly activeJobs = computed(() => this.jobsState().filter((job) => job.status === 'active'));
  readonly failedJobs = computed(() => this.jobsState().filter((job) => job.status === 'failed'));
  readonly completedJobs = computed(() =>
    this.jobsState().filter((job) => job.status === 'complete'),
  );
  readonly isEmpty = computed(() => this.jobsState().length === 0);
  readonly hasActiveWork = computed(() => this.activeJobs().length > 0);

  constructor() {
    inject(TenantResetBus).register(() => this.reset());
  }

  reset(): void {
    for (const timer of this.timers.values()) {
      clearTimeout(timer);
    }
    this.timers.clear();
    this.backoff.clear();
    this.jobsState.set([]);
  }

  /** Jobs started before this page was opened, so history is not lost on reload. */
  loadRecent(): void {
    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      return;
    }

    this.repository
      .listRecent(organizationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (jobs) => {
          for (const job of jobs) {
            this.upsert(job);
            if (job.status === 'active') {
              this.schedulePoll(job.id);
            }
          }
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  /** One rejected file must not fail the batch. */
  upload(files: readonly File[]): void {
    const organizationId = this.context.activeOrganizationId();
    if (organizationId === null) {
      this.toast.error('Select an organization before uploading material.');
      return;
    }

    for (const file of files) {
      const invalid = validateUpload(file);
      if (invalid !== null) {
        this.toast.warning(invalid);
        continue;
      }
      this.start(organizationId, file);
    }
  }

  retry(jobId: string): void {
    this.repository
      .retry(jobId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (job) => {
          this.upsert(job);
          this.schedulePoll(job.id);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  dismiss(jobId: string): void {
    const timer = this.timers.get(jobId);
    if (timer !== undefined) {
      clearTimeout(timer);
      this.timers.delete(jobId);
    }
    this.backoff.delete(jobId);
    this.jobsState.update((jobs) => jobs.filter((job) => job.id !== jobId));
  }

  private start(organizationId: string, file: File): void {
    this.repository
      .start(organizationId, file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (job) => {
          this.upsert(job);
          this.schedulePoll(job.id);
        },
        error: (error: unknown) => this.toast.error(describeError(error)),
      });
  }

  private schedulePoll(jobId: string): void {
    const previous = this.backoff.get(jobId) ?? POLL_BASE_MS;
    const next = Math.min(previous * 1.4, POLL_MAX_MS);
    this.backoff.set(jobId, next);

    const timer = setTimeout(() => this.poll(jobId), previous);
    this.timers.set(jobId, timer);
  }

  private poll(jobId: string): void {
    this.repository
      .status(jobId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (job) => {
          this.upsert(job);
          if (job.status === 'active') {
            this.schedulePoll(jobId);
            return;
          }

          this.timers.delete(jobId);
          this.backoff.delete(jobId);
          if (job.status === 'complete') {
            this.toast.success(`${job.fileName}: ${job.questionCount} questions generated.`);
          } else {
            this.toast.error(`${job.fileName} failed. ${job.error ?? ''}`.trim());
          }
        },
        error: (error: unknown) => {
          this.timers.delete(jobId);
          this.toast.error(describeError(error));
        },
      });
  }

  private upsert(job: ProcessingJob): void {
    this.jobsState.update((jobs) => {
      const index = jobs.findIndex((candidate) => candidate.id === job.id);
      if (index === -1) {
        return [...jobs, job];
      }
      const next = [...jobs];
      next[index] = job;
      return next;
    });
  }
}
