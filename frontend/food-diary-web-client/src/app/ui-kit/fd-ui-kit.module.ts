import { NgModule } from '@angular/core';
import { FdUiInputComponent } from './input/fd-ui-input.component';
import { FdUiCardComponent } from './card/fd-ui-card.component';
import { FdUiRadioGroupComponent } from './radio/fd-ui-radio-group.component';
import { FdUiSelectComponent } from './select/fd-ui-select.component';
import { FdUiTextareaComponent } from './textarea/fd-ui-textarea.component';
import { FdUiEntityCardComponent } from './entity-card/fd-ui-entity-card.component';
import { FdUiEntityCardHeaderDirective } from './entity-card/fd-ui-entity-card-header.directive';

const FD_UI_COMPONENTS = [
    FdUiInputComponent,
    FdUiCardComponent,
    FdUiRadioGroupComponent,
    FdUiSelectComponent,
    FdUiTextareaComponent,
    FdUiEntityCardComponent,
    FdUiEntityCardHeaderDirective,
];

@NgModule({
    imports: FD_UI_COMPONENTS,
    exports: FD_UI_COMPONENTS,
})
export class FdUiKitModule {}
