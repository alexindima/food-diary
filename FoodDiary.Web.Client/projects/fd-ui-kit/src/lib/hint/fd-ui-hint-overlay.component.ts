import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SafeHtml } from '@angular/platform-browser';

@Component({
    selector: 'fd-ui-hint-overlay',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-hint-overlay.component.html',
    styleUrls: ['./fd-ui-hint-overlay.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiHintOverlayComponent {
    public readonly contentText = input<string | null>(null);
    public readonly contentHtml = input<SafeHtml | null>(null);
}
