import { marketingMatch } from './marketing-match';

describe('marketingMatch', () => {
  const match = (paths: readonly string[]) =>
    marketingMatch(
      {} as never,
      paths.map((path) => ({ path }) as never),
      {} as never,
    );

  it('accepts the public home page', () => {
    expect(match([])).toBe(true);
  });

  it('accepts a public content page', () => {
    expect(match(['pricing'])).toBe(true);
    expect(match(['architecture'])).toBe(true);
  });

  it('leaves signed-in routes to the application shell', () => {
    expect(match(['dashboard'])).toBe(false);
    expect(match(['platform', 'organizations'])).toBe(false);
  });
});
