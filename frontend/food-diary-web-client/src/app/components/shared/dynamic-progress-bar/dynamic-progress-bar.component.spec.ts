import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DynamicProgressBarComponent } from './dynamic-progress-bar.component';

describe('CaloriesBarComponent', () => {
  let component: DynamicProgressBarComponent;
  let fixture: ComponentFixture<DynamicProgressBarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DynamicProgressBarComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DynamicProgressBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
