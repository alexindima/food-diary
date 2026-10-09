import { inject, Service } from '@angular/core';
import type { Observable } from 'rxjs';

import type { PageOf } from '../../../shared/models/page-of.data';
import type { RecipeCommentId, RecipeId } from '../../../shared/models/semantics/entity-id';
import { CommentService } from '../api/comment.service';
import { ExploreService } from '../api/explore.service';
import { LikeService } from '../api/like.service';
import { ReportService } from '../api/report.service';
import type { CreateCommentDto, RecipeComment, UpdateCommentDto } from '../models/comment.data';
import type { ExploreFilters, ExploreRecipe } from '../models/explore.data';
import type { RecipeLikeStatus } from '../models/like.data';
import type { ContentReport, CreateReportDto } from '../models/report.data';

@Service()
export class ExploreInteractionsFacade {
    private readonly commentService = inject(CommentService);
    private readonly exploreService = inject(ExploreService);
    private readonly likeService = inject(LikeService);
    private readonly reportService = inject(ReportService);

    public getComments(recipeId: RecipeId, page: number, limit: number): Observable<PageOf<RecipeComment>> {
        return this.commentService.getComments(recipeId, page, limit);
    }

    public createComment(recipeId: RecipeId, dto: CreateCommentDto): Observable<RecipeComment> {
        return this.commentService.createComment(recipeId, dto);
    }

    public updateComment(recipeId: RecipeId, commentId: RecipeCommentId, dto: UpdateCommentDto): Observable<RecipeComment> {
        return this.commentService.updateComment(recipeId, commentId, dto);
    }

    public deleteComment(recipeId: RecipeId, commentId: RecipeCommentId): Observable<void> {
        return this.commentService.deleteComment(recipeId, commentId);
    }

    public getLikeStatus(recipeId: string): Observable<RecipeLikeStatus> {
        return this.likeService.getStatus(recipeId);
    }

    public toggleLike(recipeId: string, isLiked: boolean): Observable<RecipeLikeStatus> {
        return this.likeService.toggle(recipeId, isLiked);
    }

    public createReport(dto: CreateReportDto): Observable<ContentReport> {
        return this.reportService.create(dto);
    }

    public queryRecipes(page: number, limit: number, filters?: ExploreFilters): Observable<PageOf<ExploreRecipe>> {
        return this.exploreService.query(page, limit, filters);
    }
}
