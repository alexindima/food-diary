import type { TelegramDiaryGoals, TelegramDiaryPreview } from '../../auth/telegram-diary.data';
import type { TelegramDiaryTransferHttpResponse } from './generated/model/telegram-diary-transfer-http-response';
import { requireSdkFields, sdkNullableFields } from './sdk-response';

const goalFields = ['calories', 'protein', 'fat', 'carbs', 'fiber', 'water', 'steps', 'weight', 'waist'] as const satisfies ReadonlyArray<
    keyof TelegramDiaryGoals
>;

export function telegramDiaryPreviewFromSdk(response: TelegramDiaryTransferHttpResponse): TelegramDiaryPreview {
    const preview = requireSdkFields(response, ['counts', 'emailGoals', 'telegramGoals', 'canUseTelegramGoals']);
    return {
        ...preview,
        counts: requireSdkFields(preview.counts, ['meals', 'recipes', 'products', 'waterEntries', 'measurements']),
        emailGoals: sdkNullableFields(preview.emailGoals, goalFields),
        telegramGoals: sdkNullableFields(preview.telegramGoals, goalFields),
    };
}
