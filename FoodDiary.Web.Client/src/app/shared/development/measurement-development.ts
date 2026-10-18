import { devDay, devNumber, devRecord, devText } from './development-input';
import { connectDevelopmentScreen, DevelopmentToolError } from './development-tools';
const MAX_DRAFT_VALUE = 1_000_000;
const MAX_UNIT_LENGTH = 10;

type MeasurementDevelopmentAdapter = {
    kind: 'weight' | 'waist';
    read: () => { date: string; value: string; unit: string; valid: boolean; loading: boolean; editing: boolean; error: string | null };
    dirty: () => boolean;
    fill: (date: string, value: string) => void;
    clear: () => void;
    close: () => void;
};

export function connectMeasurementDevelopment(adapter: MeasurementDevelopmentAdapter): void {
    const requireNew = (): void => {
        const state = adapter.read();
        if (state.editing || state.loading) {
            throw new DevelopmentToolError('A ready NEW measurement editor is required.');
        }
    };
    connectDevelopmentScreen({
        kind: `${adapter.kind}-editor`,
        read: adapter.read,
        isDirty: adapter.dirty,
        beforeNavigate: () => {
            if (adapter.read().loading) {
                throw new DevelopmentToolError('Measurement save is running.');
            }
            adapter.close();
        },
        draftSchema: {
            type: 'object',
            additionalProperties: false,
            required: ['date', 'value', 'unit'],
            properties: {
                date: { type: 'string', pattern: '^\\d{4}-\\d{2}-\\d{2}$' },
                value: { type: 'number' },
                unit: { type: 'string', description: 'Must exactly match the current displayed unit from screen context. No conversion.' },
            },
        },
        fillDraft: input => {
            requireNew();
            if (adapter.dirty()) {
                throw new DevelopmentToolError('Unsaved measurement draft exists.');
            }
            const draft = devRecord(input, ['date', 'value', 'unit']);
            const date = devDay(draft['date']);
            const value = devNumber(draft['value'], 0, MAX_DRAFT_VALUE);
            if (devText(draft['unit'], MAX_UNIT_LENGTH) !== adapter.read().unit) {
                throw new DevelopmentToolError('Measurement display unit does not match.');
            }
            adapter.fill(date, String(value));
        },
        clearDraft: () => {
            requireNew();
            adapter.clear();
        },
    });
}
