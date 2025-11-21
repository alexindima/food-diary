import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type FdUiAccentSide = 'top' | 'right' | 'bottom' | 'left';

@Component({
    selector: 'fd-ui-accent-surface, [fdUiAccentSurface]',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-accent-surface.component.html',
    styleUrls: ['./fd-ui-accent-surface.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        class: 'fd-ui-accent-surface',
        '[style.--fd-accent-color]': 'accentColor()',
        '[class.fd-ui-accent-surface--active]': 'active()',
        '[class.fd-ui-accent-surface--tinted]': 'tinted()',
        '[class.fd-ui-accent-surface--side-top]': 'accentSide() === "top"',
        '[class.fd-ui-accent-surface--side-right]': 'accentSide() === "right"',
        '[class.fd-ui-accent-surface--side-bottom]': 'accentSide() === "bottom"',
        '[class.fd-ui-accent-surface--side-left]': 'accentSide() === "left"',
    },
})
export class FdUiAccentSurfaceComponent {
    public readonly accentSide = input<FdUiAccentSide>('top');
    public readonly accentColor = input<string>('#2563eb');
    public readonly active = input(false);
    public readonly tinted = input(false);
}
