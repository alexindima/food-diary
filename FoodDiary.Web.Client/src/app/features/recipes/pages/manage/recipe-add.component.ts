import { Component } from '@angular/core';
import { RecipeManageComponent } from '../../components/manage/recipe-manage.component';

@Component({
    selector: 'app-recipe-add',
    templateUrl: './recipe-add.component.html',
    styleUrls: ['./recipe-add.component.scss'],
    imports: [RecipeManageComponent],
})
export class RecipeAddComponent extends RecipeManageComponent {}
