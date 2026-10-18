import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'fd-admin-acquisition-link-preview',
    imports: [TranslatePipe],
    templateUrl: './admin-acquisition-link-preview.html',
    styleUrl: './admin-acquisition-link-preview.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAcquisitionLinkPreviewComponent {
    public readonly url = input.required<string>();
    public readonly validation = input.required<{ key: string; max: number } | null>();
}
