import type { TelegramAuthService } from '../src/app/features/auth/api/telegram-auth.service';
import type { TelegramAuthFacade } from '../src/app/features/auth/lib/telegram-auth.facade';
import type { FastingService } from '../src/app/features/fasting/api/fasting.service';
import {
    cyclicFastingStart,
    extendedFastingHours,
    extendedFastingStart,
    fastingCycleDays,
    fastingDailyWindow,
    type FastingStartIntent,
    intermittentFastingStart,
} from '../src/app/features/fasting/models/fasting-start-intent';
import type { TelegramBackupEmailFlowService } from '../src/app/shared/auth/telegram-backup-email-flow.service';
import { telegramLoginTicket, telegramOAuthCode, telegramOAuthState } from '../src/app/shared/auth/telegram-oidc-values';
import { entityId } from '../src/app/shared/models/semantics/entity-id';

const DAILY_FAST_HOURS = 16;
const EXTENDED_FAST_HOURS = 36;
const OTHER_DAILY_FAST_HOURS = 18;

function semanticContracts(
    fasting: FastingService,
    telegram: TelegramAuthService,
    facade: TelegramAuthFacade,
    backup: TelegramBackupEmailFlowService,
): void {
    const code = telegramOAuthCode('synthetic-code');
    const state = telegramOAuthState('synthetic-state');
    const ticket = telegramLoginTicket('synthetic-ticket');
    telegram.exchange(code, state);
    void facade.exchangeAsync(code, state);
    void backup.handleCallbackAsync(code, state, false);
    telegram.complete(ticket, 'login');
    // @ts-expect-error Authorization code and state have distinct roles.
    telegram.exchange(state, code);
    // @ts-expect-error A login ticket is not an authorization code.
    void facade.exchangeAsync(ticket, state);
    // @ts-expect-error Entity IDs cannot be restamped as OAuth state.
    telegramOAuthState(entityId<'recipe'>('synthetic-recipe'));
    // @ts-expect-error A callback state cannot complete a login intent.
    telegram.complete(state, 'login');
    // @ts-expect-error Backup-email callbacks retain the same OAuth roles.
    void backup.handleCallbackAsync(state, code, false);

    const window = fastingDailyWindow(DAILY_FAST_HOURS);
    const days = fastingCycleDays(2);
    const hours = extendedFastingHours(EXTENDED_FAST_HOURS);
    fasting.start(intermittentFastingStart('Fast16Eat8', window));
    fasting.start(extendedFastingStart('Fast36', hours));
    fasting.start(cyclicFastingStart(days, fastingCycleDays(1), window));
    // @ts-expect-error Extended protocols cannot describe an intermittent plan.
    intermittentFastingStart('Fast36', window);
    // @ts-expect-error Intermittent protocols cannot describe an extended plan.
    extendedFastingStart('Fast16Eat8', hours);
    // @ts-expect-error Cyclic start requires its days and eating-day window.
    fasting.start({ planType: 'Cyclic' });
    // @ts-expect-error Extended duration cannot be used as a cycle-day count.
    cyclicFastingStart(hours, days, window);
    // @ts-expect-error Cycle-day counts cannot be used as fasting hours.
    extendedFastingStart('Fast36', days);
    // @ts-expect-error Role-specific numeric constructors cannot silently retag days as hours.
    extendedFastingHours(days);
    // @ts-expect-error Daily windows can only be made by the complementary-interval factory.
    const invalidWindow: typeof window = { fastHours: window.fastHours, eatingWindowHours: window.eatingWindowHours };
    void invalidWindow;
    // eslint-disable-next-line @typescript-eslint/no-misused-spread -- compile-only negative case deliberately loses the private invariant-bearing state
    const mixedDailyWindow = { ...window, eatingWindowHours: fastingDailyWindow(OTHER_DAILY_FAST_HOURS).eatingWindowHours };
    // @ts-expect-error Spreading windows cannot bypass the complementary-interval invariant.
    cyclicFastingStart(days, days, mixedDailyWindow);
    const mixed = { planType: 'Extended' as const, protocol: 'Fast36' as const, durationHours: hours, fastDays: days };
    // @ts-expect-error Mixed modes are rejected even when passed through a variable.
    const intent: FastingStartIntent = mixed;
    void intent;
    // @ts-expect-error Raw wire bodies are not semantic start intents.
    fasting.start({ planType: 'Intermittent', protocol: 'Fast16Eat8', plannedDurationHours: 16 });
}
void semanticContracts;
