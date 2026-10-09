import { describe, expect, it } from 'vitest';

import { decodeNotificationKind } from '../../../../shared/models/notification-kind';
import { notificationPresentation } from './notification-presentation';

describe('notificationPresentation compatibility', () => {
    it.each([
        ['DietologistInvitationReceived', 'medical_information', 'DIETOLOGIST_INVITATION'],
        ['NewRecommendation', 'medical_information', 'RECOMMENDATION'],
        ['NewRecommendationComment', 'medical_information', 'RECOMMENDATION'],
        ['NewRecommendationCommentForDietologist', 'medical_information', 'RECOMMENDATION'],
        ['NewClientTask', 'task_alt', 'TASK'],
        ['ClientTaskChangedForDietologist', 'task_alt', 'TASK'],
        ['ClientTaskCancelled', 'task_alt', 'TASK'],
        ['ClientTaskDueSoon', 'task_alt', 'TASK'],
        ['PasswordSetupSuggested', 'password', 'PASSWORD_SETUP'],
    ])('retains the existing accent, icon, badge and action for %s', (code, icon, key) => {
        const presentation = notificationPresentation(decodeNotificationKind(code));
        expect(presentation.hasAccentIcon).toBe(true);
        expect(presentation.icon).toBe(icon);
        expect(presentation.badgeKey).toBe(`NOTIFICATIONS.${key}_BADGE`);
        expect(presentation.actionKey).toBe(`NOTIFICATIONS.${key}_ACTION`);
    });

    it.each(['FutureNotification', 'constructor', 'FastingCheckInReminder', 'DietologistInvitationAccepted'])(
        'retains the generic presentation for %s',
        code => {
            expect(notificationPresentation(decodeNotificationKind(code))).toEqual({
                isPasswordSetupSuggestion: false,
                isDietologistInvitation: false,
                isDietologistRecommendation: false,
                hasAccentIcon: false,
                icon: 'notifications',
                badgeKey: null,
                actionKey: null,
            });
        },
    );
});
