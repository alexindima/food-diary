namespace FoodDiary.Presentation.Api.Responses;

public static class EnumerableHttpResponseMappings {
    public static List<THttpResponse> ToHttpResponseList<TModel, THttpResponse>(
        this IEnumerable<TModel> models,
        Func<TModel, THttpResponse> map) {
        return models.Select(map).ToList();
    }
}
