using System.Text.Json;

namespace FoodDiary.Development.Mcp.Tasks;

public interface ITaskToolExecutor {
    Task<JsonElement> ExecuteAsync(TaskToolRequest request, CancellationToken cancellationToken);
}
