import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NgClass, NgStyle } from '@angular/common';

@Component({
    selector: 'fd-dynamic-progress-bar',
    imports: [
        NgStyle,
        NgClass
    ],
    templateUrl: './dynamic-progress-bar.component.html',
    styleUrls: ['./dynamic-progress-bar.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DynamicProgressBarComponent {
    public current = input.required<number>();
    public max = input.required<number>();
    public unit = input<string>('');

    public progress = computed<number>(() => Math.round((this.current() / this.max()) * 100));
    public progressBarWidth = computed<string>(() => Math.min(this.progress(), 100) + '%');
    public maxPosition = computed(() => this.current() > this.max()
        ? (this.max() / this.current()) * 100
        : 100
    );
    public textPosition = computed<string>(() => {
        let position;
        if (this.progress() < 50) {
            position = 100 - ((100 - this.progress()) / 2);
        } else if (this.progress() > 200) {
            position = 100 - ((100 - (this.max() / this.current()) * 100 ) / 2) ;
        } else if (this.progress() > 100) {
            position = this.maxPosition() / 2;
        } else {
            position = Math.min(this.progress() / 2, 50);
        }

        return position + '%';
    })
    public barColor = computed(() => {
        if (this.progress() <= 100) {
            const greenIntensity = Math.round((this.progress() / 100) * 100);
            return `rgb(80, ${150 + greenIntensity}, 80)`;
        } else if (this.progress() > 100 && this.progress() <= 125) {
            const orangeIntensity = Math.round(((this.progress() - 100) / 25) * 100);
            return `rgb(255, ${200 - orangeIntensity}, 80)`;
        } else {
            const redIntensity = Math.min(255, Math.round(((this.progress() - 125) / 50) * 255));
            return `rgb(255, ${100 - redIntensity}, ${80 - redIntensity / 2})`;
        }
    })
    public textColorClass = computed(() =>
        this.progress() < 100 * 0.5 ? 'text-black' : 'text-white'
    );
}
