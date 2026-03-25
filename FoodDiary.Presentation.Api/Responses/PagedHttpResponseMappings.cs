using FoodDiary.Application.Common.Models;

namespace FoodDiary.Presentation.Api.Responses;

public static class PagedHttpResponseMappings {
    public static PagedHttpResponse<THttpResponse> ToPagedHttpResponse<TModel, THttpResponse>(
        this PagedResponse<TModel> response,
        Func<TModel, THttpResponse> map) {
        return new PagedHttpResponse<THttpResponse>(
            response.Data.Select(map).ToList(),
            response.Page,
            response.Limit,
            response.TotalPages,
            response.TotalItems
        );
    }
}
