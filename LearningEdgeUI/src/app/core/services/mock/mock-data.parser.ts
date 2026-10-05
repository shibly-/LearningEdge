import type { Category } from '../../models/category';
import type { Message } from '../../models/message';
import type { OrganizationDto } from '../../models/organization';
import type { ProcessingJob, ProcessingStage, ProcessingStatus } from '../../models/processing-job';
import type {
  AssignmentStatus,
  Training,
  TrainingAssignment,
  TrainingResult,
  TrainingStatus,
} from '../../models/training';
import type { UserDto } from '../../models/user';
import { UserRole, isUserRole } from '../../models/user-role';
import type { MockDataset } from './mock-data.model';

export class MockDataError extends Error {
  constructor(message: string) {
    super(`tms-sample-data.json: ${message}`);
    this.name = 'MockDataError';
  }
}

const RELATIVE_DATE = /^([+-]?\d+)d$/;

/**
 * Date fields accept an ISO string or a relative offset ('+14d', '-3d', '0d')
 * so the samples never go stale. Returns an ISO string either way.
 */
export function resolveDate(value: unknown, field: string, now: Date = new Date()): string {
  if (typeof value !== 'string' || value.trim().length === 0) {
    throw new MockDataError(`${field} must be an ISO date string or a relative offset like "+14d".`);
  }

  const match = RELATIVE_DATE.exec(value.trim());
  if (match !== null) {
    const shifted = new Date(now.getTime());
    shifted.setDate(shifted.getDate() + Number(match[1]));
    return shifted.toISOString();
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    throw new MockDataError(`${field} is not a valid date: "${value}".`);
  }
  return parsed.toISOString();
}

function resolveNullableDate(value: unknown, field: string, now: Date): string | null {
  return value === null || value === undefined ? null : resolveDate(value, field, now);
}

/**
 * Parses and validates the raw JSON. Throws with a precise message rather than
 * letting a malformed sample file surface as a confusing empty screen.
 */
export function parseMockDataset(raw: unknown, now: Date = new Date()): MockDataset {
  const root = asRecord(raw, 'root');

  return {
    organizations: collection(root, 'organizations').map((entry, i) =>
      parseOrganization(entry, `organizations[${i}]`),
    ),
    users: collection(root, 'users').map((entry, i) => parseUser(entry, `users[${i}]`)),
    categories: collection(root, 'categories').map((entry, i) =>
      parseCategory(entry, `categories[${i}]`, now),
    ),
    trainings: collection(root, 'trainings').map((entry, i) =>
      parseTraining(entry, `trainings[${i}]`, now),
    ),
    assignments: collection(root, 'assignments').map((entry, i) =>
      parseAssignment(entry, `assignments[${i}]`, now),
    ),
    results: collection(root, 'results').map((entry, i) => parseResult(entry, `results[${i}]`, now)),
    messages: collection(root, 'messages').map((entry, i) =>
      parseMessage(entry, `messages[${i}]`, now),
    ),
    processingJobs: collection(root, 'processingJobs').map((entry, i) =>
      parseProcessingJob(entry, `processingJobs[${i}]`, now),
    ),
  };
}

function asRecord(value: unknown, field: string): Record<string, unknown> {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) {
    throw new MockDataError(`${field} must be an object.`);
  }
  return value as Record<string, unknown>;
}

/** Missing collections are tolerated as empty; a wrong type is not. */
function collection(root: Record<string, unknown>, key: string): readonly unknown[] {
  const value = root[key];
  if (value === undefined) {
    return [];
  }
  if (!Array.isArray(value)) {
    throw new MockDataError(`"${key}" must be an array.`);
  }
  return value;
}

function str(record: Record<string, unknown>, key: string, field: string): string {
  const value = record[key];
  if (typeof value !== 'string' || value.length === 0) {
    throw new MockDataError(`${field}.${key} must be a non-empty string.`);
  }
  return value;
}

function optionalStr(record: Record<string, unknown>, key: string): string {
  const value = record[key];
  return typeof value === 'string' ? value : '';
}

function nullableStr(record: Record<string, unknown>, key: string, field: string): string | null {
  const value = record[key];
  if (value === null || value === undefined) {
    return null;
  }
  if (typeof value !== 'string') {
    throw new MockDataError(`${field}.${key} must be a string or null.`);
  }
  return value;
}

function num(record: Record<string, unknown>, key: string, field: string): number {
  const value = record[key];
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    throw new MockDataError(`${field}.${key} must be a number.`);
  }
  return value;
}

function bool(record: Record<string, unknown>, key: string, field: string): boolean {
  const value = record[key];
  if (typeof value !== 'boolean') {
    throw new MockDataError(`${field}.${key} must be true or false.`);
  }
  return value;
}

function oneOf<T extends string>(
  record: Record<string, unknown>,
  key: string,
  allowed: readonly T[],
  field: string,
): T {
  const value = record[key];
  if (typeof value !== 'string' || !allowed.includes(value as T)) {
    throw new MockDataError(`${field}.${key} must be one of: ${allowed.join(', ')}.`);
  }
  return value as T;
}

function parseOrganization(entry: unknown, field: string): OrganizationDto {
  const record = asRecord(entry, field);
  return {
    id: str(record, 'id', field),
    name: str(record, 'name', field),
    description: optionalStr(record, 'description'),
  };
}

function parseUser(entry: unknown, field: string): UserDto {
  const record = asRecord(entry, field);
  const role = record['role'];
  if (!isUserRole(role)) {
    throw new MockDataError(
      `${field}.role must be a number 1-4 (1 Learner, 2 Instructor, 3 OrgAdmin, 4 SysAdmin).`,
    );
  }

  return {
    id: str(record, 'id', field),
    firstName: str(record, 'firstName', field),
    lastName: optionalStr(record, 'lastName'),
    email: str(record, 'email', field),
    role: role as UserRole,
    organizationId: str(record, 'organizationId', field),
  };
}

function parseCategory(entry: unknown, field: string, now: Date): Category {
  const record = asRecord(entry, field);
  return {
    id: str(record, 'id', field),
    organizationId: str(record, 'organizationId', field),
    name: str(record, 'name', field),
    description: optionalStr(record, 'description'),
    trainingCount: num(record, 'trainingCount', field),
    createdAt: resolveDate(record['createdAt'], `${field}.createdAt`, now),
  };
}

const TRAINING_STATUSES: readonly TrainingStatus[] = ['draft', 'published', 'archived'];

function parseTraining(entry: unknown, field: string, now: Date): Training {
  const record = asRecord(entry, field);
  return {
    id: str(record, 'id', field),
    organizationId: str(record, 'organizationId', field),
    categoryId: str(record, 'categoryId', field),
    title: str(record, 'title', field),
    description: optionalStr(record, 'description'),
    status: oneOf(record, 'status', TRAINING_STATUSES, field),
    durationMinutes: num(record, 'durationMinutes', field),
    passMark: num(record, 'passMark', field),
    createdAt: resolveDate(record['createdAt'], `${field}.createdAt`, now),
  };
}

const ASSIGNMENT_STATUSES: readonly AssignmentStatus[] = [
  'assigned',
  'in-progress',
  'completed',
  'overdue',
];

function parseAssignment(entry: unknown, field: string, now: Date): TrainingAssignment {
  const record = asRecord(entry, field);
  return {
    id: str(record, 'id', field),
    organizationId: str(record, 'organizationId', field),
    trainingId: str(record, 'trainingId', field),
    trainingTitle: str(record, 'trainingTitle', field),
    traineeId: str(record, 'traineeId', field),
    traineeName: str(record, 'traineeName', field),
    assignedById: str(record, 'assignedById', field),
    assignedAt: resolveDate(record['assignedAt'], `${field}.assignedAt`, now),
    dueAt: resolveNullableDate(record['dueAt'], `${field}.dueAt`, now),
    status: oneOf(record, 'status', ASSIGNMENT_STATUSES, field),
  };
}

function parseResult(entry: unknown, field: string, now: Date): TrainingResult {
  const record = asRecord(entry, field);
  return {
    id: str(record, 'id', field),
    organizationId: str(record, 'organizationId', field),
    assignmentId: str(record, 'assignmentId', field),
    trainingId: str(record, 'trainingId', field),
    trainingTitle: str(record, 'trainingTitle', field),
    traineeId: str(record, 'traineeId', field),
    score: num(record, 'score', field),
    passMark: num(record, 'passMark', field),
    passed: bool(record, 'passed', field),
    completedAt: resolveDate(record['completedAt'], `${field}.completedAt`, now),
  };
}

function parseMessage(entry: unknown, field: string, now: Date): Message {
  const record = asRecord(entry, field);
  return {
    id: str(record, 'id', field),
    organizationId: str(record, 'organizationId', field),
    senderId: str(record, 'senderId', field),
    senderName: str(record, 'senderName', field),
    recipientId: str(record, 'recipientId', field),
    recipientName: str(record, 'recipientName', field),
    subject: str(record, 'subject', field),
    body: optionalStr(record, 'body'),
    sentAt: resolveDate(record['sentAt'], `${field}.sentAt`, now),
    read: bool(record, 'read', field),
  };
}

const PROCESSING_STAGES: readonly ProcessingStage[] = [
  'uploading',
  'parsing',
  'storing',
  'generating',
  'complete',
  'failed',
];
const PROCESSING_STATUSES: readonly ProcessingStatus[] = ['active', 'complete', 'failed'];

function parseProcessingJob(entry: unknown, field: string, now: Date): ProcessingJob {
  const record = asRecord(entry, field);
  return {
    id: str(record, 'id', field),
    organizationId: str(record, 'organizationId', field),
    trainingId: nullableStr(record, 'trainingId', field),
    fileName: str(record, 'fileName', field),
    fileSize: num(record, 'fileSize', field),
    stage: oneOf(record, 'stage', PROCESSING_STAGES, field),
    progress: num(record, 'progress', field),
    status: oneOf(record, 'status', PROCESSING_STATUSES, field),
    error: nullableStr(record, 'error', field),
    questionCount: num(record, 'questionCount', field),
    startedAt: resolveDate(record['startedAt'], `${field}.startedAt`, now),
  };
}
