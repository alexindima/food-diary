import { NgModule } from '@angular/core';
import { FdUiInputComponent } from './input/fd-ui-input.component';
import { FdUiCardComponent } from './card/fd-ui-card.component';
import { FdUiRadioGroupComponent } from './radio/fd-ui-radio-group.component';

const FD_UI_COMPONENTS = [FdUiInputComponent, FdUiCardComponent, FdUiRadioGroupComponent];

@NgModule({
    imports: FD_UI_COMPONENTS,
    exports: FD_UI_COMPONENTS,
})
export class FdUiKitModule {}
