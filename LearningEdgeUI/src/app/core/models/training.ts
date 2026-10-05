// MOCK: no backend endpoint. Frontend-owned contract (spec 3.7).

export type TrainingStatus = 'draft' | 'published' | 'archived';

export interface Training {
  readonly id: string;
  readonly organizationId: string;
  readonly categoryId: string;
  readonly title: string;
  readonly description: string;
  readonly status: TrainingStatus;
  /** Minutes. Rendered by the duration pipe. */
  readonly durationMinutes: number;
  readonly passMark: number;
  readonly createdAt: string;
}

export interface CreateTrainingCommand {
  readonly organizationId: string;
  readonly categoryId: string;
  readonly title: string;
  readonly description: string;
  readonly durationMinutes: number;
  readonly passMark: number;
}

export type AssignmentStatus = 'assigned' | 'in-progress' | 'completed' | 'overdue';

export interface TrainingAssignment {
  readonly id: string;
  readonly organizationId: string;
  readonly trainingId: string;
  readonly trainingTitle: string;
  readonly traineeId: string;
  readonly traineeName: string;
  readonly assignedById: string;
  readonly assignedAt: string;
  readonly dueAt: string | null;
  readonly status: AssignmentStatus;
}

export interface AssignTrainingCommand {
  readonly organizationId: string;
  readonly trainingId: string;
  readonly traineeIds: readonly string[];
  readonly dueAt: string | null;
}

export interface TrainingResult {
  readonly id: string;
  readonly organizationId: string;
  readonly assignmentId: string;
  readonly trainingId: string;
  readonly trainingTitle: string;
  readonly traineeId: string;
  readonly score: number;
  readonly passMark: number;
  readonly passed: boolean;
  readonly completedAt: string;
}
