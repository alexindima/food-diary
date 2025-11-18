export interface ValidationErrors {
    required?: () => string;
    userExists?: () => string;
    email?: () => string;
    matchField?: () => string;
    nonEmptyArray?: () => string;
    min?: (_params: { min: string }) => string;
    minlength?: (_params: { requiredLength: string }) => string;
}
