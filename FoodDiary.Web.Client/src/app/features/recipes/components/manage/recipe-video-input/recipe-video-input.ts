import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiCheckboxComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-recipe-video-input',
    imports: [TranslatePipe, FdUiCheckboxComponent],
    templateUrl: './recipe-video-input.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeVideoInputComponent {
    public readonly enabled = input(false);
    public readonly busy = input(false);
    public readonly enabledChanged = output<boolean>();
    public readonly fileSelected = output<File | null>();

    protected selectFile(fileInput: HTMLInputElement): void {
        this.fileSelected.emit(fileInput.files?.item(0) ?? null);
    }
}
