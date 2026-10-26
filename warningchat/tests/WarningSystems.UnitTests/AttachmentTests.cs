using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WarningSystems.Models.DTO;

namespace WarningSystems.UnitTests;

public class AttachmentTests
{
    private readonly ManagerFixture _f = new();
    private static IFormFile File(string name = "proof.pdf", byte[]? bytes = null) =>
        new FormFile(new MemoryStream(bytes ?? [1, 2, 3]), 0, (bytes ?? [1, 2, 3]).Length, "file", name);

    [Fact]
    public async Task No_previous_offences_never_uploads_even_if_files_are_supplied()
    {
        await _f.Manager.SavePreviousOffencesAsync(new() { WarningId = 42, HasPreviousOffences = false, Files = [File()] });
        _f.Storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Previous_offences_upload_nonempty_files_with_correct_warning_id()
    {
        var file = File();
        _f.Storage.Setup(x => x.UploadWarningFileAsync(42, "PreviousOffences", file)).ReturnsAsync("saved.pdf");
        await _f.Manager.SavePreviousOffencesAsync(new() { WarningId = 42, HasPreviousOffences = true, Files = [null!, File(bytes: []), file] });
        _f.Storage.VerifyAll();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Empty_evidence_uploads_are_ignored(bool legal)
    {
        var dto = new SaveAttachmentsDto { WarningId = 42, Files = [null!, File(bytes: [])] };
        var result = legal ? await _f.Manager.SaveEvidenceAttachmentAsync(dto) : await _f.Manager.SaveUserEvidenceAttachmentAsync(dto);
        Assert.IsType<OkObjectResult>(result);
        _f.Storage.VerifyNoOtherCalls();
        _f.Data.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(" Friendly name ", "Evidence_Friendly name")]
    [InlineData(null, "Evidence_proof")]
    [InlineData("/", "Evidence_File")]
    public async Task User_attachment_name_and_metadata_are_preserved(string? display, string prefix)
    {
        var file = File();
        _f.Storage.Setup(x => x.GetAttachmentType(file)).Returns("Document");
        _f.Storage.Setup(x => x.UploadWarningFileAsync(42, prefix, file)).ReturnsAsync("stored.pdf");
        _f.Data.Setup(x => x.SaveEvidenceAsync(42, "stored.pdf", "Document", 3, "actor", "note", 1)).ReturnsAsync(7);
        Assert.IsType<OkObjectResult>(await _f.Manager.SaveUserEvidenceAttachmentAsync(new() { WarningId = 42, AttachmentName = display, Files = [file], Notes = "note" }));
        _f.Data.VerifyAll();
        _f.Storage.VerifyAll();
    }

    [Fact]
    public async Task Legal_attachments_have_legal_note_type()
    {
        var file = File();
        _f.Storage.Setup(x => x.GetAttachmentType(file)).Returns("Document");
        _f.Storage.Setup(x => x.UploadWarningFileAsync(42, "LegalEvidence", file)).ReturnsAsync("stored.pdf");
        _f.Data.Setup(x => x.SaveEvidenceAsync(42, "stored.pdf", "Document", 3, "actor", "note", 3)).ReturnsAsync(7);
        Assert.IsType<OkObjectResult>(await _f.Manager.SaveEvidenceAttachmentAsync(new() { WarningId = 42, Files = [file], Notes = "note" }));
        _f.Data.VerifyAll();
    }
}
