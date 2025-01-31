import { Component, computed, input, output, signal } from '@angular/core';
import { DragHandleDirective } from '../../../directives/drag-handle.directive';
import { NgStyle } from '@angular/common';

/**
 * A custom group component that can behave as an accordion or a simple container.
 */
@Component({
  selector: 'fd-custom-group',
    imports: [
        DragHandleDirective,
        NgStyle
    ],
  templateUrl: './custom-group.component.html',
  styleUrl: './custom-group.component.less'
})
export class CustomGroupComponent {
    /**
     * The title of the component.
     * This is a required parameter.
     */
    public title = input.required<string>();

    /**
     * Flag to display the close button.
     * Default is false.
     */
    public showCloseButton = input<boolean>(false);

    /**
     * Flag indicating whether the component should behave as an accordion.
     * When true, a toggle button for collapsing/expanding is displayed.
     */
    public isAccordion = input<boolean>(false);

    /**
     * Flag to force collapse the component.
     */
    public forceCollapse = input<boolean>();

    /**
     * Event emitted when the close button is clicked.
     */
    public closeButtonClick = output<void>();

    public isOpen = signal<boolean>(true);
    public titleLeftOffset = computed<string>(() => {
        return this.isAccordion() ? '36px' : '15px';
    });

    public onCloseButtonClick(): void {
        this.closeButtonClick.emit();
    }

    public toggleAccordion(): void {
        this.isOpen.set(!this.isOpen());
    }
}
