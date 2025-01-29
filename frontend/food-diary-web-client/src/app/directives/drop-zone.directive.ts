import {
    Directive,
    ElementRef,
    Renderer2,
    inject,
    OnDestroy,
    OnInit
} from '@angular/core';
import { DragDropService } from '../services/drag-drop.service';

@Directive({
    selector: '[fdDropZone]',
})
export class DropZoneDirective implements OnInit, OnDestroy {
    public readonly elementRef = inject(ElementRef);
    private readonly renderer = inject(Renderer2);
    private readonly dragDropService = inject(DragDropService);

    private dragElement: ElementRef | null = null;

    private placeholder: HTMLElement | null = null;
    private dragView: HTMLElement | null = null;

    public ngOnInit(): void {
        this.dragDropService.registerDropZone(this);
    }

    /**
     * Начинает процесс перетаскивания.
     */
    public startDragging(draggedElement: ElementRef, view: HTMLElement, placeholder: HTMLElement): void {
        this.dragElement = draggedElement;
        this.dragView = view;
        this.placeholder = placeholder;
    }

    /**
     * Обновляет позицию `placeholder` внутри зоны.
     */
    public update(pageX: number, pageY: number): void {
        if (!this.placeholder || !this.dragView) {
            return;
        }

        const children = Array.from(this.elementRef.nativeElement.children).filter((child) => {
            if (
                !(child instanceof HTMLElement) ||
                child === this.placeholder ||
                child === this.dragView ||
                !child.hasAttribute('fdDraggable')
            ) {
                return false;
            }

            const rect = child.getBoundingClientRect();
            return rect.width > 0 && rect.height > 0; // Проверяем, что размеры элемента не равны 0
        }) as HTMLElement[];

        const dragViewRect = this.dragView.getBoundingClientRect();
        const dragViewCenterY = dragViewRect.top + dragViewRect.height / 2; // Центр перетаскиваемого элемента

        let closestBefore: HTMLElement | null = null;
        let closestAfter: HTMLElement | null = null;
        let closestBeforeDistance = Infinity;
        let closestAfterDistance = Infinity;

        children.forEach((child) => {
            const rect = child.getBoundingClientRect();
            const childCenterY = rect.top + rect.height / 2;

            if (childCenterY <= dragViewCenterY) {
                const distance = dragViewCenterY - childCenterY;
                if (distance < closestBeforeDistance) {
                    closestBefore = child;
                    closestBeforeDistance = distance;
                }
            } else {
                const distance = childCenterY - dragViewCenterY;
                if (distance < closestAfterDistance) {
                    closestAfter = child;
                    closestAfterDistance = distance;
                }
            }
        });

        if (closestBefore && closestAfter) {
            console.log('Insert between closest elements');
            this.renderer.insertBefore(
                this.elementRef.nativeElement,
                this.placeholder,
                closestAfter
            );
        } else if (closestBefore) {
            console.log('Insert after closestBefore');
            this.renderer.appendChild(this.elementRef.nativeElement, this.placeholder);
        } else if (closestAfter) {
            console.log('Insert before closestAfter');
            this.renderer.insertBefore(
                this.elementRef.nativeElement,
                this.placeholder,
                closestAfter
            );
        } else {
            console.log('Insert at the end');
            this.renderer.appendChild(this.elementRef.nativeElement, this.placeholder);
        }
    }

    public stopDragging(): void {
        //console.log("stopDragging", this.elementRef.nativeElement);

        if (this.dragElement && this.placeholder) {
            // Проверяем, находится ли placeholder внутри elementRef.nativeElement
            const isPlaceholderChild = this.elementRef.nativeElement.contains(this.placeholder);

            //console.log("Placeholder is child:", isPlaceholderChild, this.placeholder);

            if (isPlaceholderChild) {
                //console.log("isPlaceholderChild", this.placeholder);
                //console.log("dragElement:", this.dragElement);
                //console.log("dragElement instanceof Node:", this.dragElement instanceof Node);
                //console.log("placeholder:", this.placeholder);
                try {
                    this.renderer.insertBefore(
                        this.elementRef.nativeElement,
                        this.dragElement.nativeElement,
                        this.placeholder
                    );
                    //console.log("after insert", this.placeholder);
                } catch (error) {
                    //console.error("Error during insertBefore:", error);
                    return; // Прекращаем выполнение, чтобы не вызвать дополнительные ошибки
                }
                //console.log("after insert", this.placeholder);
                //console.log("Placeholder parent before remove:", this.placeholder?.parentNode);

                this.renderer.removeChild(this.elementRef.nativeElement, this.placeholder);
                //console.log("Placeholder removed");
                this.placeholder = null;
            } else {
                //console.warn("Placeholder is not a child of the current drop zone");
            }
        }
    }

    public ngOnDestroy(): void {
        this.dragDropService.unregisterDropZone(this);
    }
}

interface ClosestElement {
    element: HTMLElement | null;
    offset: number;
    childCenterY?: number; // Добавляем свойство для хранения центра элемента
}
