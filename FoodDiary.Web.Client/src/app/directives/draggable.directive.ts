import {
    AfterViewInit,
    Directive,
    ElementRef,
    inject,
    input,
    OnInit,
    output,
    Renderer2,
    TemplateRef,
    ViewContainerRef
} from '@angular/core';
import { DragDropService } from '../services/drag-drop.service';
import { DropZoneDirective } from './drop-zone.directive';

/**
 * A directive that enables draggable behavior on an element.
 */
@Directive({
  selector: '[fdDraggable]'
})
export class DraggableDirective implements OnInit, AfterViewInit {
    private readonly elementRef = inject(ElementRef);
    private readonly viewContainerRef = inject(ViewContainerRef);
    private readonly renderer = inject(Renderer2);
    private readonly dragDropService = inject(DragDropService);

    private readonly onMouseMoveHandler;
    private readonly onMouseUpHandler;

    /**
     * Custom template for the placeholder element.
     */
    public fdDraggablePlaceholder = input<TemplateRef<any> | null>(null);

    /**
     * Custom template for the drag view element.
     */
    public fdDraggableDragView = input<TemplateRef<any> | null>(null);

    /**
     * Defines the axis along which the element can be dragged.
     * Allowed values: 'X', 'Y', 'XY'.
     */
    public fdDraggableAxis = input<FdDraggableAxis>('XY');

    /**
     * Element that defines the boundary within which the element can be dragged.
     */
    public fdDraggableBoundary = input<HTMLElement | null>(null);

    /**
     * Event emitted when dragging starts.
     */
    public fdDragStarted = output<void>();

    /**
     * Event emitted when the draggable element is dropped.
     */
    public fdDrop = output<DragDropEvent>();

    private dragView: HTMLElement | null = null;
    private dropZone: DropZoneDirective | null = null;
    private originalParent: HTMLElement | null = null;
    private originalIndex: number = 0;
    private animationFrameId: number | null = null;
    private isDragging = false;
    private offsetX = 0;
    private offsetY = 0;
    private lastMouseX = 0;
    private lastMouseY = 0;

    public constructor() {
        this.onMouseMoveHandler = this.onMouseMove.bind(this);
        this.onMouseUpHandler = this.onMouseUp.bind(this);
    }

    public ngOnInit(): void {
        this.dropZone = this.dragDropService.findDropZone(this.elementRef);
    }

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
        this.fdDragStarted.emit();

        this.originalParent = this.elementRef.nativeElement.parentElement;
        if (this.originalParent) {
            const children = Array.from(this.originalParent.children);
            this.originalIndex = children.indexOf(this.elementRef.nativeElement);
        }

        const rect = this.elementRef.nativeElement.getBoundingClientRect();
        const initialLeft = rect.left + window.scrollX;
        const initialTop = rect.top + window.scrollY;

        this.offsetX = event.clientX - rect.left;
        this.offsetY = event.clientY - rect.top;

        const initialWidth = rect.width;
        const initialHeight = rect.height;

        const placeholder = this.createPlaceholder(initialWidth, initialHeight);
        const dragView = this.createView(initialWidth, initialHeight, initialLeft, initialTop);

        this.dropZone?.startDragging(this.elementRef, dragView, placeholder)
        this.moveAt(event.pageX, event.pageY);

        document.addEventListener('mousemove', this.onMouseMoveHandler);
        document.addEventListener('mouseup', this.onMouseUpHandler);
    }

    private onMouseMove(event: MouseEvent): void {
        this.lastMouseX = event.pageX;
        this.lastMouseY = event.pageY;

        if (!this.animationFrameId) {
            this.animationFrameId = requestAnimationFrame(() => {
                this.moveAt(this.lastMouseX, this.lastMouseY);
                this.animationFrameId = null;
            });
        }
    };

    private onMouseUp(): void {
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

        const placeholder = this.originalParent?.querySelector<HTMLElement>('.drag-placeholder');
        if (!this.originalParent || !placeholder) {
            return;
        }

        const newIndex = Array.from(this.originalParent.children).indexOf(placeholder);
        if (newIndex !== this.originalIndex) {
            console.log('emit', this.originalIndex, newIndex);
            this.fdDrop.emit({
                previousIndex: this.originalIndex,
                currentIndex: newIndex }
            );
        }

        this.dropZone?.stopDragging();
        this.originalIndex = 0;

        document.removeEventListener('mousemove', this.onMouseMoveHandler);
        document.removeEventListener('mouseup', this.onMouseUpHandler);
    };

    private moveAt(pageX: number, pageY: number): void {
        const target = this.dragView || this.elementRef.nativeElement;

        let newLeft = pageX - this.offsetX;
        let newTop = pageY - this.offsetY;

        const boundary = this.fdDraggableBoundary();
        if (boundary) {
            const boundaryRect = boundary.getBoundingClientRect();
            newLeft = Math.max(boundaryRect.left + window.scrollX, Math.min(newLeft, boundaryRect.right + window.scrollX - target.clientWidth));
            newTop = Math.max(boundaryRect.top + window.scrollY, Math.min(newTop, boundaryRect.bottom + window.scrollY - target.clientHeight));
        }

        if (this.fdDraggableAxis() === 'X' || this.fdDraggableAxis() === 'XY') {
            this.renderer.setStyle(target, 'left', `${newLeft}px`);
        }
        if (this.fdDraggableAxis() === 'Y' || this.fdDraggableAxis() === 'XY') {
            this.renderer.setStyle(target, 'top', `${newTop}px`);
        }

        this.dropZone?.update();
    }

    private createPlaceholder(width: number, height: number): HTMLElement {
        const wrapper = document.createElement('div');
        this.renderer.addClass(wrapper, 'drag-placeholder');
        this.renderer.setStyle(wrapper, 'width', `${width}px`);

        const customPlaceholder = this.fdDraggablePlaceholder();
        if (customPlaceholder) {
            const view = this.viewContainerRef.createEmbeddedView(customPlaceholder);

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

        const customDragView = this.fdDraggableDragView();
        if (customDragView) {
            const view = this.viewContainerRef.createEmbeddedView(customDragView);
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
}

export type FdDraggableAxis = 'X' | 'Y' | 'XY';

export interface DragDropEvent {
    previousIndex: number;
    currentIndex: number;
}
