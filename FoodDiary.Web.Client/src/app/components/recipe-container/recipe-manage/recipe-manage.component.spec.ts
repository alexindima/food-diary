import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecipeManageComponent } from './recipe-manage.component';

describe('RecipeManageComponent', () => {
  let component: RecipeManageComponent;
  let fixture: ComponentFixture<RecipeManageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecipeManageComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RecipeManageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
