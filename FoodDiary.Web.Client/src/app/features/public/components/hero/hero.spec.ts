import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { DeferBlockBehavior, DeferBlockState, TestBed } from '@angular/core/testing';
import { TranslatePipe } from '@ngx-translate/core';
import { describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../testing/translate-testing.module';
import { PublicAuthNavigationService } from '../../lib/public-auth-navigation.service';
import { HeroComponent } from './hero';

describe('HeroComponent hydration', () => {
    it('keeps sign-up interactive while the sample chart waits for hydration', async () => {
        const navigation = { navigateAsync: vi.fn().mockResolvedValue(void 0) };
        TestBed.overrideComponent(HeroComponent, { set: { imports: [TranslatePipe], schemas: [CUSTOM_ELEMENTS_SCHEMA] } });
        await TestBed.configureTestingModule({
            deferBlockBehavior: DeferBlockBehavior.Manual,
            imports: [HeroComponent],
            providers: [provideTranslateTesting(), { provide: PublicAuthNavigationService, useValue: navigation }],
        }).compileComponents();
        const fixture = TestBed.createComponent(HeroComponent);
        try {
            fixture.detectChanges();
            const element = fixture.nativeElement as HTMLElement;
            expect(element.querySelector('fd-day-nutrition-summary'), 'sample chart should wait for hydration').toBeNull();

            element.querySelector('.hero__cta--primary')?.dispatchEvent(new Event('click'));
            expect(navigation.navigateAsync).toHaveBeenCalledWith('register');

            const blocks = await fixture.getDeferBlocks();
            expect(blocks).toHaveLength(1);
            await blocks[0]?.render(DeferBlockState.Complete);
            expect(element.querySelector('fd-day-nutrition-summary')).not.toBeNull();
        } finally {
            fixture.destroy();
        }
    });
});
