import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { NgStyle } from '@angular/common';

export type SkeletonVariant = 'text' | 'circle' | 'rect';

@Component({
    selector: 'fd-skeleton',
    standalone: true,
    imports: [NgStyle],
    templateUrl: './skeleton.component.html',
    styleUrl: './skeleton.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonComponent {
    public readonly width = input<string>('100%');
    public readonly height = input<string>('16px');
    public readonly borderRadius = input<string>('4px');
    public readonly variant = input<SkeletonVariant>('text');

    public readonly styles = computed(() => {
        const v = this.variant();

        switch (v) {
            case 'circle':
                return {
                    width: this.width() !== '100%' ? this.width() : this.height(),
                    height: this.height(),
                    'border-radius': '50%',
                };
            case 'rect':
                return {
                    width: this.width(),
                    height: this.height() !== '16px' ? this.height() : '120px',
                    'border-radius': this.borderRadius() !== '4px' ? this.borderRadius() : '8px',
                };
            default:
                return {
                    width: this.width(),
                    height: this.height(),
                    'border-radius': this.borderRadius(),
                };
        }
    });
}
