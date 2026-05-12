export type RecipeComment = {
    id: string;
    recipeId: string;
    authorId: string;
    authorUsername?: string | null;
    authorFirstName?: string | null;
    text: string;
    createdAtUtc: string;
    modifiedAtUtc?: string | null;
    isOwnedByCurrentUser: boolean;
};

export type CreateCommentDto = {
    text: string;
};

export type UpdateCommentDto = {
    text: string;
};
