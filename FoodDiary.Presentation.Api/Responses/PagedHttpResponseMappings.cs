using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Presentation.Api.Responses;

public static class PagedHttpResponseMappings {
    extension<TModel>(PagedResponse<TModel> response) {
        public PagedHttpResponse<THttpResponse> ToPagedHttpResponse<THttpResponse>(
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
}
