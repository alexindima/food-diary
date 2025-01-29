import {
    AfterViewInit,
    Directive,
    ElementRef,
    inject,
    Input,
    Renderer2,
    TemplateRef,
    ViewContainerRef
} from '@angular/core';
import { DragDropService } from '../services/drag-drop.service';
import { DropZoneDirective } from './drop-zone.directive';

@Directive({
  selector: '[fdDraggable]'
})
export class DraggableDirective implements AfterViewInit {
    private readonly elementRef = inject(ElementRef);
    private readonly viewContainerRef = inject(ViewContainerRef);
    private readonly renderer = inject(Renderer2);
    private readonly dragDropService = inject(DragDropService);

    @Input() public fdDraggablePlaceholder?: TemplateRef<any>;
    @Input() public fdDraggableDragView?: TemplateRef<any>;
    @Input() public fdDraggableAxis: FdDraggableAxis = 'XY';
    @Input() public fdDraggableBoundary: HTMLElement | null = null;

    private isDragging = false;
    private offsetX = 0;
    private offsetY = 0;
    private animationFrameId: number | null = null;
    private lastMouseX = 0;
    private lastMouseY = 0;

    private originalParent?: HTMLElement;
    private dragView: HTMLElement | null = null;

    public ngAfterViewInit(): void {
        const dragHandleElement = this.elementRef.nativeElement.querySelector('[fdDragHandle]')
            ?? this.elementRef.nativeElement;

        this.initDragHandle(dragHandleElement);
    }

    private initDragHandle(handleElement: HTMLElement): void {
        this.renderer.setStyle(handleElement, 'cursor', 'move');
        this.renderer.listen(handleElement, 'mousedown', this.onMouseDown.bind(this));
    }

    private onMouseDown(event: MouseEvent): void {
        this.isDragging = true;

        const dropZone = this.findDropZone();
        if (!dropZone) {
            console.error('Drop zone not found!');
            return;
        }

        this.originalParent = this.elementRef.nativeElement.parentNode;

        const rect = this.elementRef.nativeElement.getBoundingClientRect();
        const initialLeft = rect.left + window.scrollX;
        const initialTop = rect.top + window.scrollY;

        this.offsetX = event.clientX - rect.left;
        this.offsetY = event.clientY - rect.top;

        const initialWidth = rect.width;
        const initialHeight = rect.height;

        this.dragDropService.setActiveDropZone(dropZone);
        const placeholder = this.createPlaceholder(initialWidth, initialHeight);
        const dragView = this.createView(initialWidth, initialHeight, initialLeft, initialTop);

        dropZone.startDragging(this.elementRef, dragView, placeholder)

        document.addEventListener('mousemove', this.onMouseMove);
        document.addEventListener('mouseup', this.onMouseUp);

        this.moveAt(event.pageX, event.pageY);
    }

    private onMouseMove = (event: MouseEvent): void => {
        this.lastMouseX = event.pageX;
        this.lastMouseY = event.pageY;

        if (!this.animationFrameId) {
            this.animationFrameId = requestAnimationFrame(() => {
                this.moveAt(this.lastMouseX, this.lastMouseY);
                this.animationFrameId = null;
            });
        }
    };

    private onMouseUp = (): void => {
        if (!this.isDragging) {
            return;
        }

        this.isDragging = false;

        if (this.dragView) {
            this.renderer.removeChild(document.body, this.dragView);
            this.dragView = null;
        }

        if (this.originalParent) {
            this.renderer.appendChild(this.originalParent, this.elementRef.nativeElement);
        }

        this.renderer.removeStyle(this.elementRef.nativeElement, 'display');
        this.renderer.setStyle(this.elementRef.nativeElement, 'position', '');
        this.renderer.setStyle(this.elementRef.nativeElement, 'z-index', '');
        this.renderer.setStyle(this.elementRef.nativeElement, 'left', '');
        this.renderer.setStyle(this.elementRef.nativeElement, 'top', '');

        this.dragDropService.getActiveDropZone()?.stopDragging();

        document.removeEventListener('mousemove', this.onMouseMove);
        document.removeEventListener('mouseup', this.onMouseUp);
    };

    private moveAt(pageX: number, pageY: number): void {
        const target = this.dragView || this.elementRef.nativeElement;

        const targetRect = target.getBoundingClientRect();

        let newLeft = pageX - this.offsetX;
        let newTop = pageY - this.offsetY;

        if (this.fdDraggableBoundary) {
            const boundaryRect = this.fdDraggableBoundary.getBoundingClientRect();

            const boundaryLeft = boundaryRect.left + window.scrollX;
            const boundaryTop = boundaryRect.top + window.scrollY;
            const boundaryRight = boundaryRect.right + window.scrollX;
            const boundaryBottom = boundaryRect.bottom + window.scrollY;

            if (this.fdDraggableAxis === 'X' || this.fdDraggableAxis === 'XY') {
                newLeft = Math.max(boundaryLeft, Math.min(newLeft, boundaryRight - targetRect.width));
            }

            if (this.fdDraggableAxis === 'Y' || this.fdDraggableAxis === 'XY') {
                newTop = Math.max(boundaryTop, Math.min(newTop, boundaryBottom - targetRect.height));
            }
        }

        if (this.fdDraggableAxis === 'X' || this.fdDraggableAxis === 'XY') {
            this.renderer.setStyle(target, 'left', `${newLeft}px`);
        }
        if (this.fdDraggableAxis === 'Y' || this.fdDraggableAxis === 'XY') {
            this.renderer.setStyle(target, 'top', `${newTop}px`);
        }

        this.dragDropService.getActiveDropZone()?.update(pageX, pageY);
    }

    private createPlaceholder(width: number, height: number): HTMLElement {
        const wrapper = document.createElement('div');
        this.renderer.addClass(wrapper, 'drag-placeholder');
        this.renderer.setStyle(wrapper, 'width', `${width}px`);

        if (this.fdDraggablePlaceholder) {
            const view = this.viewContainerRef.createEmbeddedView(this.fdDraggablePlaceholder);

            view.rootNodes.forEach((node) => {
                if (node instanceof HTMLElement) {
                    wrapper.appendChild(node);
                }
            });
        } else {
            this.renderer.setStyle(wrapper, 'height', `${height}px`);
            this.renderer.setStyle(wrapper, 'background', 'rgba(200, 200, 200, 0.3)');
            this.renderer.setStyle(wrapper, 'border', '2px dashed #ccc');
            this.renderer.setStyle(wrapper, 'borderRadius', '4px');
            this.renderer.setStyle(wrapper, 'boxSizing', 'border-box');
        }

        return wrapper;
    }

    private createView(width: number, height: number, left: number, top: number): HTMLElement {
        const wrapper = this.renderer.createElement('div');
        this.renderer.addClass(wrapper, 'drag-view');
        this.renderer.setStyle(wrapper, 'cursor', 'move');
        this.renderer.setStyle(wrapper, 'position', 'absolute');
        this.renderer.setStyle(wrapper, 'width', `${width}px`);
        this.renderer.setStyle(wrapper, 'left', `${left}px`);
        this.renderer.setStyle(wrapper, 'top', `${top}px`);
        this.renderer.setStyle(wrapper, 'zIndex', '1000');

        if (this.fdDraggableDragView) {
            const view = this.viewContainerRef.createEmbeddedView(this.fdDraggableDragView);
            view.rootNodes.forEach((node) => {
                if (node instanceof HTMLElement) {
                    this.renderer.appendChild(wrapper, node);
                }
            });

            this.renderer.setStyle(this.elementRef.nativeElement, 'display', 'none');
        } else {
            this.renderer.setStyle(wrapper, 'height', `${height}px`);
            this.renderer.appendChild(wrapper, this.elementRef.nativeElement);
        }

        this.renderer.appendChild(document.body, wrapper);
        this.dragView = wrapper;

        return wrapper;
    }

    private findDropZone(): DropZoneDirective | null {
        const dropZoneElement = this.elementRef.nativeElement.closest('[fdDropZone]');

        if (dropZoneElement) {
            // Найти зарегистрированную дроп-зону по элементу
            const dropZone = this.dragDropService.getDropZones().find(
                (zone) => zone.elementRef.nativeElement === dropZoneElement
            );

            if (dropZone) {
                return dropZone;
            }
        }

        return null; // Если дроп-зона не найдена
    }

}

export type FdDraggableAxis = 'X' | 'Y' | 'XY';
