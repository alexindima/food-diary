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
    public title = input.required<string>()
    public showButton = input<boolean>(false);
    public isAccordion = input<boolean>(false);
    public forceCollapse = input<boolean>();

    public isOpen = signal<boolean>(true);
    public titleLeftOffset = computed<string>(() => {
        return this.isAccordion() ? '36px' : '15px';
    })

    public buttonClick = output<void>();

    public onButtonClick(): void {
        this.buttonClick.emit();
    }

    public toggleAccordion(): void {
        this.isOpen.set(!this.isOpen());
    }
}
