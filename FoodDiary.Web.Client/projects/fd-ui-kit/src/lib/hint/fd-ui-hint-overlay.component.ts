import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, type TemplateRef } from '@angular/core';
import { type SafeHtml } from '@angular/platform-browser';

@Component({
    selector: 'fd-ui-hint-overlay',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './fd-ui-hint-overlay.component.html',
    styleUrls: ['./fd-ui-hint-overlay.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiHintOverlayComponent {
    public readonly tooltipId = input<string | null>(null);
    public readonly contentText = input<string | null>(null);
    public readonly contentHtml = input<SafeHtml | null>(null);
    public readonly contentTemplate = input<TemplateRef<unknown> | null>(null);
    public readonly contentContext = input<Record<string, unknown> | null>(null);
}
