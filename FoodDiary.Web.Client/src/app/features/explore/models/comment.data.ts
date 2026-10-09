import type { UtcInstant } from '../../../shared/models/semantics/date-value';
import type { RecipeCommentId, RecipeId, UserId } from '../../../shared/models/semantics/entity-id';

export type RecipeComment = {
    id: RecipeCommentId;
    recipeId: RecipeId;
    authorId: UserId;
    authorUsername?: string | null;
    authorFirstName?: string | null;
    text: string;
    createdAtUtc: UtcInstant;
    modifiedAtUtc?: UtcInstant | null;
    isOwnedByCurrentUser: boolean;
};

export type CreateCommentDto = {
    text: string;
};

export type UpdateCommentDto = {
    text: string;
};
