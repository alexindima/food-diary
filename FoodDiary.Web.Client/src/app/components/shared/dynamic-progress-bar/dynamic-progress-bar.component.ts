import { NgStyle } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { PERCENT_MULTIPLIER as PERCENT_MAX } from '../../../shared/lib/nutrition.constants';

const HALF_PERCENT = 50;
const DOUBLE_PERCENT = 200;
const WARNING_PROGRESS_MAX = 125;
const WARNING_PROGRESS_RANGE = 25;
const DANGER_PROGRESS_RANGE = 50;
const TEXT_DARK_THRESHOLD_FACTOR = 0.5;
const COLOR_CHANNEL_MIN = 0;
const COLOR_CHANNEL_MAX = 255;
const COLOR_HEX_RADIX = 16;
const COLOR_HEX_PAD_LENGTH = 2;
const GREEN_BASE_CHANNEL = 80;
const GREEN_START_CHANNEL = 150;
const ORANGE_START_CHANNEL = 200;
const RED_START_CHANNEL = 100;

@Component({
    selector: 'fd-dynamic-progress-bar',
    imports: [NgStyle],
    templateUrl: './dynamic-progress-bar.component.html',
    styleUrls: ['./dynamic-progress-bar.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DynamicProgressBarComponent {
    public readonly current = input.required<number>();
    public readonly max = input.required<number>();
    public readonly unit = input<string>('');

    public readonly progress = computed<number>(() => {
        const maxValue = this.max();
        if (maxValue <= 0) {
            return 0;
        }
        return Math.round((this.current() / maxValue) * PERCENT_MAX);
    });

    public readonly progressBarWidth = computed<string>(() => `${Math.min(this.progress(), PERCENT_MAX)}%`);

    public readonly maxPosition = computed(() => {
        const maxValue = this.max();
        const currentValue = this.current();
        if (maxValue <= 0 || currentValue <= 0) {
            return PERCENT_MAX;
        }
        return currentValue > maxValue ? (maxValue / currentValue) * PERCENT_MAX : PERCENT_MAX;
    });
    public readonly maxPositionOffset = computed<string>(() => `${this.maxPosition()}%`);

    public readonly textPosition = computed<string>(() => {
        let position;
        if (this.max() <= 0) {
            position = HALF_PERCENT;
        } else if (this.progress() < HALF_PERCENT) {
            position = PERCENT_MAX - (PERCENT_MAX - this.progress()) / 2;
        } else if (this.progress() > DOUBLE_PERCENT) {
            position = PERCENT_MAX - (PERCENT_MAX - (this.max() / this.current()) * PERCENT_MAX) / 2;
        } else if (this.progress() > PERCENT_MAX) {
            position = this.maxPosition() / 2;
        } else {
            position = Math.min(this.progress() / 2, HALF_PERCENT);
        }

        return `${position}%`;
    });
    public readonly barColor = computed(() => {
        if (this.progress() <= PERCENT_MAX) {
            const greenIntensity = Math.round((this.progress() / PERCENT_MAX) * PERCENT_MAX);
            return this.toHex([GREEN_BASE_CHANNEL, GREEN_START_CHANNEL + greenIntensity, GREEN_BASE_CHANNEL]);
        }
        if (this.progress() > PERCENT_MAX && this.progress() <= WARNING_PROGRESS_MAX) {
            const orangeIntensity = Math.round(((this.progress() - PERCENT_MAX) / WARNING_PROGRESS_RANGE) * PERCENT_MAX);
            return this.toHex([COLOR_CHANNEL_MAX, ORANGE_START_CHANNEL - orangeIntensity, GREEN_BASE_CHANNEL]);
        }

        const redIntensity = Math.min(
            COLOR_CHANNEL_MAX,
            Math.round(((this.progress() - WARNING_PROGRESS_MAX) / DANGER_PROGRESS_RANGE) * COLOR_CHANNEL_MAX),
        );
        return this.toHex([COLOR_CHANNEL_MAX, RED_START_CHANNEL - redIntensity, GREEN_BASE_CHANNEL - redIntensity / 2]);
    });
    public readonly textColorClass = computed(() =>
        this.progress() < PERCENT_MAX * TEXT_DARK_THRESHOLD_FACTOR ? 'text-black' : 'text-white',
    );

    private toHex(channels: number[]): string {
        return `#${channels
            .map(channel =>
                Math.max(COLOR_CHANNEL_MIN, Math.min(COLOR_CHANNEL_MAX, Math.round(channel)))
                    .toString(COLOR_HEX_RADIX)
                    .padStart(COLOR_HEX_PAD_LENGTH, '0'),
            )
            .join('')}`;
    }
}
