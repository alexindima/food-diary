import { Component, } from '@angular/core';
import { RecipeManageComponent } from '../recipe-manage.component';

@Component({
    selector: 'app-recipe-add',
    templateUrl: './recipe-add.component.html',
    styleUrls: ['./recipe-add.component.scss'],
    imports: [RecipeManageComponent]
})
export class RecipeAddComponent extends RecipeManageComponent {}
