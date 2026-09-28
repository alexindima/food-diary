export type RecipeLanguage = 'en' | 'ru';

export function normalizeRecipeLanguage(value: string | null | undefined): RecipeLanguage {
    return value?.toLowerCase().startsWith('ru') === true ? 'ru' : 'en';
}

type RecipeText = {
    name: string;
    description?: string | null;
    steps: ReadonlyArray<{ instruction?: string; description?: string; title?: string | null }>;
};

export function recipeLanguageText(recipe: RecipeText): string {
    return [recipe.name, recipe.description, ...recipe.steps.flatMap(step => [step.title, step.instruction ?? step.description])]
        .filter(value => value !== null && value !== undefined)
        .join(' ')
        .toLowerCase();
}

const MIN_LETTERS = 80;
const MIN_WORDS = 12;
const SCRIPT_CONFIDENCE = 0.9;
const MIN_DISTINCT_HINTS = 4;
const MIN_HINT_RATIO = 0.15;
const UNCHANGED_WORD_RATIO = 0.8;
const HINTS = {
    en: new Set(
        'the and with into until then add cook heat stir mix place pour bake serve cut chop remove let for in of to a on is it minutes salt oil water pan oven'.split(
            ' ',
        ),
    ),
    ru: new Set(
        'и в на с до затем добавьте смешайте нарежьте разогрейте готовьте обжарьте перемешайте выпекайте выложите подавайте оставьте снимите залейте минут соль масло воду воды сковороду духовку нужно чтобы после'.split(
            ' ',
        ),
    ),
};

function words(text: string): string[] {
    return text.toLowerCase().match(/\p{L}+/gu) ?? [];
}

function detectScriptLanguage(letters: string): RecipeLanguage | null {
    const latin = (letters.match(/[a-z]/gu) ?? []).length;
    const cyrillic = (letters.match(/[а-яё]/gu) ?? []).length;
    return latin / letters.length >= SCRIPT_CONFIDENCE ? 'en' : cyrillic / letters.length >= SCRIPT_CONFIDENCE ? 'ru' : null;
}

/** Abstains on short, mixed-script or lexically ambiguous text; never sends recipe content off-device. */
export function detectRecipeLanguage(text: string): RecipeLanguage | null {
    const tokens = words(text);
    const letters = tokens.join('');
    if (letters.length < MIN_LETTERS || tokens.length < MIN_WORDS || /[іїєґў]/u.test(letters)) {
        return null;
    }
    const candidate = detectScriptLanguage(letters);
    if (candidate === null) {
        return null;
    }
    const hints = tokens.filter(word => HINTS[candidate].has(word));
    return new Set(hints).size >= MIN_DISTINCT_HINTS && hints.length / tokens.length >= MIN_HINT_RATIO ? candidate : null;
}

export function hasSubstantiallyChangedRecipeText(previous: string, current: string): boolean {
    const oldWords = words(previous);
    const newWords = words(current);
    const remaining = new Map<string, number>();
    for (const word of oldWords) {
        remaining.set(word, (remaining.get(word) ?? 0) + 1);
    }
    let shared = 0;
    for (const word of newWords) {
        const count = remaining.get(word) ?? 0;
        if (count > 0) {
            shared++;
            remaining.set(word, count - 1);
        }
    }
    const total = Math.max(oldWords.length, newWords.length);
    return total > 0 && shared / total < UNCHANGED_WORD_RATIO;
}
