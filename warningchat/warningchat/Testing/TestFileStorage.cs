using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using WarningSystems.Core.DataAccess.AzureFileStorage;

namespace WarningSystems.Testing;

public sealed class TestFileStorage : IAzureFileStorageDataAccess
{
    private readonly ConcurrentDictionary<(long Warning, string Name), (byte[] Bytes, string ContentType)> _files = new();

    public async Task<string> UploadWarningFileAsync(int warningId, string type, IFormFile file)
    {
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        var name = $"{type}/{Path.GetFileName(file.FileName)}";
        _files[(warningId, name)] = (buffer.ToArray(), file.ContentType);
        return name;
    }

    public async Task DownloadFileAsync(int warningId, string fileName, string savePath)
    {
        var file = await GetFileAsync(warningId, fileName);
        await using var stream = file.Stream;
        await using var destination = File.Create(savePath);
        await stream.CopyToAsync(destination);
    }

    public Task DeleteFileAsync(long warningId, string fileName)
    {
        _files.TryRemove((warningId, fileName), out _);
        return Task.CompletedTask;
    }

    public Task<List<string>> ListWarningFilesAsync(long warningId) => Task.FromResult(
        _files.Keys.Where(key => key.Warning == warningId).Select(key => key.Name).Order().ToList());

    public string GetAttachmentType(IFormFile file) => file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "Image" : "Document";

    public async Task<Stream> GetFileStreamAsync(int warningId, string fileName) =>
        (await GetFileAsync(warningId, fileName)).Stream;

    public Task<(Stream Stream, string ContentType, string FileName)> GetFileAsync(int warningId, string fileName)
    {
        if (!_files.TryGetValue((warningId, fileName), out var file)) throw new FileNotFoundException(fileName);
        return Task.FromResult<(Stream, string, string)>((new MemoryStream(file.Bytes, false), file.ContentType, Path.GetFileName(fileName)));
    }
}
