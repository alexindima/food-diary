import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from '@angular/common/http/testing';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FdUiDialogService } from 'fd-ui-kit/dialog/fd-ui-dialog.service';
import { describe, expect, it, onTestFinished, vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { waitForAsyncTasksAsync } from '../../../testing/async-testing';
import { stubScrollIntoView } from '../../../testing/dom-testing';
import { provideTranslateTesting } from '../../../testing/translate-testing.module';
import { AuthService } from '../../services/auth.service';
import { SessionEventsService } from '../../shared/auth/session-events.service';
import { PublicAuthDialogService } from '../public/contracts/auth-dialog';
import { ShoppingListAddFacade } from './lib/shopping-list-add.facade';

const LIST_URL = environment.apiUrls.shoppingLists;
const DESTINATION = { id: 'shopping-list-1', name: 'Groceries', createdAt: '2026-11-20T08:00:00Z' };
const DESTINATION_URL = `${LIST_URL}/${DESTINATION.id}`;
const REVISION_LENGTH = 64;
const REVISION = 'a'.repeat(REVISION_LENGTH);
const EXISTING_LIST = { ...DESTINATION, revision: REVISION, items: [] };
const SAVED_LIST = {
    ...EXISTING_LIST,
    revision: 'b'.repeat(REVISION_LENGTH),
    items: [{ id: 'saved-rice', shoppingListId: DESTINATION.id, name: 'Rice', isChecked: false, sortOrder: 1 }],
};
const SHOPPING_PICKER = 'fd-shopping-list-picker';

@Component({
    template: `<button type="button" data-add-rice [disabled]="shopping.busy() || shopping.isAdded(0)" (click)="addRice()">Add rice</button>
        <button type="button" data-choose-list [disabled]="shopping.busy()" (click)="chooseList()">Choose list</button>
        <button type="button" data-add-all [disabled]="shopping.busy()" (click)="addAll()">Add all</button>
        <span data-destination>{{ shopping.target()?.name }}</span>
        <span aria-live="polite">{{ shopping.message() }}</span>`,
    providers: [ShoppingListAddFacade],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
class ShoppingAppendHostComponent {
    protected readonly shopping = inject(ShoppingListAddFacade);

    public constructor() {
        this.shopping.setScope('recipe-1', 'Soup');
    }

    protected addRice(): void {
        void this.shopping.addAsync(0, { name: 'Rice' });
    }

    protected chooseList(): void {
        void this.shopping.chooseAsync();
    }

    protected addAll(): void {
        void this.shopping.addAllAsync([{ index: 0, item: { name: 'Rice' } }]);
    }
}

describe('Shopping append across a session boundary', () => {
    it.each([{ signInAgain: false }, { signInAgain: true }])(
        'does not write an item staged before sign-out (signed in again=$signInAgain)',
        async ({ signInAgain }) => {
            const { fixture, authenticated, sessions, http } = renderShopping();
            const delayedRead = await chooseExistingListAsync(fixture, http);

            authenticated.set(false);
            sessions.notifySessionEnded();
            fixture.detectChanges();
            if (signInAgain) {
                authenticated.set(true);
                sessions.notifyAuthenticated();
                fixture.detectChanges();
            }

            delayedRead.flush(EXISTING_LIST);
            await waitForAsyncTasksAsync();
            const abandonedWrites = http.match({ method: 'PATCH', url: DESTINATION_URL });
            expect(abandonedWrites).toHaveLength(0);
            await vi.waitFor(() => {
                fixture.detectChanges();
                expect(button(fixture.nativeElement as HTMLElement, 'button').disabled).toBe(false);
            });

            if (signInAgain) {
                const freshRead = await chooseExistingListAsync(fixture, http);
                freshRead.flush(EXISTING_LIST);
                const freshWrite = await vi.waitFor(() => http.expectOne({ method: 'PATCH', url: DESTINATION_URL }));
                expect(freshWrite.request.body).toEqual({
                    expectedRevision: REVISION,
                    items: [{ name: 'Rice', sortOrder: 1 }],
                });
                freshWrite.flush(SAVED_LIST);
                await vi.waitFor(() => {
                    fixture.detectChanges();
                    expect((fixture.nativeElement as HTMLElement).textContent).toContain('PUBLIC_RECIPES.SHOPPING_SUCCESS');
                });
            }
        },
    );
});

describe('Shopping bulk append when returning to a previous list', () => {
    it('does not append the same ingredient again when the picked list already received it', async () => {
        const { fixture, http } = renderShopping();
        const read = await chooseExistingListAsync(fixture, http);
        read.flush(EXISTING_LIST);
        const update = await vi.waitFor(() => http.expectOne({ method: 'PATCH', url: DESTINATION_URL }));
        update.flush(SAVED_LIST);

        const otherList = { ...DESTINATION, id: 'shopping-list-2', name: 'Pantry' };
        await chooseDestinationAsync(fixture, http, 'button[data-choose-list]', otherList);
        const host = fixture.nativeElement as HTMLElement;
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[data-destination]')?.textContent).toBe(otherList.name);
            expect(button(host, 'button[data-add-all]').disabled).toBe(false);
        });

        await chooseDestinationAsync(fixture, http, 'button[data-add-all]', DESTINATION);
        await vi.waitFor(() => {
            fixture.detectChanges();
            expect(host.querySelector('[data-destination]')?.textContent).toBe(DESTINATION.name);
            expect(button(host, 'button[data-add-all]').disabled).toBe(false);
            expect(button(host, 'button[data-add-rice]').disabled).toBe(true);
        });
        http.expectNone({ method: 'GET', url: DESTINATION_URL });
        http.expectNone({ method: 'PATCH', url: DESTINATION_URL });
    });
});

function renderShopping(): {
    fixture: ComponentFixture<ShoppingAppendHostComponent>;
    authenticated: ReturnType<typeof signal<boolean>>;
    sessions: SessionEventsService;
    http: HttpTestingController;
} {
    stubScrollIntoView();
    const authenticated = signal(true);
    vi.stubGlobal(
        'matchMedia',
        vi.fn(() => ({ matches: false })),
    );
    TestBed.configureTestingModule({
        imports: [ShoppingAppendHostComponent],
        providers: [
            provideRouter([]),
            provideHttpClient(),
            provideHttpClientTesting(),
            provideTranslateTesting(),
            { provide: AuthService, useValue: { isAuthenticated: authenticated } },
            { provide: PublicAuthDialogService, useValue: { openAsync: vi.fn().mockResolvedValue(null) } },
        ],
    });
    const http = TestBed.inject(HttpTestingController);
    const dialogs = TestBed.inject(FdUiDialogService);
    onTestFinished(() => {
        dialogs.closeAll();
        vi.unstubAllGlobals();
        http.verify();
    });
    const fixture = TestBed.createComponent(ShoppingAppendHostComponent);
    fixture.detectChanges();
    return { fixture, authenticated, sessions: TestBed.inject(SessionEventsService), http };
}

async function chooseExistingListAsync(
    fixture: ComponentFixture<ShoppingAppendHostComponent>,
    http: HttpTestingController,
): Promise<TestRequest> {
    await chooseDestinationAsync(fixture, http, 'button[data-add-rice]', DESTINATION);
    return vi.waitFor(() => http.expectOne({ method: 'GET', url: DESTINATION_URL }));
}

async function chooseDestinationAsync(
    fixture: ComponentFixture<ShoppingAppendHostComponent>,
    http: HttpTestingController,
    triggerSelector: string,
    destination: typeof DESTINATION,
): Promise<void> {
    const trigger = await vi.waitFor(() => {
        fixture.detectChanges();
        const action = button(fixture.nativeElement as HTMLElement, triggerSelector);
        expect(action.disabled).toBe(false);
        return action;
    });
    expect(trigger.disabled).toBe(false);
    trigger.click();
    const available = await vi.waitFor(() => http.expectOne(request => request.method === 'GET' && request.url === `${LIST_URL}/page`));
    available.flush([{ ...destination, itemsCount: 0, remainingCount: 0 }]);
    const picker = await vi.waitFor(() => {
        fixture.detectChanges();
        const dialog = document.querySelector(SHOPPING_PICKER);
        if (!(dialog instanceof HTMLElement)) {
            throw new Error('Expected a rendered shopping list picker');
        }
        return dialog;
    });
    button(picker, 'fd-ui-select button[aria-haspopup="listbox"]').click();
    fixture.detectChanges();
    const option = Array.from(document.querySelectorAll<HTMLButtonElement>('button[role="option"]')).find(
        candidate => candidate.textContent.trim() === destination.name,
    );
    if (option === undefined) {
        throw new Error('Expected the existing shopping list option');
    }
    option.click();
    fixture.detectChanges();
    button(picker, 'button[type="submit"]').click();
}

function button(host: ParentNode, selector: string): HTMLButtonElement {
    const found = host.querySelector<HTMLButtonElement>(selector);
    if (found === null) {
        throw new Error('Expected the shopping append action');
    }
    return found;
}
