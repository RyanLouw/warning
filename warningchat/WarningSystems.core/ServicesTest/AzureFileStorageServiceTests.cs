using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using Moq;

namespace WarningSystemTest.ServicesTest
{
    public class AzureFileStorageServiceTests
    {
        private const string Conn = "fake-conn";

        // Create Azure Response<T>
        private static Response<T> R<T>(T value) => Response.FromValue(value, new Mock<Response>().Object);

        private static IFormFile MakeFormFile(string fileName, byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "file", fileName);
        }

        [Fact]
        public async Task ListWarningFilesAsync_ReturnsEmpty_WhenFolderDoesNotExist()
        {
            var share = new Mock<ShareClient>();
            var idDir = new Mock<ShareDirectoryClient>();
            var filesDir = new Mock<ShareDirectoryClient>();

            share.Setup(s => s.GetDirectoryClient("123")).Returns(idDir.Object);
            idDir.Setup(d => d.GetSubdirectoryClient("files")).Returns(filesDir.Object);

            filesDir.Setup(d => d.ExistsAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(R(false));

            var sut = new AzureFileStorageDataAccess(Conn, () => share.Object);

            var result = await sut.ListWarningFilesAsync(123);

            Assert.Empty(result);
        }

        [Fact]
        public async Task DeleteFileAsync_CallsDeleteIfExists()
        {
            var share = new Mock<ShareClient>();
            var idDir = new Mock<ShareDirectoryClient>();
            var filesDir = new Mock<ShareDirectoryClient>();
            var fileClient = new Mock<ShareFileClient>();

            share.Setup(s => s.GetDirectoryClient("77")).Returns(idDir.Object);
            idDir.Setup(d => d.GetSubdirectoryClient("files")).Returns(filesDir.Object);
            filesDir.Setup(d => d.GetFileClient("x.txt")).Returns(fileClient.Object);

            fileClient.Setup(f => f.DeleteIfExistsAsync(
                    It.IsAny<ShareFileRequestConditions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(R(true));

            var sut = new AzureFileStorageDataAccess(Conn, () => share.Object);

            await sut.DeleteFileAsync(77, "x.txt");

            fileClient.Verify(f => f.DeleteIfExistsAsync(
                    It.IsAny<ShareFileRequestConditions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DownloadFileAsync_Throws_WhenFileMissing()
        {
            var share = new Mock<ShareClient>();
            var idDir = new Mock<ShareDirectoryClient>();
            var filesDir = new Mock<ShareDirectoryClient>();
            var fileClient = new Mock<ShareFileClient>();

            share.Setup(s => s.GetDirectoryClient("88")).Returns(idDir.Object);
            idDir.Setup(d => d.GetSubdirectoryClient("files")).Returns(filesDir.Object);
            filesDir.Setup(d => d.GetFileClient("missing.txt")).Returns(fileClient.Object);

            fileClient.Setup(f => f.ExistsAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(R(false));

            var sut = new AzureFileStorageDataAccess(Conn, () => share.Object);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                sut.DownloadFileAsync(88, "missing.txt", Path.GetTempFileName()));
        }

        [Fact]
        public async Task GetFileStreamAsync_Throws_WhenFileMissing()
        {
            var share = new Mock<ShareClient>();
            var idDir = new Mock<ShareDirectoryClient>();
            var filesDir = new Mock<ShareDirectoryClient>();
            var fileClient = new Mock<ShareFileClient>();

            share.Setup(s => s.GetDirectoryClient("12")).Returns(idDir.Object);
            idDir.Setup(d => d.GetSubdirectoryClient("files")).Returns(filesDir.Object);
            filesDir.Setup(d => d.GetFileClient("missing.bin")).Returns(fileClient.Object);

            fileClient.Setup(f => f.ExistsAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(R(false));

            var sut = new AzureFileStorageDataAccess(Conn, () => share.Object);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                sut.GetFileStreamAsync(12, "missing.bin"));
        }

        [Theory]
        [InlineData("pic.jpg", "Image")]
        [InlineData("pic.jpeg", "Image")]
        [InlineData("pic.png", "Image")]
        [InlineData("sound.mp3", "Audio")]
        [InlineData("video.mp4", "Video")]
        [InlineData("doc.pdf", "Document")]
        [InlineData("doc.docx", "Document")]
        [InlineData("notes.txt", "Document")]
        [InlineData("unknown.xyz", "Other")]
        public void GetAttachmentType_ReturnsExpected(string fileName, string expected)
        {
            // For this method, ShareClient isn't even used, but ctor needs it.
            var share = new Mock<ShareClient>();
            var sut = new AzureFileStorageDataAccess(Conn, () => share.Object);

            var file = MakeFormFile(fileName, new byte[] { 1 });

            var type = sut.GetAttachmentType(file);

            Assert.Equal(expected, type);
        }
    }
}