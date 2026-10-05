import { Injectable, inject } from '@angular/core';
import { Observable, delay, of, throwError } from 'rxjs';
import { ApiFailure } from '../http/api-result';
import {
  ACCEPTED_UPLOAD_EXTENSIONS,
  MAX_UPLOAD_BYTES,
  type ProcessingJob,
  type ProcessingStage,
} from '../models/processing-job';
import { MockDb } from './mock/mock-db';
import { MOCK_LATENCY_MS, notImplementedUpstream } from './repository-support';

/**
 * MOCK: no orchestrator endpoint exists (spec 9).
 *
 * Polling is deliberately behind this abstraction so it can be replaced with
 * SignalR or WebSocket push without touching any component.
 */
export abstract class ProcessingRepository {
  /** Jobs already on record for the tenant, newest first. */
  abstract listRecent(organizationId: string): Observable<readonly ProcessingJob[]>;
  abstract start(organizationId: string, file: File): Observable<ProcessingJob>;
  abstract status(jobId: string): Observable<ProcessingJob>;
  abstract retry(jobId: string): Observable<ProcessingJob>;
}

export function validateUpload(file: File): string | null {
  const name = file.name.toLowerCase();
  const allowed = ACCEPTED_UPLOAD_EXTENSIONS.some((ext) => name.endsWith(ext));
  if (!allowed) {
    return `${file.name} is not a .pdf or .docx file.`;
  }
  if (file.size > MAX_UPLOAD_BYTES) {
    const limitMb = Math.round(MAX_UPLOAD_BYTES / (1024 * 1024));
    return `${file.name} exceeds the ${limitMb} MB limit.`;
  }
  if (file.size === 0) {
    return `${file.name} is empty.`;
  }
  return null;
}

@Injectable()
export class HttpProcessingRepository extends ProcessingRepository {
  override listRecent(_organizationId: string): Observable<readonly ProcessingJob[]> {
    return throwError(() => notImplementedUpstream('Listing processing jobs'));
  }

  override start(_organizationId: string, _file: File): Observable<ProcessingJob> {
    return throwError(() => notImplementedUpstream('Starting a processing job'));
  }

  override status(_jobId: string): Observable<ProcessingJob> {
    return throwError(() => notImplementedUpstream('Polling a processing job'));
  }

  override retry(_jobId: string): Observable<ProcessingJob> {
    return throwError(() => notImplementedUpstream('Retrying a processing job'));
  }
}

interface Timeline {
  readonly startedAtMs: number;
  /** Files named *fail* exercise the partial-failure path in dev. */
  readonly shouldFail: boolean;
}

const STAGE_TIMELINE: readonly { readonly until: number; readonly stage: ProcessingStage }[] = [
  { until: 1200, stage: 'uploading' },
  { until: 2600, stage: 'parsing' },
  { until: 4000, stage: 'storing' },
  { until: 5600, stage: 'generating' },
];
const TOTAL_MS = 5600;
const FAIL_AT_MS = 2000;

@Injectable()
export class MockProcessingRepository extends ProcessingRepository {
  private readonly db = inject(MockDb);
  private readonly timelines = new Map<string, Timeline>();

  override listRecent(organizationId: string): Observable<readonly ProcessingJob[]> {
    const jobs = this.db.processingJobs.filter((job) => job.organizationId === organizationId);
    return of([...jobs].sort((a, b) => b.startedAt.localeCompare(a.startedAt))).pipe(
      delay(MOCK_LATENCY_MS),
    );
  }

  override start(organizationId: string, file: File): Observable<ProcessingJob> {
    const invalid = validateUpload(file);
    if (invalid !== null) {
      return throwError(() => new ApiFailure('client', invalid));
    }

    const job: ProcessingJob = {
      id: this.db.nextId('job'),
      organizationId,
      trainingId: null,
      fileName: file.name,
      fileSize: file.size,
      stage: 'uploading',
      progress: 0,
      status: 'active',
      error: null,
      questionCount: 0,
      startedAt: new Date().toISOString(),
    };

    this.timelines.set(job.id, {
      startedAtMs: Date.now(),
      shouldFail: file.name.toLowerCase().includes('fail'),
    });
    this.db.processingJobs = [...this.db.processingJobs, job];

    return of(job).pipe(delay(150));
  }

  override status(jobId: string): Observable<ProcessingJob> {
    const job = this.db.processingJobs.find((candidate) => candidate.id === jobId);
    const timeline = this.timelines.get(jobId);

    if (job === undefined || timeline === undefined) {
      return throwError(() => new ApiFailure('not-found', 'That processing job no longer exists.'));
    }

    const advanced = advance(job, timeline);
    this.db.processingJobs = this.db.processingJobs.map((candidate) =>
      candidate.id === jobId ? advanced : candidate,
    );
    return of(advanced).pipe(delay(120));
  }

  override retry(jobId: string): Observable<ProcessingJob> {
    const job = this.db.processingJobs.find((candidate) => candidate.id === jobId);
    if (job === undefined) {
      return throwError(() => new ApiFailure('not-found', 'That processing job no longer exists.'));
    }

    const restarted: ProcessingJob = {
      ...job,
      stage: 'uploading',
      progress: 0,
      status: 'active',
      error: null,
      questionCount: 0,
      startedAt: new Date().toISOString(),
    };
    // A retry succeeds, so the failure path is reachable but not a dead end.
    this.timelines.set(jobId, { startedAtMs: Date.now(), shouldFail: false });
    this.db.processingJobs = this.db.processingJobs.map((candidate) =>
      candidate.id === jobId ? restarted : candidate,
    );
    return of(restarted).pipe(delay(150));
  }
}

function advance(job: ProcessingJob, timeline: Timeline): ProcessingJob {
  if (job.status !== 'active') {
    return job;
  }

  const elapsed = Date.now() - timeline.startedAtMs;

  if (timeline.shouldFail && elapsed >= FAIL_AT_MS) {
    return {
      ...job,
      stage: 'failed',
      status: 'failed',
      progress: 35,
      error: 'The document could not be parsed. It may be scanned, encrypted, or corrupt.',
    };
  }

  if (elapsed >= TOTAL_MS) {
    return {
      ...job,
      stage: 'complete',
      status: 'complete',
      progress: 100,
      error: null,
      questionCount: 8 + (job.fileName.length % 7),
    };
  }

  const stage = STAGE_TIMELINE.find((entry) => elapsed < entry.until)?.stage ?? 'generating';
  const progress = Math.min(99, Math.round((elapsed / TOTAL_MS) * 100));
  return { ...job, stage, progress };
}
