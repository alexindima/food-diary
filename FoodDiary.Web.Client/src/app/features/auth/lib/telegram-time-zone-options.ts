import type { FdUiSelectOption } from 'fd-ui-kit/select/fd-ui-select';

export function telegramTimeZoneOptions(selected: string, date: Date): Array<FdUiSelectOption<string>> {
    const available = typeof Intl.supportedValuesOf === 'function' ? Intl.supportedValuesOf('timeZone') : [];
    const zones = new Set(['UTC', ...available, selected]);
    return [...zones].sort().flatMap(value => {
        try {
            const offsetSuffix = new Intl.DateTimeFormat('en', { timeZone: value, timeZoneName: 'longOffset' })
                .formatToParts(date)
                .filter(part => part.type === 'timeZoneName')
                .map(part => ` (${part.value.replace('GMT', 'UTC')})`)
                .join('');
            return [{ value, label: `${value.replaceAll('_', ' ')}${offsetSuffix}` }];
        } catch {
            return [];
        }
    });
}
