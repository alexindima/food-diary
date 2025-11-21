import { NgModule } from '@angular/core';
import { FdUiInputComponent } from './input/fd-ui-input.component';
import { FdUiCardComponent } from './card/fd-ui-card.component';
import { FdUiRadioGroupComponent } from './radio/fd-ui-radio-group.component';
import { FdUiSelectComponent } from './select/fd-ui-select.component';
import { FdUiTextareaComponent } from './textarea/fd-ui-textarea.component';
import { FdUiEntityCardComponent } from './entity-card/fd-ui-entity-card.component';
import { FdUiEntityCardHeaderDirective } from './entity-card/fd-ui-entity-card-header.directive';
import { FdUiButtonComponent } from './button/fd-ui-button.component';
import { FdUiCheckboxComponent } from './checkbox/fd-ui-checkbox.component';
import { FdUiDateInputComponent } from './date-input/fd-ui-date-input.component';
import { FdUiTabsComponent } from './tabs/fd-ui-tabs.component';
import { FdUiSatietyScaleComponent } from './satiety-scale/fd-ui-satiety-scale.component';
import { FdUiDialogComponent } from './dialog/fd-ui-dialog.component';
import { FdUiDialogFooterDirective } from './dialog/fd-ui-dialog-footer.directive';
import { FdUiConfirmDialogComponent } from './dialog/fd-ui-confirm-dialog.component';
import { FdUiAccentSurfaceComponent } from './accent-surface/fd-ui-accent-surface.component';

const FD_UI_COMPONENTS = [
    FdUiInputComponent,
    FdUiAccentSurfaceComponent,
    FdUiCardComponent,
    FdUiRadioGroupComponent,
    FdUiSelectComponent,
    FdUiTextareaComponent,
    FdUiEntityCardComponent,
    FdUiEntityCardHeaderDirective,
    FdUiButtonComponent,
    FdUiCheckboxComponent,
    FdUiDateInputComponent,
    FdUiTabsComponent,
    FdUiSatietyScaleComponent,
    FdUiDialogComponent,
    FdUiDialogFooterDirective,
    FdUiConfirmDialogComponent,
];

@NgModule({
    imports: FD_UI_COMPONENTS,
    exports: FD_UI_COMPONENTS,
})
export class FdUiKitModule {}
