import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecipeContainerComponent } from './recipe-container.component';

describe('RecipeContainerComponent', () => {
  let component: RecipeContainerComponent;
  let fixture: ComponentFixture<RecipeContainerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecipeContainerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RecipeContainerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
