export type FastingEmojiScaleOption = {
    value: number;
    emoji: string;
};

export const FASTING_WARNING_THRESHOLD_HOURS = 72;
export const FASTING_HARD_STOP_THRESHOLD_HOURS = 168;
export const FASTING_SESSION_CHECK_INS_PAGE_SIZE = 5;

export const FASTING_HUNGER_EMOJI_SCALE: FastingEmojiScaleOption[] = [
    { value: 1, emoji: '\u{1F62B}' },
    { value: 2, emoji: '\u{1F61F}' },
    { value: 3, emoji: '\u{1F610}' },
    { value: 4, emoji: '\u{1F642}' },
    { value: 5, emoji: '\u{1F60C}' },
];

export const FASTING_ENERGY_EMOJI_SCALE: FastingEmojiScaleOption[] = [
    { value: 1, emoji: '\u{1F634}' },
    { value: 2, emoji: '\u{1F62E}\u200D\u{1F4A8}' },
    { value: 3, emoji: '\u{1F642}' },
    { value: 4, emoji: '\u26A1' },
    { value: 5, emoji: '\u{1F680}' },
];

export const FASTING_MOOD_EMOJI_SCALE: FastingEmojiScaleOption[] = [
    { value: 1, emoji: '\u{1F623}' },
    { value: 2, emoji: '\u{1F615}' },
    { value: 3, emoji: '\u{1F610}' },
    { value: 4, emoji: '\u{1F642}' },
    { value: 5, emoji: '\u{1F604}' },
];
