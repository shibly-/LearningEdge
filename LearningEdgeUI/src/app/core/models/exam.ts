export interface Exam {
  readonly id: string;
  readonly trainingId: string;
  readonly preparedByUserId: string;
  readonly title: string;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface CreateExamCommand {
  readonly trainingId: string;
  readonly preparedByUserId: string;
  readonly title: string;
  readonly isActive: boolean;
}

export interface UpdateExamCommand {
  readonly trainingId: string;
  readonly id: string;
  readonly title: string;
  readonly isActive: boolean;
}

export interface AddExamQuestionCommand {
  readonly trainingId: string;
  readonly examId: string;
  readonly questionBankId: string;
  readonly questionId: string;
}

export const EXAM_LIMITS = { title: 200 } as const;
