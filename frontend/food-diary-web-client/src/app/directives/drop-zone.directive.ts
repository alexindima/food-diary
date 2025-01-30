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

    private element: ElementRef | null = null;
    private view: HTMLElement | null = null;
    private placeholder: HTMLElement | null = null;

    public ngOnInit(): void {
        this.dragDropService.registerDropZone(this);
    }

    public startDragging(
        draggedElement: ElementRef,
        view: HTMLElement,
        placeholder: HTMLElement
    ): void {
        this.element = draggedElement;
        this.view = view;
        this.placeholder = placeholder;

        this.update();
    }

    public update(): void {
        if (!this.placeholder || !this.view) {
            return;
        }

        const children = Array.from<HTMLElement>(this.elementRef.nativeElement.children).filter((child) => {
            if (
                !(child instanceof HTMLElement) ||
                child === this.placeholder ||
                child === this.view ||
                !child.hasAttribute('fdDraggable')
            ) {
                return false;
            }

            const rect = child.getBoundingClientRect();
            return rect.width > 0 && rect.height > 0;
        });
        const dragViewRect = this.view.getBoundingClientRect();
        const dragViewCenterY = dragViewRect.top + dragViewRect.height / 2;

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
            this.renderer.insertBefore(
                this.elementRef.nativeElement,
                this.placeholder,
                closestAfter
            );
        } else if (closestBefore) {
            this.renderer.appendChild(this.elementRef.nativeElement, this.placeholder);
        } else if (closestAfter) {
            this.renderer.insertBefore(
                this.elementRef.nativeElement,
                this.placeholder,
                closestAfter
            );
        } else {
            this.renderer.appendChild(this.elementRef.nativeElement, this.placeholder);
        }
    }

    public stopDragging(): void {
        if (this.element && this.placeholder) {
            const isPlaceholderChild = this.elementRef.nativeElement.contains(this.placeholder);

            if (isPlaceholderChild) {
                this.renderer.insertBefore(
                    this.elementRef.nativeElement,
                    this.element.nativeElement,
                    this.placeholder
                );

                this.renderer.removeChild(this.elementRef.nativeElement, this.placeholder);
                this.placeholder = null;
            }
        }
    }

    public ngOnDestroy(): void {
        this.dragDropService.unregisterDropZone(this);
    }
}
