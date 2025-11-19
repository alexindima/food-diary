import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RecipeManageComponent } from '../recipe-manage.component';

@Component({
    selector: 'fd-recipe-edit',
    templateUrl: './recipe-edit.component.html',
    styleUrls: ['./recipe-edit.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [RecipeManageComponent],
})
export class RecipeEditComponent extends RecipeManageComponent {}
