import { HttpContextToken } from '@angular/common/http';

export const SKIP_OBSERVABILITY = new HttpContextToken<boolean>(() => false);
