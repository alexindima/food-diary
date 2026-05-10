import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

@Component({
    selector: 'fd-ui-switch',
    standalone: true,
    templateUrl: './fd-ui-switch.component.html',
    styleUrls: ['./fd-ui-switch.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSwitchComponent {
    public readonly checked = model(false);
    public readonly disabled = input(false);
    public readonly ariaLabel = input('');
    public readonly onLabel = input('On');
    public readonly offLabel = input('Off');
    public readonly showStateLabel = input(false);

    protected toggle(): void {
        if (this.disabled()) {
            return;
        }

        this.checked.update(checked => !checked);
    }
}
