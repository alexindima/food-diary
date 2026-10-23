import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { FdUiButtonComponent, FdUiCheckboxComponent } from 'fd-ui-kit';

@Component({
    selector: 'fd-recipe-video-input',
    imports: [TranslatePipe, FdUiButtonComponent, FdUiCheckboxComponent],
    templateUrl: './recipe-video-input.html',
    styleUrl: './recipe-video-input.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecipeVideoInputComponent {
    public readonly enabled = input(false);
    public readonly busy = input(false);
    public readonly selectedFile = input<File | null>(null);
    public readonly enabledChanged = output<boolean>();
    public readonly fileSelected = output<File | null>();

    protected selectFile(fileInput: HTMLInputElement): void {
        if (this.busy()) {
            return;
        }
        const file = fileInput.files?.item(0) ?? null;
        this.fileSelected.emit(file);
    }
}
