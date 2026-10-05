import type { Category } from '../../models/category';
import type { Message } from '../../models/message';
import type { OrganizationDto } from '../../models/organization';
import type { ProcessingJob } from '../../models/processing-job';
import type { Training, TrainingAssignment, TrainingResult } from '../../models/training';
import type { UserDto } from '../../models/user';

/**
 * Shape of public/mock-data/tms-sample-data.json after the loader resolves
 * relative dates. Each collection is the `data` payload of a future endpoint.
 */
export interface MockDataset {
  readonly organizations: readonly OrganizationDto[];
  readonly users: readonly UserDto[];
  readonly categories: readonly Category[];
  readonly trainings: readonly Training[];
  readonly assignments: readonly TrainingAssignment[];
  readonly results: readonly TrainingResult[];
  readonly messages: readonly Message[];
  readonly processingJobs: readonly ProcessingJob[];
}

export const MOCK_DATA_URL = '/mock-data/tms-sample-data.json';

export const EMPTY_MOCK_DATASET: MockDataset = {
  organizations: [],
  users: [],
  categories: [],
  trainings: [],
  assignments: [],
  results: [],
  messages: [],
  processingJobs: [],
};
