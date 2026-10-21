import { RECIPE_MAX_INGREDIENT_AMOUNT } from './recipe-manage-form.mapper';

export const RECIPE_DEVELOPMENT_SCHEMA = {
    type: 'object',
    additionalProperties: false,
    required: ['name', 'servings', 'steps'],
    properties: {
        name: { type: 'string' },
        servings: { type: 'integer', minimum: 1 },
        cookTime: { type: 'number', minimum: 1, description: 'Minutes; defaults to 1.' },
        steps: {
            type: 'array',
            minItems: 1,
            maxItems: 10,
            items: {
                type: 'object',
                required: ['description', 'ingredients'],
                additionalProperties: false,
                properties: {
                    description: { type: 'string' },
                    ingredients: {
                        type: 'array',
                        maxItems: 20,
                        items: {
                            type: 'object',
                            required: ['amount', 'unit'],
                            oneOf: [{ required: ['productId'] }, { required: ['recipeId'] }],
                            additionalProperties: false,
                            properties: {
                                productId: { type: 'string' },
                                recipeId: { type: 'string', description: 'Exact accessible nested recipe ID; amount counts servings.' },
                                amount: { type: 'number', exclusiveMinimum: 0, maximum: RECIPE_MAX_INGREDIENT_AMOUNT },
                                unit: { enum: ['G', 'ML', 'PCS', 'servings'] },
                            },
                        },
                    },
                },
            },
        },
    },
};
