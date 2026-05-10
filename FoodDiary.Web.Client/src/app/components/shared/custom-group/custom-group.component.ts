import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
    selector: 'fd-custom-group',
    templateUrl: './custom-group.component.html',
    styleUrl: './custom-group.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomGroupComponent {
    public readonly title = input.required<string>();
}
