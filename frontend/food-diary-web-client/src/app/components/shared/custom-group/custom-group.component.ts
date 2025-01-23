import { Component, input, output } from '@angular/core';
import { DragHandleDirective } from '../../../directives/drag-handle.directive';

@Component({
  selector: 'fd-custom-group',
    imports: [
        DragHandleDirective
    ],
  templateUrl: './custom-group.component.html',
  styleUrl: './custom-group.component.less'
})
export class CustomGroupComponent {
    public title = input.required<string>()
    public showButton = input<boolean>(false);

    public buttonClick = output<void>();

    public onButtonClick(): void {
        this.buttonClick.emit();
    }
}
