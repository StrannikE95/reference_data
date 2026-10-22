using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using management.api.Contracts;

namespace management.api.Services;

/// <summary>
/// Файлы заполнения черновика и заливка строк для DataController.
/// Паспорт и статус версии не меняет. Пока хранилища нет, здесь проверяется форма запроса,
/// а успешный ответ — пример контракта.
/// </summary>
public sealed class DataService
{
    /// <summary>
    /// Начинает запись пачки файлов черновика и возвращает ссылки на запись.
    /// </summary>
    public ActionResult<FileBatchResponse> StartDraftFiles(
        string dictionaryCode,
        FileBatchRequest? request,
        ModelStateDictionary modelState)
    {
        var rejected = RejectFiles(request, modelState);
        if (rejected is not null)
            return rejected;

        var files = new List<FileDescription>();
        foreach (var file in request!.Files!)
        {
            var fileId = Guid.NewGuid();
            files.Add(new FileDescription
            {
                FileId = fileId,
                Name = file.Name!,
                Size = file.Size,
                State = "uploading",
                Error = null,
                UploadUrl = $"https://storage.example/upload/{fileId}"
            });
        }

        return new OkObjectResult(new FileBatchResponse { Files = files });
    }

    /// <summary>
    /// Подтверждает запись файла черновика. Файл версии должен заполнять исходную таблицу профиля.
    /// </summary>
    public ActionResult<FileDescription> CompleteDraftFile(
        string dictionaryCode,
        Guid fileId,
        ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request", DictionaryCode = dictionaryCode });

        return new OkObjectResult(new FileDescription
        {
            FileId = fileId,
            Name = "wall_material.sql",
            Size = 240,
            State = "valid",
            Error = null
        });
    }

    /// <summary>
    /// Снимает файл с черновика. Файл в состоянии loaded дополнительно очищает секцию своей таблицы.
    /// </summary>
    public ActionResult DeleteDraftFile(string dictionaryCode, Guid fileId, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request", DictionaryCode = dictionaryCode });

        return new NoContentResult();
    }

    /// <summary>
    /// Завершает заливку файлов в состоянии valid. Ошибка одной таблицы не откатывает таблицы, которые уже loaded.
    /// </summary>
    public ActionResult<DraftResponse> Apply(string dictionaryCode, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request", DictionaryCode = dictionaryCode });

        return new OkObjectResult(new DraftResponse
        {
            DictionaryCode = dictionaryCode,
            VersionId = 3,
            VersionStatus = "draft",
            Delivery = "full",
            Files =
            [
                new FileDescription
                {
                    FileId = Guid.Parse("91c0e2b4-6a18-4f0d-9b77-3e5a1c8d4420"),
                    Name = "wall_material.sql",
                    Size = 240,
                    State = "loaded",
                    Error = null
                }
            ]
        });
    }

    private static ActionResult? RejectFiles(FileBatchRequest? request, ModelStateDictionary modelState)
    {
        if (!modelState.IsValid || request?.Files is null || request.Files.Count is < 1 or > 100)
            return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in request.Files)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.Name) || file.Size < 1 || !names.Add(file.Name))
                return new BadRequestObjectResult(new AdminErrorResponse { Error = "invalid_request" });
        }

        return null;
    }
}
