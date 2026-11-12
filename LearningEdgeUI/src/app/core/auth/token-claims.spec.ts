import { UserRole } from '../models/user-role';
import { readTokenIdentity } from './token-claims';

function token(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    sub: 'kc-5f2a',
    preferred_username: 'mark.price',
    name: 'Mark Price',
    email: 'mark.price@simplelearn.com',
    realm_access: { roles: ['OrgAdmin', 'default-roles-learningedge'] },
    ...overrides,
  };
}

describe('readTokenIdentity', () => {
  it('reads the subject, username, name and email', () => {
    const identity = readTokenIdentity(token());

    expect(identity).toEqual({
      subject: 'kc-5f2a',
      username: 'mark.price',
      displayName: 'Mark Price',
      email: 'mark.price@simplelearn.com',
      role: UserRole.OrgAdmin,
    });
  });

  it('rejects a token with no subject, which cannot identify anyone', () => {
    expect(readTokenIdentity(token({ sub: undefined }))).toBeNull();
    expect(readTokenIdentity(null)).toBeNull();
    expect(readTokenIdentity('not-an-object')).toBeNull();
  });

  it('matches realm roles regardless of case', () => {
    const identity = readTokenIdentity(token({ realm_access: { roles: ['sysadmin'] } }));
    expect(identity?.role).toBe(UserRole.SysAdmin);
  });

  it('takes the highest role when several are assigned', () => {
    const identity = readTokenIdentity(
      token({ realm_access: { roles: ['Learner', 'OrgAdmin', 'Instructor'] } }),
    );
    expect(identity?.role).toBe(UserRole.OrgAdmin);
  });

  it('reports no role when the realm uses names this app does not know', () => {
    expect(
      readTokenIdentity(token({ realm_access: { roles: ['uma_authorization'] } }))?.role,
    ).toBeNull();
    expect(readTokenIdentity(token({ realm_access: undefined }))?.role).toBeNull();
  });

  it('builds a display name from given and family name when name is absent', () => {
    const identity = readTokenIdentity(token({ name: undefined }));
    expect(identity?.displayName).toBe('mark.price');

    const named = readTokenIdentity(
      token({ name: undefined, given_name: 'Mark', family_name: 'Price' }),
    );
    expect(named?.displayName).toBe('Mark Price');
  });

  it('falls back from username to email, then to the subject', () => {
    expect(readTokenIdentity(token({ preferred_username: undefined }))?.username).toBe(
      'mark.price@simplelearn.com',
    );
    expect(
      readTokenIdentity(token({ preferred_username: undefined, email: undefined }))?.username,
    ).toBe('kc-5f2a');
  });
});
