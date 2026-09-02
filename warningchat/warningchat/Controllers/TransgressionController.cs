using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics;
using WarningSystems.Core;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.Manager;
using WarningSystems.Core.ViewModels;
using WarningSystems.Models.DTO;
using WarningSystems.Services.Interface;

namespace WarningSystems.Controllers
{
    [Authorize]
    public class TransgressionController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITransgressionManager _transgression;
        private readonly IAzureFileStorageDataAccess _fileStorage;
        private readonly IWarningPdfExportService _pdf;

        public TransgressionController(ILogger<HomeController> logger, ITransgressionManager trangretion, IAzureFileStorageDataAccess fileStorage, IWarningPdfExportService pdf)
        {
            _logger = logger;
            _transgression = trangretion;
            _fileStorage = fileStorage;
            _pdf = pdf;
        }

        [AuthorizeForScopes(Scopes = new[] { "User.Read.All" })]

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Index(long? id)
        {
            var vm = await _transgression.GetWarningWizardAsync(id);
            if (vm.HasAccess == false)
                return Forbid();
            return View(vm);
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> ExportOverviewPdf(long warningId)
        {
            WarningWizardVm model = await _transgression.GetWarningWizardAsync(warningId);

            var bytes = await _pdf.ExportOverviewPdfAsync(model);

            return File(bytes, "application/pdf", $"Warning_{warningId}_Overview.pdf");
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransgression(CreateTransgressionDTO dto)
        {
            var result = await _transgression.SaveIssueStepAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            if (dto.WarningId.HasValue && dto.WarningId.Value > 0)
            {
                return Json(new
                {
                    success = true,
                    warningId = result.WarningId
                });
            }

            return RedirectToAction("Index", new { id = result.WarningId });
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerDto dto)
        {
            if (dto.WarningId is 0)
                return BadRequest(new { success = false, message = "WarningId is required." });

            if (dto.QuestionId is 0)
                return BadRequest(new { success = false, message = "QuestionId is required." });

            await _transgression.SaveAnswerAsync(dto);

            return Ok(new { success = true });
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> SaveAnswers([FromBody] List<SaveAnswerDto> answers)
        {
            if (answers is null || answers.Count is 0)
            {
               return BadRequest(new { success = false, message = "No answers provided." });
            }
               

            var warningId = answers.First().WarningId;
            if (warningId is 0 || answers.Any(a => a.WarningId != warningId))
            {
              return BadRequest(new { success = false, message = "All answers must have the same valid WarningId." });
            }

            foreach (var answer in answers)
            {
                await _transgression.SaveAnswerAsync(answer);
            }
            return Ok(new { success = true });
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> SavePreviousOffences(SavePreviousOffencesDto dto)
        {
            string type = "PreviousOffences";
            if (dto.HasPreviousOffences)
            {
                foreach (var file in dto.Files)
                {
                    if (file is null || file.Length is 0)
                    {
                        continue;
                    }

                    await _fileStorage.UploadWarningFileAsync(dto.WarningId, type, file);
                }
            }

            return Ok(new { success = true });
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> DeleteAttachment(long warningId, string fileName)
        {
            await _fileStorage.DeleteFileAsync(warningId, fileName);
            return Ok(new { success = true });
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(int id, string fileName)
        {
            try
            {
                var file = await _fileStorage.GetFileAsync(id, fileName);
                return File(file.Stream, file.ContentType, fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> SaveEvidenceAttachment([FromForm] SaveAttachmentsDto dto)
        {
            if (dto.WarningId is 0)
            {
               return BadRequest();
            }
               

            if (dto.Files is null || dto.Files.Count is 0)
            {
                return BadRequest(new { success = false, message = "No files selected." });
            }
                

            foreach (var file in dto.Files)
            {
                if (file is null || file.Length is 0)
                { 
                  continue;
                }
                   

                var mediaType = _fileStorage.GetAttachmentType(file);

                var originalName = System.IO.Path.GetFileNameWithoutExtension(file.FileName);
                var ext = System.IO.Path.GetExtension(file.FileName);

                var baseName = !string.IsNullOrWhiteSpace(dto.AttachmentName)
                    ? dto.AttachmentName.Trim()
                    : originalName;

                var safeOriginalName = string.Concat(
                    baseName.Where(c => !System.IO.Path.GetInvalidFileNameChars().Contains(c))
                ).Trim();

                if (string.IsNullOrWhiteSpace(safeOriginalName))
                    safeOriginalName = "File";

                var storagePrefix = $"Evidence_{safeOriginalName}";
                var storedFileName = await _fileStorage.UploadWarningFileAsync(dto.WarningId, storagePrefix, file);

                await _transgression.SaveEvidenceAsync(
                    dto.WarningId,
                    storedFileName,
                    mediaType,
                    file.Length,
                    dto.Notes,
                    1,
                    false
                );
            }

            return Ok(new { success = true });
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> SaveAdditionalInfo([FromBody] SaveAdditionalInfoDto dto)
        {
            await _transgression.SaveEvidenceAsync(dto.WarningId, null, null, null, dto.AdditionalInfoHtml, 1, false);// Submitted to Legal
            return Ok(new { success = true });
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateWarningStatusDto dto)
        {
            if (dto is null || dto.WarningId is 0)
                return BadRequest(new { success = false, message = "WarningId missing." });

            if (string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { success = false, message = "Status missing." });

            DateOnly? due = null;

            if (!string.IsNullOrWhiteSpace(dto.DueDate))
            {
                if (!DateOnly.TryParse(dto.DueDate, out var parsed))
                    return BadRequest(new { success = false, message = "Invalid DueDate format. Use yyyy-MM-dd." });

                due = parsed;
            }
            await _transgression.SendEmailToLegalAsync(dto.WarningId, due, dto.Status);
            await _transgression.UpdateWarningStatusAsync(dto.WarningId, due, dto.Status);

            return Ok(new { success = true });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> GetOverview(long warningId)
        {
            var vm = await _transgression.GetWarningWizardAsync(warningId);
            return PartialView("_WarningOverview", vm);
        }

        [AuthorizeForScopes(Scopes = new[] { "User.Read.All" })]

        [Authorize(Roles = "User")]
        public async Task<IActionResult> TeamLeadWarning(long id)
        {
            var vm = await _transgression.GetWarningLegalAsync(id);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> AddTeamLeadEvidence(AddTeamLeadEvidenceVm model)
        {
            var hasFiles = model.Files != null && model.Files.Any(x => x.Length > 0);
            var hasNote = !string.IsNullOrWhiteSpace(model.NoteText);

            if (!hasFiles && !hasNote)
            {
                TempData["Error"] = "Please add at least one document or note.";
                return RedirectToAction(nameof(TeamLeadWarning), new { id = model.WarningId });
            }

            await _transgression.AddTeamLeadEvidenceAsync(model);

            TempData["Success"] = "Additional information was saved successfully.";

            return RedirectToAction(nameof(TeamLeadWarning), new { id = model.WarningId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NotifyLegalIssueCompleted([FromBody] NotifyLegalIssueCompletedDto dto)
        {
            if (dto is null || dto.WarningId is 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid warning id."
                });
            }

           
            await _transgression.NotifyLegalIssueCompletedAsync(dto.WarningId);

            return Ok(new
            {
                success = true,
                message = "Legal notified successfully."
            });
          
        }
    }
}