import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
    selector: 'fd-ui-switch',
    templateUrl: './fd-ui-switch.html',
    styleUrls: ['./fd-ui-switch.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FdUiSwitchComponent {
    // eslint-disable-next-line @angular-eslint/prefer-signal-model -- Switch is controlled so confirmable actions can keep parent state authoritative.
    public readonly checked = input(false);
    public readonly disabled = input(false);
    public readonly ariaLabel = input('');
    public readonly onLabel = input('On');
    public readonly offLabel = input('Off');
    public readonly showStateLabel = input(false);

    public readonly checkedChange = output<boolean>();

    protected toggle(): void {
        if (this.disabled()) {
            return;
        }

        this.checkedChange.emit(!this.checked());
    }
}
