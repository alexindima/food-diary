import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import type { AdminAiPromptKey } from '../models/admin-ai-prompt-scenario';

@Component({
    selector: 'fd-admin-ai-prompt-context',
    imports: [TranslatePipe],
    templateUrl: './admin-ai-prompt-context.html',
    styleUrl: './admin-ai-prompt-context.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAiPromptContextComponent {
    public readonly scenarioKey = input.required<AdminAiPromptKey>();
    public readonly responseFormatJson = input.required<string>();
}
