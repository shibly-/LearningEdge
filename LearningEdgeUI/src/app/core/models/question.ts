/** Mirrors QuestionType on the server. JSON sends the numeric value. */
export enum QuestionType {
  Mcq = 0,
  TrueFalse = 1,
  MultipleSelection = 2,
}

export const QUESTION_TYPE_OPTIONS: readonly { id: QuestionType; label: string }[] = [
  { id: QuestionType.Mcq, label: 'Multiple choice' },
  { id: QuestionType.TrueFalse, label: 'True / False' },
  { id: QuestionType.MultipleSelection, label: 'Multiple selection' },
];

export interface Question {
  readonly id: string;
  readonly questionBankId: string;
  readonly text: string;
  readonly score: number;
  readonly isActive: boolean;
  readonly type: QuestionType;
  readonly options: readonly string[];
  readonly correctOptions: readonly string[];
  readonly correctValue: boolean | null;
  readonly createdAt: string;
}

export interface QuestionContent {
  readonly text: string;
  readonly score: number;
  readonly isActive: boolean;
  readonly options: readonly string[];
  readonly correctOptions: readonly string[];
  readonly correctValue: boolean | null;
}

export interface UpdateQuestionCommand extends QuestionContent {
  readonly trainingId: string;
  readonly questionBankId: string;
  readonly id: string;
}

export const QUESTION_LIMITS = {
  text: 2000,
  minScore: 1,
  maxScore: 100,
  minOptions: 2,
  maxOptions: 8,
  option: 300,
} as const;

export function questionTypeLabel(type: QuestionType): string {
  return QUESTION_TYPE_OPTIONS.find((option) => option.id === type)?.label ?? 'Question';
}

export function questionDetails(question: Question): string {
  if (question.type === QuestionType.TrueFalse) {
    return 'True or false';
  }
  return question.options.join(' · ');
}

export function questionAnswer(question: Question): string {
  if (question.type === QuestionType.TrueFalse) {
    return question.correctValue ? 'True' : 'False';
  }
  return question.correctOptions.join(', ');
}
