import type { ComponentFixture } from '@angular/core/testing';
import { TestBed } from '@angular/core/testing';
import { Subject, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { provideTranslateTesting } from '../../../../../../../src/testing/translate-testing.module';
import { AdminCatalogService } from '../api/admin-catalog.service';
import type { CatalogProduct, CatalogRecipe } from '../models/catalog-transfer';
import { AdminCatalogComponent } from './admin-catalog';

describe('AdminCatalog export concurrency and recovery', () => {
    let fixture: ComponentFixture<AdminCatalogComponent>;
    const exportCatalog = vi.fn();

    beforeEach(async () => {
        exportCatalog.mockReset();
        await TestBed.configureTestingModule({
            imports: [AdminCatalogComponent],
            providers: [...provideTranslateTesting(), { provide: AdminCatalogService, useValue: { exportCatalog } }],
        }).compileComponents();
        fixture = TestBed.createComponent(AdminCatalogComponent);
        fixture.detectChanges();
    });

    it('ignores a second click before disabled state renders and permits retry after an error', async () => {
        const pending = new Subject<{ products: CatalogProduct[]; recipes: CatalogRecipe[] }>();
        exportCatalog.mockReturnValueOnce(pending).mockReturnValueOnce(throwError(() => new Error('Retry unavailable')));
        const element = fixture.nativeElement as HTMLElement;
        const button = element.querySelector<HTMLButtonElement>('fd-ui-button button');
        if (button === null) {
            throw new Error('Export button is missing');
        }

        button.click();
        button.click();
        expect(exportCatalog).toHaveBeenCalledTimes(1);
        fixture.detectChanges();
        expect(button.disabled).toBe(true);

        pending.error(new Error('Export unavailable'));
        await fixture.whenStable();
        fixture.detectChanges();
        expect(button.disabled).toBe(false);
        expect(element.querySelector('[role=alert]')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.EXPORT_FAILED');

        button.click();
        await fixture.whenStable();
        fixture.detectChanges();
        expect(exportCatalog).toHaveBeenCalledTimes(2);
        expect(button.disabled).toBe(false);
        expect(element.querySelector('[role=alert]')?.textContent).toContain('ADMIN_CATALOG_TRANSFER.EXPORT_FAILED');
    });
});
