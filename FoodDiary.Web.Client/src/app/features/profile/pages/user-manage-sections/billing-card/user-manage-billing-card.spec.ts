import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../testing/translate-testing.module';
import { UserManageBillingCardComponent } from './user-manage-billing-card';

describe('UserManageBillingCardComponent', () => {
    it('emits billing card actions', async () => {
        const fixture = await createComponentAsync();
        const component = fixture.componentInstance;
        const billingReload = vi.fn();
        const billingPortalOpen = vi.fn();
        const premiumPageOpen = vi.fn();
        component['billingReload'].subscribe(billingReload);
        component['billingPortalOpen'].subscribe(billingPortalOpen);
        component['premiumPageOpen'].subscribe(premiumPageOpen);

        component['billingReload'].emit();
        component['billingPortalOpen'].emit();
        component['premiumPageOpen'].emit();

        expect(billingReload).toHaveBeenCalledTimes(1);
        expect(billingPortalOpen).toHaveBeenCalledTimes(1);
        expect(premiumPageOpen).toHaveBeenCalledTimes(1);
    });
});

async function createComponentAsync(): Promise<ComponentFixture<UserManageBillingCardComponent>> {
    await TestBed.configureTestingModule({
        imports: [UserManageBillingCardComponent],
        providers: [provideTranslateTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManageBillingCardComponent);
    fixture.componentRef.setInput('isLoadingBilling', false);
    fixture.componentRef.setInput('billingError', null);
    fixture.componentRef.setInput('billingView', null);
    fixture.componentRef.setInput('isOpeningBillingPortal', false);
    fixture.detectChanges();
    return fixture;
}
