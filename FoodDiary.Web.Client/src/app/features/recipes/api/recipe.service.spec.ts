import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { RecipeService } from './recipe.service';
import { PageOf } from '../../../shared/models/page-of.data';
import { Recipe } from '../models/recipe.data';

describe('RecipeService', () => {
    let service: RecipeService;
    let httpMock: HttpTestingController;
    const baseUrl = 'http://localhost:5300/api/recipes';

    const mockRecipe: Recipe = {
        id: 'r1',
        name: 'Grilled Chicken Salad',
        servings: 2,
        totalCalories: 350,
        totalProteins: 40,
        totalFats: 12,
        totalCarbs: 15,
        totalFiber: 4,
        totalAlcohol: 0,
    } as Recipe;

    const mockPage: PageOf<Recipe> = {
        data: [mockRecipe],
        page: 1,
        limit: 10,
        totalPages: 1,
        totalItems: 1,
    };

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                RecipeService,
                provideHttpClient(),
                provideHttpClientTesting(),
            ],
        });
        service = TestBed.inject(RecipeService);
        httpMock = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        httpMock.verify();
    });

    it('should be created', () => {
        expect(service).toBeTruthy();
    });

    it('should query recipes with pagination params', () => {
        service.query(1, 10).subscribe(result => {
            expect(result).toEqual(mockPage);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        expect(req.request.params.get('page')).toBe('1');
        expect(req.request.params.get('limit')).toBe('10');
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(mockPage);
    });

    it('should include search filter in query params', () => {
        const filters = { search: 'salad' };

        service.query(1, 10, filters, false).subscribe();

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        expect(req.request.params.get('search')).toBe('salad');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(mockPage);
    });

    it('should get recipe by id with includePublic param', () => {
        service.getById('r1').subscribe(result => {
            expect(result).toEqual(mockRecipe);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/r1` && r.method === 'GET');
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(mockRecipe);
    });

    it('should get recipe by id with includePublic false', () => {
        service.getById('r1', false).subscribe(result => {
            expect(result).toEqual(mockRecipe);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/r1` && r.method === 'GET');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush(mockRecipe);
    });

    it('should create recipe', () => {
        const createData = { name: 'New Recipe', servings: 4 } as any;

        service.create(createData).subscribe(result => {
            expect(result).toEqual(mockRecipe);
        });

        const req = httpMock.expectOne(`${baseUrl}/`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual(createData);
        req.flush(mockRecipe);
    });

    it('should update recipe via PATCH', () => {
        const updateData = { name: 'Updated Recipe' } as any;

        service.update('r1', updateData).subscribe(result => {
            expect(result).toEqual(mockRecipe);
        });

        const req = httpMock.expectOne(`${baseUrl}/r1`);
        expect(req.request.method).toBe('PATCH');
        expect(req.request.body).toEqual(updateData);
        req.flush(mockRecipe);
    });

    it('should delete recipe by id', () => {
        service.deleteById('r1').subscribe();

        const req = httpMock.expectOne(`${baseUrl}/r1`);
        expect(req.request.method).toBe('DELETE');
        req.flush(null);
    });

    it('should duplicate recipe', () => {
        service.duplicate('r1').subscribe(result => {
            expect(result).toEqual(mockRecipe);
        });

        const req = httpMock.expectOne(`${baseUrl}/r1/duplicate`);
        expect(req.request.method).toBe('POST');
        expect(req.request.body).toEqual({});
        req.flush(mockRecipe);
    });

    it('should get recent recipes with default params', () => {
        const recentRecipes = [mockRecipe];

        service.getRecent().subscribe(result => {
            expect(result).toEqual(recentRecipes);
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/recent` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe('10');
        expect(req.request.params.get('includePublic')).toBe('true');
        req.flush(recentRecipes);
    });

    it('should get recent recipes with custom params', () => {
        service.getRecent(5, false).subscribe();

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/recent` && r.method === 'GET');
        expect(req.request.params.get('limit')).toBe('5');
        expect(req.request.params.get('includePublic')).toBe('false');
        req.flush([]);
    });

    it('should re-throw error on query failure', () => {
        service.query(1, 10).subscribe({
            error: err => {
                expect(err.status).toBe(500);
            },
        });

        const req = httpMock.expectOne(r => r.url === `${baseUrl}/` && r.method === 'GET');
        req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
    });
});
