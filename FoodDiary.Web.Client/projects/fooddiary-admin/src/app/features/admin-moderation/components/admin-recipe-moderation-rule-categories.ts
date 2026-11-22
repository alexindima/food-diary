import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCheckboxComponent } from 'fd-ui-kit/checkbox/fd-ui-checkbox';

export type AdminRecipeModerationCategory = { value: string; label: string };

@Component({
    selector: 'fd-admin-recipe-moderation-rule-categories',
    imports: [FdUiCheckboxComponent, TranslatePipe],
    templateUrl: './admin-recipe-moderation-rule-categories.html',
    styleUrl: './admin-recipe-moderation-rule-categories.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminRecipeModerationRuleCategoriesComponent {
    public readonly legend = input.required<string>();
    public readonly options = input.required<readonly AdminRecipeModerationCategory[]>();
    public readonly selected = input.required<readonly string[]>();
    public readonly disabled = input(false);
    public readonly categoryChange = output<{ category: string; selected: boolean }>();
}
