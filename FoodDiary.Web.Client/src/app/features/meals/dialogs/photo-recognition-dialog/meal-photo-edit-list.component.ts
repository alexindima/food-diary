import { type CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import type { EditableAiItem, PhotoAiEditItemDrop, PhotoAiEditItemUpdate, UnitOptionView } from './meal-photo-recognition-dialog.types';

@Component({
    selector: 'fd-meal-photo-edit-list',
    imports: [DragDropModule, TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent],
    templateUrl: './meal-photo-edit-list.component.html',
    styleUrl: './meal-photo-recognition-dialog.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class MealPhotoEditListComponent {
    public readonly items = input.required<EditableAiItem[]>();
    public readonly unitOptions = input.required<readonly UnitOptionView[]>();

    public readonly itemDropped = output<PhotoAiEditItemDrop>();
    public readonly itemUpdated = output<PhotoAiEditItemUpdate>();
    public readonly itemRemoved = output<number>();
    public readonly itemAdded = output<void>();

    protected onDrop(event: CdkDragDrop<EditableAiItem[]>): void {
        this.itemDropped.emit({
            previousIndex: event.previousIndex,
            currentIndex: event.currentIndex,
        });
    }

    protected updateItem(index: number, field: PhotoAiEditItemUpdate['field'], value: string): void {
        this.itemUpdated.emit({ index, field, value });
    }
}
