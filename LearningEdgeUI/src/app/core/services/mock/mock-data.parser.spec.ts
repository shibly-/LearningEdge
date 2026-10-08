import { readFileSync } from 'node:fs';
import { MockDataError, parseMockDataset, resolveDate } from './mock-data.parser';

const NOW = new Date('2026-01-15T12:00:00.000Z');

describe('resolveDate', () => {
  it('passes an ISO string through, normalised', () => {
    expect(resolveDate('2026-03-01T08:30:00Z', 'field', NOW)).toBe('2026-03-01T08:30:00.000Z');
  });

  it('resolves a forward offset against the supplied clock', () => {
    expect(resolveDate('+14d', 'field', NOW)).toBe('2026-01-29T12:00:00.000Z');
  });

  it('resolves a backward offset', () => {
    expect(resolveDate('-3d', 'field', NOW)).toBe('2026-01-12T12:00:00.000Z');
  });

  it('treats 0d as now', () => {
    expect(resolveDate('0d', 'field', NOW)).toBe(NOW.toISOString());
  });

  it('names the offending field when the value is unusable', () => {
    expect(() => resolveDate('not-a-date', 'trainings[0].createdAt', NOW)).toThrow(
      /trainings\[0\]\.createdAt/,
    );
  });
});

describe('parseMockDataset', () => {
  it('treats a missing collection as empty rather than failing', () => {
    const dataset = parseMockDataset({ organizations: [] }, NOW);

    expect(dataset.organizations).toEqual([]);
    expect(dataset.users).toEqual([]);
    expect(dataset.processingJobs).toEqual([]);
  });

  it('rejects a collection that is not an array', () => {
    expect(() => parseMockDataset({ users: {} }, NOW)).toThrow(MockDataError);
  });

  it('keeps the numeric role the backend enum uses', () => {
    const dataset = parseMockDataset(
      {
        users: [
          {
            id: 'user-a1',
            firstName: 'Ayesha',
            lastName: 'Rahman',
            email: 'ayesha@example.com',
            role: 3,
            organizationId: 'org-a',
          },
        ],
      },
      NOW,
    );

    expect(dataset.users[0].role).toBe(3);
  });

  it('rejects a role outside the backend enum', () => {
    const invalid = {
      users: [
        {
          id: 'user-a1',
          firstName: 'Ayesha',
          lastName: 'Rahman',
          email: 'ayesha@example.com',
          role: 9,
          organizationId: 'org-a',
        },
      ],
    };

    expect(() => parseMockDataset(invalid, NOW)).toThrow(/role must be a number 1-4/);
  });

  it('resolves relative dates inside nested collections', () => {
    const dataset = parseMockDataset(
      {
        assignments: [
          {
            id: 'asg-1',
            organizationId: 'org-a',
            trainingId: 'trn-a1',
            trainingTitle: 'Forklift Safety',
            traineeId: 'user-a4',
            traineeName: 'Tomas Varga',
            assignedById: 'user-a2',
            assignedAt: '-5d',
            dueAt: '+10d',
            status: 'in-progress',
          },
        ],
      },
      NOW,
    );

    expect(dataset.assignments[0].assignedAt).toBe('2026-01-10T12:00:00.000Z');
    expect(dataset.assignments[0].dueAt).toBe('2026-01-25T12:00:00.000Z');
  });

  it('allows dueAt to be absent', () => {
    const dataset = parseMockDataset(
      {
        assignments: [
          {
            id: 'asg-1',
            organizationId: 'org-a',
            trainingId: 'trn-a1',
            trainingTitle: 'Forklift Safety',
            traineeId: 'user-a4',
            traineeName: 'Tomas Varga',
            assignedById: 'user-a2',
            assignedAt: '0d',
            dueAt: null,
            status: 'assigned',
          },
        ],
      },
      NOW,
    );

    expect(dataset.assignments[0].dueAt).toBeNull();
  });

  it('rejects a training whose isActive is not a boolean', () => {
    const invalid = {
      trainings: [
        {
          id: 'trn-a1',
          organizationId: 'org-a',
          categoryId: 'cat-a1',
          name: 'Forklift Safety',
          description: '',
          isActive: 'yes',
        },
      ],
    };

    expect(() => parseMockDataset(invalid, NOW)).toThrow(/isActive/);
  });
});

/**
 * Guards the shipped sample file itself: a typo there would otherwise only show
 * up as empty screens at runtime.
 */
describe('public/mock-data/tms-sample-data.json', () => {
  const raw: unknown = JSON.parse(readFileSync('public/mock-data/tms-sample-data.json', 'utf8'));
  const dataset = parseMockDataset(raw, NOW);

  it('populates every collection', () => {
    for (const [name, rows] of Object.entries(dataset)) {
      expect(rows.length, `${name} should have sample rows`).toBeGreaterThan(0);
    }
  });

  it('scopes every tenant-owned record to a known organization', () => {
    const orgIds = new Set(dataset.organizations.map((org) => org.id));
    const scoped = [
      ...dataset.users,
      ...dataset.categories,
      ...dataset.trainings,
      ...dataset.assignments,
      ...dataset.results,
      ...dataset.messages,
      ...dataset.processingJobs,
    ];

    for (const row of scoped) {
      expect(orgIds.has(row.organizationId), `${row.id} points at an unknown organization`).toBe(
        true,
      );
    }
  });

  it('points every assignment at a training and a user in the same tenant', () => {
    const trainings = new Map(dataset.trainings.map((training) => [training.id, training]));
    const users = new Map(dataset.users.map((user) => [user.id, user]));

    for (const assignment of dataset.assignments) {
      expect(trainings.get(assignment.trainingId)?.organizationId).toBe(assignment.organizationId);
      expect(users.get(assignment.traineeId)?.organizationId).toBe(assignment.organizationId);
    }
  });

  it('keeps every training inside a category of its own tenant', () => {
    const categories = new Map(dataset.categories.map((category) => [category.id, category]));

    for (const training of dataset.trainings) {
      expect(categories.get(training.categoryId)?.organizationId).toBe(training.organizationId);
    }
  });

  it('agrees with itself on whether each result is a pass', () => {
    for (const result of dataset.results) {
      expect(result.passed, `${result.id}`).toBe(result.score >= result.passMark);
    }
  });
});
