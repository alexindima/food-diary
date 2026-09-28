import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiDialogRef } from 'fd-ui-kit';

export type RecipeLanguageChoice = 'change' | 'keep';
export type RecipeLanguageDialogData = { detected: string; selected: string };

@Component({
    selector: 'fd-recipe-language-dialog',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './recipe-language-dialog.html',
})
export class RecipeLanguageDialogComponent {
    protected readonly data = inject<RecipeLanguageDialogData>(FD_UI_DIALOG_DATA);
    protected readonly dialog = inject(FdUiDialogRef<RecipeLanguageDialogComponent, RecipeLanguageChoice>);
}
