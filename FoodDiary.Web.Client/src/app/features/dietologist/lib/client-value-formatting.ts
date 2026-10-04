import { resolveAppLocale } from '../../../shared/lib/locale.constants';
import { centimetersToImperialHeight, type MeasurementSystem } from '../../../shared/measurements/measurement-system.service';

type ClientValueUnit = 'kcal' | 'g' | 'ml' | 'kg' | 'cm' | 'lb' | 'ft' | 'in' | 'h';
const DECIMAL_BASE = 10;

export type ClientValueFormatting = {
    number: (value: number, unit?: ClientValueUnit, maximumFractionDigits?: number) => string;
    macros: (protein: number, fat: number, carbs: number) => string;
};

function formatPlainNumber(value: number, unit?: ClientValueUnit, maximumFractionDigits = 0): string {
    const precision = DECIMAL_BASE ** maximumFractionDigits;
    return `${Math.round(value * precision) / precision}${unit === undefined ? '' : ` ${unit}`}`;
}

export const DEFAULT_CLIENT_VALUE_FORMATTING: ClientValueFormatting = {
    number: formatPlainNumber,
    macros: (protein, fat, carbs) =>
        `P ${formatPlainNumber(protein, 'g')} / F ${formatPlainNumber(fat, 'g')} / C ${formatPlainNumber(carbs, 'g')}`,
};

export function createClientValueFormatting(language: string, translate: (key: string) => string): ClientValueFormatting {
    const numberFormats = new Map<number, Intl.NumberFormat>();
    const number: ClientValueFormatting['number'] = (value, unit, maximumFractionDigits = 0) => {
        let numberFormat = numberFormats.get(maximumFractionDigits);
        if (numberFormat === undefined) {
            numberFormat = new Intl.NumberFormat(resolveAppLocale(language), { maximumFractionDigits });
            numberFormats.set(maximumFractionDigits, numberFormat);
        }

        const formatted = numberFormat.format(value).replace('-', '−');
        return unit === undefined ? formatted : `${formatted} ${translate(`GENERAL.UNITS.${unit.toUpperCase()}`)}`;
    };

    return {
        number,
        macros: (protein, fat, carbs) =>
            [
                `${translate('GENERAL.NUTRIENTS.PROTEIN')} ${number(protein, 'g')}`,
                `${translate('GENERAL.NUTRIENTS.FAT')} ${number(fat, 'g')}`,
                `${translate('GENERAL.NUTRIENTS.CARB')} ${number(carbs, 'g')}`,
            ].join(' / '),
    };
}

export function formatClientHeight(
    heightCm: number | null | undefined,
    system: MeasurementSystem,
    formatting: ClientValueFormatting = DEFAULT_CLIENT_VALUE_FORMATTING,
): string | null {
    if (heightCm === null || heightCm === undefined) {
        return null;
    }

    if (system === 'metric') {
        return formatting.number(heightCm, 'cm', 1);
    }

    const height = centimetersToImperialHeight(heightCm);
    return `${formatting.number(height.feet, 'ft')} ${formatting.number(height.inches, 'in')}`;
}
