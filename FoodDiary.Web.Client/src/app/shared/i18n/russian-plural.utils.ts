export type RussianPluralCategory = 'one' | 'few' | 'many';

const HUNDRED = 100;
const TEN = 10;
const ONE = 1;
const TWO = 2;
const FOUR = 4;
const ELEVEN = 11;
const TWELVE = 12;
const FOURTEEN = 14;

export function resolveRussianPluralCategory(count: number): RussianPluralCategory {
    const absoluteCount = Math.abs(count);
    const modulo100 = absoluteCount % HUNDRED;
    const modulo10 = absoluteCount % TEN;

    if (modulo10 === ONE && modulo100 !== ELEVEN) {
        return 'one';
    }

    if (modulo10 >= TWO && modulo10 <= FOUR && (modulo100 < TWELVE || modulo100 > FOURTEEN)) {
        return 'few';
    }

    return 'many';
}
