import { NgStyle } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { FdUiButtonComponent } from 'fd-ui-kit/button/fd-ui-button.component';

/**
 * A custom group component that can behave as an accordion or a simple container.
 */
@Component({
    selector: 'fd-custom-group',
    imports: [NgStyle, FdUiButtonComponent],
    templateUrl: './custom-group.component.html',
    styleUrl: './custom-group.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomGroupComponent {
    /**
     * The title of the component.
     * This is a required parameter.
     */
    public readonly title = input.required<string>();
    public readonly collapsedHint = input<string>();

    /**
     * Flag to display the close button.
     * Default is false.
     */
    public readonly showCloseButton = input<boolean>(false);

    /**
     * Flag indicating whether the component should behave as an accordion.
     * When true, a toggle button for collapsing/expanding is displayed.
     */
    public readonly isAccordion = input<boolean>(false);

    /**
     * Flag to force collapse the component.
     */
    public readonly forceCollapse = input<boolean>();

    /**
     * Event emitted when the close button is clicked.
     */
    public closeButtonClick = output<void>();

    public readonly isOpen = signal<boolean>(true);
    public readonly titleLeftOffset = computed<string>(() => {
        return this.isAccordion()
            ? 'calc(var(--fd-size-control-xs) + var(--fd-space-sm))'
            : 'calc(var(--fd-space-sm) + var(--fd-border-width-strong) + var(--fd-border-width))';
    });

    public onCloseButtonClick(): void {
        this.closeButtonClick.emit();
    }

    public toggleAccordion(): void {
        this.isOpen.set(!this.isOpen());
    }
}
