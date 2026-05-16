import { NgStyle } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type SkeletonVariant = 'text' | 'circle' | 'rect';

@Component({
    selector: 'fd-skeleton',
    imports: [NgStyle],
    templateUrl: './skeleton.component.html',
    styleUrl: './skeleton.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonComponent {
    private readonly defaultTextHeight = 'var(--fd-text-body-size)';
    private readonly defaultRadius = 'var(--fd-radius-xs)';
    private readonly defaultRectHeight = 'calc((var(--fd-size-control-xl) * 2) + var(--fd-space-xs))';
    private readonly defaultRectRadius = 'var(--fd-radius-md)';

    public readonly width = input<string>('100%');
    public readonly height = input<string>(this.defaultTextHeight);
    public readonly borderRadius = input<string>(this.defaultRadius);
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
                    height: this.height() !== this.defaultTextHeight ? this.height() : this.defaultRectHeight,
                    'border-radius': this.borderRadius() !== this.defaultRadius ? this.borderRadius() : this.defaultRectRadius,
                };
            case 'text':
                return {
                    width: this.width(),
                    height: this.height(),
                    'border-radius': this.borderRadius(),
                };
        }
    });
}
