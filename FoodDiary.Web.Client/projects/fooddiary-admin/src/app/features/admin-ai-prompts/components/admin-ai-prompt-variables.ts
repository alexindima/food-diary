import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
    selector: 'fd-admin-ai-prompt-variables',
    imports: [TranslatePipe],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './admin-ai-prompt-variables.html',
})
export class AdminAiPromptVariablesComponent {
    public readonly variables = input.required<string[]>();
}
