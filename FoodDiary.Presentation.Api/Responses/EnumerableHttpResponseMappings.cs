namespace FoodDiary.Presentation.Api.Responses;

public static class EnumerableHttpResponseMappings {
    extension<TModel>(IEnumerable<TModel> models) {
        public IReadOnlyList<THttpResponse> ToHttpResponseList<THttpResponse>(
                Func<TModel, THttpResponse> map) {
            return models.Select(map).ToList();
        }
    }
}
