namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Common;

public interface IRecommendationRepository :
    IRecommendationReadRepository,
    IRecommendationReadModelRepository,
    IRecommendationWriteRepository;
