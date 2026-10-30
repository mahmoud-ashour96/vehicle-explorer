import { Make } from '../vehicle.models';

/**
 * Suggested before the user types anything. vPIC lists ~12k makes, most of them small trailer and
 * custom-vehicle builders, so the alphabetical top of the list is not a useful starting point.
 */
export const POPULAR_MAKES = [
  'TOYOTA',
  'HONDA',
  'FORD',
  'CHEVROLET',
  'NISSAN',
  'HYUNDAI',
  'KIA',
  'TESLA',
  'BMW',
  'MERCEDES-BENZ',
  'VOLKSWAGEN',
  'AUDI',
  'LEXUS',
  'SUBARU',
  'MAZDA',
  'JEEP',
];

export const MAX_SUGGESTIONS = 50;

export interface MakeSearchResult {
  matches: Make[];
  /** Number of makes that matched before applying the limit. */
  total: number;
}

/** Case-insensitive search: names that start with the query rank above names that only contain it. */
export function searchMakes(
  makes: readonly Make[],
  query: string,
  limit = MAX_SUGGESTIONS,
): MakeSearchResult {
  const q = query.trim().toLowerCase();

  if (!q) {
    const popular = POPULAR_MAKES.map((name) => makes.find((m) => m.name === name)).filter(
      (m): m is Make => m !== undefined,
    );
    return { matches: popular, total: popular.length };
  }

  const startsWith: Make[] = [];
  const contains: Make[] = [];
  for (const make of makes) {
    const name = make.name.toLowerCase();
    if (name.startsWith(q)) {
      startsWith.push(make);
    } else if (name.includes(q)) {
      contains.push(make);
    }
  }

  const all = [...startsWith, ...contains];
  return { matches: all.slice(0, limit), total: all.length };
}
