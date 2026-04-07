import { Component } from '@angular/core';
import { RecipeManageComponent } from '../../components/manage/recipe-manage.component';
import { RecipeManageFacade } from '../../lib/recipe-manage.facade';

@Component({
    selector: 'app-recipe-add',
    templateUrl: './recipe-add.component.html',
    styleUrls: ['./recipe-add.component.scss'],
    imports: [RecipeManageComponent],
    providers: [RecipeManageFacade],
})
export class RecipeAddComponent extends RecipeManageComponent {}
