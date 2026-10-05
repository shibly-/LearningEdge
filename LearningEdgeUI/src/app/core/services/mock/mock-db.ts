import { Injectable } from '@angular/core';
import type { Category } from '../../models/category';
import type { Message } from '../../models/message';
import type { OrganizationDto } from '../../models/organization';
import type { ProcessingJob } from '../../models/processing-job';
import type { Training, TrainingAssignment, TrainingResult } from '../../models/training';
import type { UserDto } from '../../models/user';
import { EMPTY_MOCK_DATASET, type MockDataset } from './mock-data.model';

/**
 * MOCK: in-memory store behind the Mock* repositories (spec 3.7).
 *
 * Seed data lives in public/mock-data/tms-sample-data.json and is loaded at
 * bootstrap by MockDataLoader. Nothing is hardcoded here, so editing the JSON
 * is all it takes to change what every page shows.
 */
@Injectable({ providedIn: 'root' })
export class MockDb {
  organizations: readonly OrganizationDto[] = EMPTY_MOCK_DATASET.organizations;
  users: readonly UserDto[] = EMPTY_MOCK_DATASET.users;
  categories: readonly Category[] = EMPTY_MOCK_DATASET.categories;
  trainings: readonly Training[] = EMPTY_MOCK_DATASET.trainings;
  assignments: readonly TrainingAssignment[] = EMPTY_MOCK_DATASET.assignments;
  results: readonly TrainingResult[] = EMPTY_MOCK_DATASET.results;
  messages: readonly Message[] = EMPTY_MOCK_DATASET.messages;
  processingJobs: readonly ProcessingJob[] = EMPTY_MOCK_DATASET.processingJobs;

  private seeded = false;
  private sequence = 1000;

  get isSeeded(): boolean {
    return this.seeded;
  }

  seed(dataset: MockDataset): void {
    this.organizations = dataset.organizations;
    this.users = dataset.users;
    this.categories = dataset.categories;
    this.trainings = dataset.trainings;
    this.assignments = dataset.assignments;
    this.results = dataset.results;
    this.messages = dataset.messages;
    this.processingJobs = dataset.processingJobs;
    this.seeded = true;
  }

  snapshot(): MockDataset {
    return {
      organizations: this.organizations,
      users: this.users,
      categories: this.categories,
      trainings: this.trainings,
      assignments: this.assignments,
      results: this.results,
      messages: this.messages,
      processingJobs: this.processingJobs,
    };
  }

  nextId(prefix: string): string {
    this.sequence += 1;
    return `${prefix}-${this.sequence}`;
  }

  /** GUID-shaped id, matching what the real API returns from a POST. */
  nextGuid(): string {
    this.sequence += 1;
    const hex = this.sequence.toString(16).padStart(12, '0');
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4000-8000-${hex.padEnd(12, '0').slice(0, 12)}`;
  }
}
