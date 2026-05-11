import { type CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent } from 'fd-ui-kit';

import type { AiEditItemDrop, AiEditItemUpdate, AiEditUnitOption, EditableAiItem } from './ai-photo-result.types';

@Component({
    selector: 'fd-ai-photo-edit-list',
    imports: [DragDropModule, TranslatePipe, FdUiButtonComponent, FdUiHintDirective, FdUiIconComponent],
    templateUrl: './ai-photo-edit-list.component.html',
    styleUrl: './ai-photo-result.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: {
        style: 'display: contents',
    },
})
export class AiPhotoEditListComponent {
    private readonly translateService = inject(TranslateService);

    public readonly items = input.required<EditableAiItem[]>();
    public readonly unitOptions = input.required<AiEditUnitOption[]>();

    public readonly itemDropped = output<AiEditItemDrop>();
    public readonly itemUpdated = output<AiEditItemUpdate>();
    public readonly itemRemoved = output<number>();
    public readonly itemAdded = output<void>();

    protected onDrop(event: CdkDragDrop<EditableAiItem[]>): void {
        this.itemDropped.emit({
            previousIndex: event.previousIndex,
            currentIndex: event.currentIndex,
        });
    }

    protected updateItem(index: number, field: AiEditItemUpdate['field'], value: string): void {
        this.itemUpdated.emit({ index, field, value });
    }

    protected removeItemAriaLabel(item: EditableAiItem): string {
        const name = item.name.trim().length > 0 ? item.name.trim() : item.nameEn.trim().length > 0 ? item.nameEn.trim() : '?';

        return this.translateService.instant('AI_INPUT_BAR.REMOVE_AI_ITEM', { name });
    }
}
