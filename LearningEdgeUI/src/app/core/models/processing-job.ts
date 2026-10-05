// MOCK: no backend endpoint. Frontend-owned contract (spec 3.7 / 9).

/**
 * Mirrors the orchestrator pipeline: parse the document, store paragraphs and
 * embeddings, generate questionnaires, then report completion.
 */
export type ProcessingStage =
  'uploading' | 'parsing' | 'storing' | 'generating' | 'complete' | 'failed';

export type ProcessingStatus = 'active' | 'complete' | 'failed';

export interface ProcessingJob {
  readonly id: string;
  readonly organizationId: string;
  readonly trainingId: string | null;
  readonly fileName: string;
  readonly fileSize: number;
  readonly stage: ProcessingStage;
  /** 0-100 across the whole pipeline, not per stage. */
  readonly progress: number;
  readonly status: ProcessingStatus;
  readonly error: string | null;
  readonly questionCount: number;
  readonly startedAt: string;
}

export const PROCESSING_STAGE_ORDER: readonly ProcessingStage[] = [
  'uploading',
  'parsing',
  'storing',
  'generating',
  'complete',
];

const STAGE_LABELS: Readonly<Record<ProcessingStage, string>> = {
  uploading: 'Uploading',
  parsing: 'Extracting text',
  storing: 'Storing paragraphs',
  generating: 'Generating questions',
  complete: 'Complete',
  failed: 'Failed',
};

export function stageLabel(stage: ProcessingStage): string {
  return STAGE_LABELS[stage];
}

export const ACCEPTED_UPLOAD_EXTENSIONS: readonly string[] = ['.pdf', '.docx'];
export const MAX_UPLOAD_BYTES = 25 * 1024 * 1024;
