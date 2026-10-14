/** GET /api/v1/training/{trainingId}/question-bank -> QuestionBankDTO[] */
export interface QuestionBank {
  readonly id: string;
  readonly trainingId: string;
  readonly title: string;
  readonly isActive: boolean;
  readonly createdAt: string;
}

export interface CreateQuestionBankCommand {
  readonly trainingId: string;
  readonly title: string;
  readonly isActive: boolean;
}

export interface UpdateQuestionBankCommand extends CreateQuestionBankCommand {
  readonly id: string;
}

export const QUESTION_BANK_LIMITS = { title: 200 } as const;
