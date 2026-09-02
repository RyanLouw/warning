using Microsoft.AspNetCore.Http;

namespace WarningSystems.Core.DataAccess.AzureFileStorage;

public interface IAzureFileStorageDataAccess
{
    public Task<string> UploadWarningFileAsync(int warningId, string type, IFormFile file);

    public Task DownloadFileAsync(int warningId, string fileName, string savePath);

    public Task DeleteFileAsync(long warningId, string fileName);

    public Task<List<string>> ListWarningFilesAsync(long warningId);

    public string GetAttachmentType(IFormFile file);

    public Task<Stream> GetFileStreamAsync(int warningId, string fileName);

    public Task<(Stream Stream, string ContentType, string FileName)> GetFileAsync(int warningId, string fileName);
}