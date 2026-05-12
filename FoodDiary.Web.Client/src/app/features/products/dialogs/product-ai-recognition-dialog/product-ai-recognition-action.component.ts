import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

@Component({
    selector: 'fd-product-ai-recognition-action',
    imports: [TranslatePipe, FdUiButtonComponent],
    templateUrl: './product-ai-recognition-action.component.html',
    styleUrl: './product-ai-recognition-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProductAiRecognitionActionComponent {
    public readonly hasAnalyzed = input.required<boolean>();
    public readonly disabled = input.required<boolean>();

    public readonly analyze = output();
    public readonly reanalyze = output();
}
