import type { QuestionType } from './question';

/** Mirrors PromptStatus on the server. JSON sends the numeric value. */
export enum PromptStatus {
  Pending = 0,
  Completed = 1,
  Failed = 2,
}

export interface PromptRequest {
  readonly id: string;
  readonly trainingId: string;
  readonly questionBankId: string;
  readonly promptText: string;
  readonly questionType: QuestionType;
  readonly questionCount: number;
  readonly requestedOn: string;
  readonly status: PromptStatus;
}

export interface SubmitPromptRequestCommand {
  readonly trainingId: string;
  readonly questionBankId: string;
  readonly promptText: string;
  readonly questionType: QuestionType;
  readonly questionCount: number;
}

export const PROMPT_LIMITS = { text: 4000, minCount: 1, maxCount: 50 } as const;

export function promptStatusLabel(status: PromptStatus): string {
  switch (status) {
    case PromptStatus.Completed:
      return 'Completed';
    case PromptStatus.Failed:
      return 'Failed';
    default:
      return 'Pending';
  }
}

export function promptStatusBadge(status: PromptStatus): string {
  switch (status) {
    case PromptStatus.Completed:
      return 'complete';
    case PromptStatus.Failed:
      return 'failed';
    default:
      return 'parsing';
  }
}
