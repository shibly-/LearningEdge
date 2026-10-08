import { failureKindForStatus, readProblemMessage } from './api-result';

describe('failureKindForStatus', () => {
  it.each([
    [0, 'offline'],
    [400, 'validation'],
    [403, 'forbidden'],
    [404, 'not-found'],
    [409, 'conflict'],
    [503, 'rate-limited'],
    [500, 'server'],
    [502, 'server'],
    [401, 'client'],
    [422, 'client'],
  ] as const)('maps %i to %s', (status, kind) => {
    expect(failureKindForStatus(status)).toBe(kind);
  });
});

describe('readProblemMessage', () => {
  it('prefers the ProblemDetails detail', () => {
    expect(
      readProblemMessage({
        title: 'Conflict',
        detail: 'An organization named Acme already exists.',
      }),
    ).toBe('An organization named Acme already exists.');
  });

  it('joins the messages of a validation errors dictionary', () => {
    expect(
      readProblemMessage({
        title: 'One or more validation errors occurred.',
        errors: { Name: ['Name is required.'], Email: ['Email is invalid.'] },
      }),
    ).toBe('Name is required. Email is invalid.');
  });

  it('falls back to the title', () => {
    expect(readProblemMessage({ title: 'Not Found' })).toBe('Not Found');
  });

  it('accepts a plain-text body', () => {
    expect(readProblemMessage('  Too many requests  ')).toBe('Too many requests');
  });

  it('returns null when nothing useful is present', () => {
    expect(readProblemMessage(null)).toBeNull();
    expect(readProblemMessage('')).toBeNull();
    expect(readProblemMessage({ errors: {} })).toBeNull();
  });
});
