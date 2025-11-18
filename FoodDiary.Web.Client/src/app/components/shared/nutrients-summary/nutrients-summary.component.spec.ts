import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NutrientsSummaryComponent } from './nutrients-summary.component';

describe('NutrientsSummaryComponent', () => {
  let component: NutrientsSummaryComponent;
  let fixture: ComponentFixture<NutrientsSummaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NutrientsSummaryComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NutrientsSummaryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
