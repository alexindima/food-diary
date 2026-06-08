import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { NutritionMismatchWarning } from './nutrition-editor.types';

@Component({
    selector: 'fd-nutrition-editor-messages',
    imports: [TranslatePipe],
    templateUrl: './nutrition-editor-messages.html',
    styleUrls: ['./nutrition-editor-messages.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NutritionEditorMessagesComponent {
    public readonly macrosError = input<string | null>(null);
    public readonly showManualHint = input(false);
    public readonly manualHintKey = input('');
    public readonly warning = input<NutritionMismatchWarning | null>(null);

    protected readonly hasMacrosError = computed(() => this.hasText(this.macrosError()));
    protected readonly hasManualHint = computed(() => this.showManualHint() && this.manualHintKey().trim().length > 0);

    private hasText(value: string | null): boolean {
        return value !== null && value.trim().length > 0;
    }
}
