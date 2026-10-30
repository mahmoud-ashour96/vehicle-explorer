import { Make } from '../vehicle.models';
import { POPULAR_MAKES, searchMakes } from './make-search';

const makes: Make[] = [
  { id: 1, name: 'ACURA' },
  { id: 2, name: 'HONDA' },
  { id: 3, name: 'HONDA MOTORCYCLES' },
  { id: 4, name: 'AMERICAN HONDA' },
  { id: 5, name: 'TOYOTA' },
];

describe('searchMakes', () => {
  it('suggests popular makes in their curated order when the query is empty', () => {
    const result = searchMakes(makes, '  ');

    expect(result.matches.map((m) => m.name)).toEqual(['TOYOTA', 'HONDA']);
    expect(POPULAR_MAKES.indexOf('TOYOTA')).toBeLessThan(POPULAR_MAKES.indexOf('HONDA'));
  });

  it('matches case-insensitively and ranks prefix matches above substring matches', () => {
    const result = searchMakes(makes, 'honda');

    expect(result.matches.map((m) => m.name)).toEqual([
      'HONDA',
      'HONDA MOTORCYCLES',
      'AMERICAN HONDA',
    ]);
  });

  it('caps the suggestions but reports the total number of matches', () => {
    const result = searchMakes(makes, 'a', 2);

    expect(result.matches).toHaveLength(2);
    expect(result.total).toBe(5);
  });

  it('returns nothing when no make matches', () => {
    expect(searchMakes(makes, 'zzz')).toEqual({ matches: [], total: 0 });
  });
});
