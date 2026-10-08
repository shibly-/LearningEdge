import { TRAINING_FILE_RULES, validateTrainingFiles } from './training';

function file(name: string, size = 10): File {
  const blob = new File(['x'], name);
  Object.defineProperty(blob, 'size', { value: size });
  return blob;
}

describe('validateTrainingFiles', () => {
  it('accepts PDF, DOCX and TXT regardless of extension case', () => {
    expect(validateTrainingFiles([file('a.pdf'), file('b.DOCX'), file('c.txt')])).toBeNull();
  });

  it('requires at least one file', () => {
    expect(validateTrainingFiles([])).toBe('Choose at least one file.');
  });

  it('rejects other file types', () => {
    expect(validateTrainingFiles([file('deck.pptx')])).toMatch(/not a PDF, DOCX or TXT/);
  });

  it('rejects empty files', () => {
    expect(validateTrainingFiles([file('a.pdf', 0)])).toMatch(/is empty/);
  });

  it('enforces the per-file and per-request limits', () => {
    expect(validateTrainingFiles([file('big.pdf', TRAINING_FILE_RULES.maxFileBytes + 1)])).toMatch(
      /larger than 20 MB/,
    );

    const tooMany = Array.from({ length: TRAINING_FILE_RULES.maxFiles + 1 }, (_, i) =>
      file(`f${i}.txt`),
    );
    expect(validateTrainingFiles(tooMany)).toMatch(/at most 10 files/);

    const heavy = Array.from({ length: 6 }, (_, i) =>
      file(`f${i}.pdf`, TRAINING_FILE_RULES.maxFileBytes),
    );
    expect(validateTrainingFiles(heavy)).toMatch(/more than 100 MB/);
  });
});
