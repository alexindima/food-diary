import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConsumptionContainerComponent } from './consumption-container.component';

describe('ConsumptionContainerComponent', () => {
    let component: ConsumptionContainerComponent;
    let fixture: ComponentFixture<ConsumptionContainerComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ConsumptionContainerComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(ConsumptionContainerComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
