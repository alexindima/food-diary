import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { type FdUiTab, FdUiTabsComponent } from 'fd-ui-kit/tabs/fd-ui-tabs';

@Component({
    selector: 'fd-admin-moderation-navigation',
    imports: [FdUiTabsComponent, TranslatePipe],
    templateUrl: './admin-moderation-navigation.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminModerationNavigationComponent {
    public readonly active = input.required<'/moderation' | '/moderation/recipes' | '/moderation/rules'>();
    protected readonly tabs: FdUiTab[] = [
        { value: '/moderation', labelKey: 'ADMIN_RECIPE_MODERATION.REPORTS' },
        { value: '/moderation/recipes', labelKey: 'ADMIN_RECIPE_MODERATION.CHECKS' },
        { value: '/moderation/rules', labelKey: 'ADMIN_RECIPE_MODERATION.RULES' },
    ];
}
