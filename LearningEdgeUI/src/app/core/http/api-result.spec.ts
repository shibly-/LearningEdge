import { ApiFailure, isApiResult, unwrap, unwrapOptional } from './api-result';

describe('ApiResult envelope', () => {
  it('unwraps the payload on success', () => {
    expect(unwrap<{ id: string }>({ success: true, error: '', data: { id: 'x' } })).toEqual({
      id: 'x',
    });
  });

  // A missing record returns HTTP 200 with success:false, never a 404.
  it('throws an envelope failure carrying the server message when success is false', () => {
    try {
      unwrap({ success: false, error: 'Organization name is required.', data: null });
      expect.unreachable('unwrap should have thrown');
    } catch (error) {
      expect(error).toBeInstanceOf(ApiFailure);
      expect((error as ApiFailure).kind).toBe('envelope');
      expect((error as ApiFailure).message).toBe('Organization name is required.');
    }
  });

  it('falls back to a generic message when the server sends an empty error', () => {
    expect(() => unwrap({ success: false, error: '', data: null })).toThrowError(
      'The request was rejected by the server.',
    );
  });

  it('treats a successful envelope with null data as not-found', () => {
    try {
      unwrap({ success: true, error: '', data: null });
      expect.unreachable('unwrap should have thrown');
    } catch (error) {
      expect((error as ApiFailure).kind).toBe('not-found');
    }
  });

  it('rejects a response that is not an envelope at all', () => {
    try {
      unwrap({ id: 'bare-dto' });
      expect.unreachable('unwrap should have thrown');
    } catch (error) {
      expect((error as ApiFailure).kind).toBe('malformed');
    }
  });

  describe('unwrapOptional', () => {
    it('returns null instead of throwing when the record is absent', () => {
      expect(unwrapOptional({ success: false, error: 'Not found', data: null })).toBeNull();
      expect(unwrapOptional({ success: true, error: '', data: null })).toBeNull();
    });

    it('still returns the payload when present', () => {
      expect(unwrapOptional<number>({ success: true, error: '', data: 7 })).toBe(7);
    });

    it('still rejects a malformed response', () => {
      expect(() => unwrapOptional({ nope: true })).toThrowError(ApiFailure);
    });
  });

  describe('isApiResult', () => {
    it('accepts a well-formed envelope', () => {
      expect(isApiResult({ success: true, error: '', data: null })).toBe(true);
    });

    it('rejects null, primitives, and bare DTOs', () => {
      expect(isApiResult(null)).toBe(false);
      expect(isApiResult('nope')).toBe(false);
      expect(isApiResult({ id: 'x' })).toBe(false);
    });
  });
});
