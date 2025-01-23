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

@Directive({
  selector: '[fdDraggable]'
})
export class DraggableDirective implements AfterViewInit {
    private readonly elementRef = inject(ElementRef);
    private readonly viewContainerRef = inject(ViewContainerRef);
    private readonly renderer = inject(Renderer2);

    @Input() public fdDraggablePlaceholder?: TemplateRef<any>;
    @Input() public fdDraggableDragView?: TemplateRef<any>;

    private isDragging = false;
    private offsetX = 0;
    private offsetY = 0;

    private originalParent?: HTMLElement;
    private dragPlaceholder: HTMLElement | null = null;
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

        this.originalParent = this.elementRef.nativeElement.parentNode;

        const rect = this.elementRef.nativeElement.getBoundingClientRect();
        const initialLeft = rect.left + window.scrollX;
        const initialTop = rect.top + window.scrollY;

        this.offsetX = event.clientX - rect.left;
        this.offsetY = event.clientY - rect.top;

        const initialWidth = rect.width;
        const initialHeight = rect.height;

        this.createPlaceholder(initialWidth, initialHeight);
        this.createView(initialWidth, initialHeight, initialLeft, initialTop);

        document.addEventListener('mousemove', this.onMouseMove);
        document.addEventListener('mouseup', this.onMouseUp);

        this.moveAt(event.pageX, event.pageY);
    }

    private onMouseMove = (event: MouseEvent): void => {
        if (!this.isDragging) {
            return;
        }

        this.moveAt(event.pageX, event.pageY);
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

        if (this.dragPlaceholder && this.originalParent) {
            this.renderer.removeChild(this.originalParent, this.dragPlaceholder);
            this.dragPlaceholder = null;
        }

        if (this.originalParent) {
            this.renderer.appendChild(this.originalParent, this.elementRef.nativeElement);
        }

        this.renderer.removeStyle(this.elementRef.nativeElement, 'display');
        this.renderer.setStyle(this.elementRef.nativeElement, 'position', '');
        this.renderer.setStyle(this.elementRef.nativeElement, 'z-index', '');
        this.renderer.setStyle(this.elementRef.nativeElement, 'left', '');
        this.renderer.setStyle(this.elementRef.nativeElement, 'top', '');

        document.removeEventListener('mousemove', this.onMouseMove);
        document.removeEventListener('mouseup', this.onMouseUp);
    };

    private moveAt(pageX: number, pageY: number): void {
        const target = this.dragView || this.elementRef.nativeElement;
        this.renderer.setStyle(target, 'left', `${pageX - this.offsetX}px`);
        this.renderer.setStyle(target, 'top', `${pageY - this.offsetY}px`);
    }

    private createPlaceholder(width: number, height: number): void {
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

        this.renderer.insertBefore(
            this.originalParent,
            wrapper,
            this.elementRef.nativeElement
        );
        this.dragPlaceholder = wrapper;
    }

    private createView(width: number, height: number, left: number, top: number): void {
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
    }
}
