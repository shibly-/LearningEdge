import { endOfLocalDay } from './training-assign.component';

describe('endOfLocalDay', () => {
  it('keeps the chosen calendar day in local time', () => {
    const due = new Date(endOfLocalDay('2026-03-15'));

    expect(due.getFullYear()).toBe(2026);
    expect(due.getMonth()).toBe(2);
    expect(due.getDate()).toBe(15);
    expect(due.getHours()).toBe(23);
  });
});
