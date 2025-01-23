import { Directive, ElementRef, Input, Renderer2 } from '@angular/core';
import { DropZoneService } from "../services/drop-zone.service";

@Directive({
  selector: '[fdDropZone]'
})
export class DropZoneDirective {
    @Input() dropZoneData!: any[]; // Массив данных для сортировки

    private placeholder: HTMLElement | null = null;

    constructor(private el: ElementRef, private renderer: Renderer2, private dropZoneService: DropZoneService) {
        this.dropZoneService.dragStart$.subscribe((draggedData) => {
            // Создаём placeholder при старте перетаскивания
            this.placeholder = this.createPlaceholder();
        });

        this.dropZoneService.dragUpdate$.subscribe(({ clientX, clientY }) => {
            const elements = Array.from(this.el.nativeElement.children) as HTMLElement[];
            const draggedElement = this.dropZoneService.getDraggedElement();

            elements.forEach((child: HTMLElement) => {
                const rect = child.getBoundingClientRect();
                if (clientY > rect.top && clientY < rect.bottom && child !== draggedElement) {
                    this.el.nativeElement.insertBefore(this.placeholder, child);
                }
            });
        });

        this.dropZoneService.dragEnd$.subscribe(() => {
            // Завершаем перетаскивание и вставляем элемент
            const draggedElement = this.dropZoneService.getDraggedElement();
            if (this.placeholder && draggedElement) {
                this.el.nativeElement.insertBefore(draggedElement, this.placeholder);
                this.placeholder.remove();
                this.placeholder = null;
            }
        });
    }

    private createPlaceholder(): HTMLElement {
        const placeholder = this.renderer.createElement('div');
        this.renderer.setStyle(placeholder, 'height', '50px');
        this.renderer.setStyle(placeholder, 'background', 'rgba(100, 181, 246, 0.1)');
        this.renderer.setStyle(placeholder, 'border', '2px dashed #64b5f6');
        return placeholder;
    }

}
