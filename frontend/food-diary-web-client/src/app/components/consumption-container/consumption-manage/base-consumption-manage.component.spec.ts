import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BaseConsumptionManageComponent } from './base-consumption-manage.component';

describe('BaseFoodManageComponent', () => {
    let component: BaseConsumptionManageComponent;
    let fixture: ComponentFixture<BaseConsumptionManageComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [BaseConsumptionManageComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(BaseConsumptionManageComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
