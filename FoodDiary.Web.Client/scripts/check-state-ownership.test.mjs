import assert from 'node:assert/strict';
import test from 'node:test';

import { findStateOwnershipViolations } from './check-state-ownership.mjs';

test('accepts a stateful feature facade with Injectable and an explicit owner provider', () => {
    const violations = findStateOwnershipViolations([
        {
            path: 'src/app/features/meals/lib/meal-list.facade.ts',
            content: `@Injectable() export class MealListFacade { readonly items = signal([]); }`,
        },
        {
            path: 'src/app/features/meals/pages/meal-list.ts',
            content: `@Component({ providers: [MealListFacade] }) export class MealList {}`,
        },
    ]);

    assert.deepEqual(violations, []);
});

test('rejects a root-decorated stateful feature facade', () => {
    const violations = findStateOwnershipViolations([
        {
            path: 'src/app/features/meals/lib/meal-list.facade.ts',
            content: `@Service() export class MealListFacade { readonly items = signal([]); }`,
        },
        {
            path: 'src/app/features/meals/pages/meal-list.ts',
            content: `@Component({ providers: [MealListFacade] }) export class MealList {}`,
        },
    ]);

    assert.equal(violations.length, 1);
    assert.match(violations[0], /must use @Injectable/u);
});

test('rejects a stateful feature facade without an explicit owner provider', () => {
    const violations = findStateOwnershipViolations([
        {
            path: 'projects/fooddiary-admin/src/app/features/billing/lib/billing.facade.ts',
            content: `@Injectable() export class BillingFacade { readonly page = signal(1); }`,
        },
    ]);

    assert.equal(violations.length, 1);
    assert.match(violations[0], /needs an explicit/u);
});

test('does not require component-local or root app services to be feature facade providers', () => {
    const violations = findStateOwnershipViolations([
        {
            path: 'src/app/services/auth.service.ts',
            content: `@Service() export class AuthService { readonly token = signal(null); }`,
        },
        {
            path: 'src/app/features/meals/lib/meal-api.facade.ts',
            content: `@Service() export class MealApiFacade { query() {} }`,
        },
    ]);

    assert.deepEqual(violations, []);
});
