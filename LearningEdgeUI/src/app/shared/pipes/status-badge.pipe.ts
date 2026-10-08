export type BadgeTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral';

const TONES: Readonly<Record<string, BadgeTone>> = {
  // Category and training active flag
  on: 'success',
  off: 'neutral',
  // Assignment status
  completed: 'success',
  'in-progress': 'info',
  assigned: 'info',
  overdue: 'danger',
  // Processing status and stages
  complete: 'success',
  active: 'info',
  failed: 'danger',
  uploading: 'info',
  parsing: 'info',
  storing: 'info',
  generating: 'warning',
  // Result outcome
  passed: 'success',
  pass: 'success',
  fail: 'danger',
};

/**
 * Status string to a badge tone. Colour alone never conveys meaning — the badge
 * always renders its label text too (spec 7).
 */
export function badgeToneFor(status: string | null | undefined): BadgeTone {
  if (status === null || status === undefined) {
    return 'neutral';
  }
  return TONES[status.toLowerCase()] ?? 'neutral';
}
