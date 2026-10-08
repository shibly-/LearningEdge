/** A file attached to a training. */
export interface TrainingFile {
  readonly id: string;
  readonly fileName: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly uploadedByUserId: string;
  readonly createdAt: string;
}

/** GET /api/v1/category/{categoryId}/training -> TrainingDTO[] */
export interface Training {
  readonly id: string;
  /**
   * Not part of TrainingDTO: the repository fills it in from the category the
   * training was loaded through, so tenant filtering keeps working.
   */
  readonly organizationId: string;
  readonly categoryId: string;
  readonly name: string;
  readonly description: string;
  readonly isActive: boolean;
  readonly files: readonly TrainingFile[];
}

/** POST /api/v1/category/{categoryId}/training -> 201 with the new GUID */
export interface CreateTrainingCommand {
  readonly organizationId: string;
  readonly categoryId: string;
  readonly name: string;
  readonly description: string;
  readonly isActive: boolean;
}

/** PUT /api/v1/category/{categoryId}/training/{id} -> TrainingDTO */
export interface UpdateTrainingCommand extends CreateTrainingCommand {
  readonly id: string;
}

/** Mirrors the server-side validators. */
export const TRAINING_LIMITS = { name: 100, description: 500 } as const;

/** Mirrors TrainingFileRules on the server. */
export const TRAINING_FILE_RULES = {
  extensions: ['.pdf', '.docx', '.txt'],
  maxFiles: 10,
  maxFileBytes: 20 * 1024 * 1024,
  maxTotalBytes: 100 * 1024 * 1024,
} as const;

/** Returns a user-facing problem with the selection, or null when it can be uploaded. */
export function validateTrainingFiles(files: readonly File[]): string | null {
  if (files.length === 0) {
    return 'Choose at least one file.';
  }
  if (files.length > TRAINING_FILE_RULES.maxFiles) {
    return `Upload at most ${TRAINING_FILE_RULES.maxFiles} files at once.`;
  }
  for (const file of files) {
    const name = file.name.toLowerCase();
    if (!TRAINING_FILE_RULES.extensions.some((ext) => name.endsWith(ext))) {
      return `${file.name} is not a PDF, DOCX or TXT file.`;
    }
    if (file.size === 0) {
      return `${file.name} is empty.`;
    }
    if (file.size > TRAINING_FILE_RULES.maxFileBytes) {
      return `${file.name} is larger than 20 MB.`;
    }
  }
  const total = files.reduce((sum, file) => sum + file.size, 0);
  if (total > TRAINING_FILE_RULES.maxTotalBytes) {
    return 'The selected files add up to more than 100 MB.';
  }
  return null;
}

// MOCK: assignments and results have no backend endpoint. Frontend-owned contract (spec 3.7).

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
