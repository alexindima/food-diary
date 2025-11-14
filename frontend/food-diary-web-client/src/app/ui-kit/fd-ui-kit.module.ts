import { NgModule } from '@angular/core';
import { FdUiInputComponent } from './input/fd-ui-input.component';

const FD_UI_COMPONENTS = [FdUiInputComponent];

@NgModule({
    imports: FD_UI_COMPONENTS,
    exports: FD_UI_COMPONENTS,
})
export class FdUiKitModule {}

