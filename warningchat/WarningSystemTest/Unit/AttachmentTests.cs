using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FakeItEasy;
using WarningSystems.Models.DTO;

namespace WarningSystemTest.Unit;

[Trait("Category", "Unit")]
public class AttachmentTests
{
    private readonly ManagerFixture _f = new();
    private static IFormFile File(string name = "proof.pdf", byte[]? bytes = null) =>
        new FormFile(new MemoryStream(bytes ?? [1, 2, 3]), 0, (bytes ?? [1, 2, 3]).Length, "file", name);

    [Fact]
    public async Task No_previous_offences_never_uploads_even_if_files_are_supplied()
    {
        await _f.Manager.SavePreviousOffencesAsync(new() { WarningId = 42, HasPreviousOffences = false, Files = [File()] });
        Assert.Empty(Fake.GetCalls(_f.Storage));
    }

    [Fact]
    public async Task Previous_offences_upload_nonempty_files_with_correct_warning_id()
    {
        var file = File();
        A.CallTo(() => _f.Storage.UploadWarningFileAsync(42, "PreviousOffences", file)).Returns("saved.pdf");
        await _f.Manager.SavePreviousOffencesAsync(new() { WarningId = 42, HasPreviousOffences = true, Files = [null!, File(bytes: []), file] });
        A.CallTo(() => _f.Storage.UploadWarningFileAsync(42, "PreviousOffences", file)).MustHaveHappened();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Empty_evidence_uploads_are_ignored(bool legal)
    {
        var dto = new SaveAttachmentsDto { WarningId = 42, Files = [null!, File(bytes: [])] };
        var result = legal ? await _f.Manager.SaveEvidenceAttachmentAsync(dto) : await _f.Manager.SaveUserEvidenceAttachmentAsync(dto);
        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(Fake.GetCalls(_f.Storage));
        Assert.Empty(Fake.GetCalls(_f.Data));
    }

    [Theory]
    [InlineData(" Friendly name ", "Evidence_Friendly name")]
    [InlineData(null, "Evidence_proof")]
    [InlineData("/", "Evidence_File")]
    public async Task User_attachment_name_and_metadata_are_preserved(string? display, string prefix)
    {
        var file = File();
        A.CallTo(() => _f.Storage.GetAttachmentType(file)).Returns("Document");
        A.CallTo(() => _f.Storage.UploadWarningFileAsync(42, prefix, file)).Returns("stored.pdf");
        A.CallTo(() => _f.Data.SaveEvidenceAsync(42, "stored.pdf", "Document", 3, "actor", "note", 1)).Returns(7);
        Assert.IsType<OkObjectResult>(await _f.Manager.SaveUserEvidenceAttachmentAsync(new() { WarningId = 42, AttachmentName = display, Files = [file], Notes = "note" }));
        A.CallTo(() => _f.Data.SaveEvidenceAsync(42, "stored.pdf", "Document", 3, "actor", "note", 1)).MustHaveHappened();
        A.CallTo(() => _f.Storage.GetAttachmentType(file)).MustHaveHappened();
        A.CallTo(() => _f.Storage.UploadWarningFileAsync(42, prefix, file)).MustHaveHappened();
    }

    [Fact]
    public async Task Legal_attachments_have_legal_note_type()
    {
        var file = File();
        A.CallTo(() => _f.Storage.GetAttachmentType(file)).Returns("Document");
        A.CallTo(() => _f.Storage.UploadWarningFileAsync(42, "LegalEvidence", file)).Returns("stored.pdf");
        A.CallTo(() => _f.Data.SaveEvidenceAsync(42, "stored.pdf", "Document", 3, "actor", "note", 3)).Returns(7);
        Assert.IsType<OkObjectResult>(await _f.Manager.SaveEvidenceAttachmentAsync(new() { WarningId = 42, Files = [file], Notes = "note" }));
        A.CallTo(() => _f.Data.SaveEvidenceAsync(42, "stored.pdf", "Document", 3, "actor", "note", 3)).MustHaveHappened();
    }
}
