import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FD_UI_DIALOG_DATA, FdUiDialogRef } from 'fd-ui-kit/material';
import { ConsumptionDetailComponent } from './consumption-detail.component';

describe('ConsumptionDetailComponent', () => {
    let component: ConsumptionDetailComponent;
    let fixture: ComponentFixture<ConsumptionDetailComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [ConsumptionDetailComponent],
            providers: [
                {
                    provide: FD_UI_DIALOG_DATA,
                    useValue: {
                        id: 1,
                        date: new Date().toISOString(),
                        items: [],
                    },
                },
                {
                    provide: FdUiDialogRef,
                    useValue: jasmine.createSpyObj('FdUiDialogRef', ['close']),
                },
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(ConsumptionDetailComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
