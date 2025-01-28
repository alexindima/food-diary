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

    constructor() {}

    ngOnInit(): void {
        // Регистрируем дроп-зону в сервисе
        this.dragDropService.registerDropZone(this);
    }

    ngOnDestroy(): void {
        // Удаляем зону при уничтожении компонента
        this.dragDropService.unregisterDropZone(this);
    }

    /**
     * Начинает процесс перетаскивания.
     */
    public startDragging(draggedElement: ElementRef, placeholder: HTMLElement): void {
        this.dragElement = draggedElement;
        this.placeholder = placeholder;
    }

    /**
     * Обновляет позицию `placeholder` внутри зоны.
     */
    public update(pageX: number, pageY: number): void {
        if (!this.placeholder) return;

        const children = Array.from(this.elementRef.nativeElement.children).filter(
            (child) => child !== this.placeholder
        );

        const closest = children.reduce((closestChild, child) => {
            const rect = (child as HTMLElement).getBoundingClientRect();
            const offset = Math.abs(pageY - rect.top);
            // @ts-ignore
            return offset < closestChild.offset
                ? { element: child, offset }
                : closestChild;
        }, { element: null, offset: Infinity });

        // @ts-ignore
        if (closest.element) {
            this.renderer.insertBefore(
                this.elementRef.nativeElement,
                this.placeholder,
            // @ts-ignore
                closest.element
            );
        } else {
            this.renderer.appendChild(this.elementRef.nativeElement, this.placeholder);
        }
    }

    /**
     * Завершает процесс перетаскивания: устанавливает элемент на место `placeholder`.
     */
    public stopDragging(): void {
        console.log("stopDragging", this.elementRef.nativeElement);

        if (this.dragElement && this.placeholder) {
            // Проверяем, находится ли placeholder внутри elementRef.nativeElement
            const isPlaceholderChild = this.elementRef.nativeElement.contains(this.placeholder);

            console.log("Placeholder is child:", isPlaceholderChild, this.placeholder);

            if (isPlaceholderChild) {
                console.log("isPlaceholderChild", this.placeholder);
                console.log("dragElement:", this.dragElement);
                console.log("dragElement instanceof Node:", this.dragElement instanceof Node);
                console.log("placeholder:", this.placeholder);
                try {
                    this.renderer.insertBefore(
                        this.elementRef.nativeElement,
                        this.dragElement.nativeElement,
                        this.placeholder
                    );
                    console.log("after insert", this.placeholder);
                } catch (error) {
                    console.error("Error during insertBefore:", error);
                    return; // Прекращаем выполнение, чтобы не вызвать дополнительные ошибки
                }
                console.log("after insert", this.placeholder);
                console.log("Placeholder parent before remove:", this.placeholder?.parentNode);

                this.renderer.removeChild(this.elementRef.nativeElement, this.placeholder);
                console.log("Placeholder removed");
                this.placeholder = null;
            } else {
                console.warn("Placeholder is not a child of the current drop zone");
            }
        }
    }

    /**
     * Возвращает элемент дроп-зоны.
     */
    public get element(): HTMLElement {
        return this.elementRef.nativeElement;
    }
}
