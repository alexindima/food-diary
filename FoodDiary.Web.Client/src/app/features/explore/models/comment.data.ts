export interface RecipeComment {
    id: string;
    recipeId: string;
    authorId: string;
    authorUsername?: string | null;
    authorFirstName?: string | null;
    text: string;
    createdAtUtc: string;
    modifiedAtUtc?: string | null;
    isOwnedByCurrentUser: boolean;
}

export interface CreateCommentDto {
    text: string;
}

export interface UpdateCommentDto {
    text: string;
}
