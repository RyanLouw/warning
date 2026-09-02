using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;

namespace WarningSystems.Core.DataAccess.AzureFileStorage;

public class AzureFileStorageDataAccess : IAzureFileStorageDataAccess
{
    private readonly string _connectionString;
    private const string ShareName = "warningshare";

    private readonly Func<ShareClient> _shareClientFactory;

    public AzureFileStorageDataAccess(string connectionString)
    {
        _connectionString = connectionString;
        _shareClientFactory = () => new ShareClient(_connectionString, ShareName);
    }

    public async Task<List<string>> ListWarningFilesAsync(long warningId)
    {
        var shareClient = _shareClientFactory();

        var filesDir = shareClient
            .GetDirectoryClient(warningId.ToString())
            .GetSubdirectoryClient("files");

        if (!await filesDir.ExistsAsync())
            return new List<string>();

        var result = new List<string>();

        await foreach (ShareFileItem item in filesDir.GetFilesAndDirectoriesAsync())
        {
            if (!item.IsDirectory)
                result.Add(item.Name);
        }

        return result;
    }

    public async Task<string> UploadWarningFileAsync(int warningId, string type, IFormFile file)
    {
        string fileName = $"{type}_{Guid.NewGuid()}_{file.FileName}";

        var shareClient = _shareClientFactory();
        await shareClient.CreateIfNotExistsAsync();

        var idDir = shareClient.GetDirectoryClient(warningId.ToString());
        await idDir.CreateIfNotExistsAsync();

        var filesDir = idDir.GetSubdirectoryClient("files");
        await filesDir.CreateIfNotExistsAsync();

        var fileClient = filesDir.GetFileClient(fileName);

        using var stream = file.OpenReadStream();
        await fileClient.CreateAsync(stream.Length);
        await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);

        return fileName;
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> GetFileAsync(int warningId, string fileName)
    {
        var shareClient = _shareClientFactory();

        var fileClient = shareClient
            .GetDirectoryClient(warningId.ToString())
            .GetSubdirectoryClient("files")
            .GetFileClient(fileName);

        if (!await fileClient.ExistsAsync())
            throw new FileNotFoundException($"File not found: {fileName}");

        var download = await fileClient.DownloadAsync();

        var memoryStream = new MemoryStream();
        await download.Value.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var contentType = GetContentType(fileName);

        return (memoryStream, contentType, fileName);
    }

    private static string GetContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    public async Task DeleteFileAsync(long warningId, string fileName)
    {
        var shareClient = _shareClientFactory();

        var fileClient = shareClient
            .GetDirectoryClient(warningId.ToString())
            .GetSubdirectoryClient("files")
            .GetFileClient(fileName);

        await fileClient.DeleteIfExistsAsync();
    }

    public async Task DownloadFileAsync(int warningId, string fileName, string savePath)
    {
        var shareClient = _shareClientFactory();

        var fileClient = shareClient
            .GetDirectoryClient(warningId.ToString())
            .GetSubdirectoryClient("files")
            .GetFileClient(fileName);

        if (!await fileClient.ExistsAsync())
            throw new FileNotFoundException($"File not found: {fileName}");

        var download = await fileClient.DownloadAsync();

        using var fileStream = File.OpenWrite(savePath);
        await download.Value.Content.CopyToAsync(fileStream);
    }

    public async Task<Stream> GetFileStreamAsync(int warningId, string fileName)
    {
        var shareClient = _shareClientFactory();

        var fileClient = shareClient
            .GetDirectoryClient(warningId.ToString())
            .GetSubdirectoryClient("files")
            .GetFileClient(fileName);

        if (!await fileClient.ExistsAsync())
            throw new FileNotFoundException($"File not found: {fileName}");

        var download = await fileClient.DownloadAsync();
        return download.Value.Content;
    }

    public string GetAttachmentType(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLower();

        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" => "Image",
            ".mp3" or ".wav" or ".ogg" => "Audio",
            ".mp4" or ".mov" or ".avi" => "Video",
            ".pdf" or ".doc" or ".docx" or ".txt" => "Document",
            _ => "Other"
        };
    }
}