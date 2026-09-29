import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FD_UI_DIALOG_DATA, FdUiButtonComponent, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiDialogRef } from 'fd-ui-kit';

export type RecipePublicationChoice = 'publish' | 'text';
export type RecipePublicationDialogData = { names: readonly string[]; canPublish: boolean };

@Component({
    selector: 'fd-recipe-publication-dialog',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [TranslatePipe, FdUiDialogComponent, FdUiDialogFooterDirective, FdUiButtonComponent],
    templateUrl: './recipe-publication-dialog.html',
})
export class RecipePublicationDialogComponent {
    protected readonly data = inject<RecipePublicationDialogData>(FD_UI_DIALOG_DATA);
    protected readonly dialog = inject(FdUiDialogRef<RecipePublicationDialogComponent, RecipePublicationChoice>);
}
