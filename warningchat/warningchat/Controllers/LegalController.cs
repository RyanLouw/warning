using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics;
using WarningSystems.core.Models.DTO;
using WarningSystems.Core;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.Manager;
using WarningSystems.Core.ViewModels;
using WarningSystems.Models.DTO;

namespace WarningSystems.Controllers
{
    [Authorize]
    public class LegalController : Controller
    {
        private readonly ILogger<LegalController> _logger;
        private readonly ITransgressionManager _transgressionManager;
        private readonly IAzureFileStorageDataAccess _fileStorageService;

        public LegalController(ILogger<LegalController> logger, ITransgressionManager trangretion, IAzureFileStorageDataAccess IFileStorageService)
        {
            _logger = logger;
            _transgressionManager = trangretion;
            _fileStorageService = IFileStorageService;
        }

        [Authorize(Roles = "Legal")]
        [AuthorizeForScopes(Scopes = new[] { "User.Read.All" })]
        public async Task<IActionResult> Index(long id)
        {
            LegalWizardVm? vm;

            try
            {
                vm = await _transgressionManager.GetWarningLegalAsync(id);
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
            {
                return Challenge(
                    new AuthenticationProperties
                    {
                        RedirectUri = Url.Action("Index", "Legal", new { id })
                    },
                    OpenIdConnectDefaults.AuthenticationScheme);
            }

            if (vm == null)
                return NotFound();

            if (string.Equals(vm.Status, "Draft", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            if (string.Equals(vm.Status, "New", StringComparison.OrdinalIgnoreCase))
            {
                await _transgressionManager.MarkWarningInProgressAsync(id);

                try
                {
                    vm = await _transgressionManager.GetWarningLegalAsync(id);
                }
                catch (MicrosoftIdentityWebChallengeUserException)
                {
                    return Challenge(
                        new AuthenticationProperties
                        {
                            RedirectUri = Url.Action("Index", "Legal", new { id })
                        },
                        OpenIdConnectDefaults.AuthenticationScheme);
                }

                if (vm == null)
                    return NotFound();
            }

            return View(vm);
        }

        [Authorize(Roles = "Legal")]
        [HttpPost]
        public async Task<IActionResult> SaveEvidenceAttachment(SaveAttachmentsDto dto)
        {
            if (dto.WarningId is 0) return BadRequest();

            foreach (var file in dto.Files)
            {
                if (file is null || file.Length is 0)
                {
                    continue;
                }

                var Mediatype = _fileStorageService.GetAttachmentType(file);
                var type = "LegalEvidence";

                var fileName = await _fileStorageService.UploadWarningFileAsync(dto.WarningId, type, file);
                await _transgressionManager.SaveEvidenceAsync(dto.WarningId, fileName, Mediatype, file.Length, dto.Notes, 3, true);
            }

            return Ok(new { success = true });
        }

        [Authorize(Roles = "Legal")]
        [HttpPost]
        public async Task<IActionResult> AddEvidenceNote([FromBody] AddEvidenceNoteDto dto)
        {
            if (dto == null)
                return BadRequest("Invalid request.");

            if (dto.WarningId is 0)
                return BadRequest("Missing warning id.");

            if (dto.NoteTypeId is 0)
                return BadRequest("Please select a note type.");

            if (string.IsNullOrWhiteSpace(dto.NoteText))
                return BadRequest("Note is required.");

            await _transgressionManager.AddWarningNoteAsync(
                dto.WarningId,
                dto.EvidenceId,
                dto.NoteTypeId,
                dto.NoteText);

            return Ok(new { success = true });
        }

        [Authorize(Roles = "Legal")]
        [HttpGet]
        public async Task<IActionResult> Download(int warningId, string fileName)
        {
            try
            {
                var stream = await _fileStorageService.GetFileStreamAsync(warningId, fileName);

                return File(
                    stream,
                    "application/octet-stream",
                    fileName
                );
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }

        [Authorize(Roles = "Legal")]
        [HttpPost]
        public async Task<IActionResult> CompleteTransgression([FromForm] CompleteTransgressionDto dto)
        {
            if (dto.WarningId is 0) return BadRequest("Invalid warningId.");
            if (string.IsNullOrWhiteSpace(dto.Status)) return BadRequest("Status required.");

            if (dto.File is not  null && dto.File.Length is > 0)
            {
                var mediaType = _fileStorageService.GetAttachmentType(dto.File);
                var type = "LegalComplete";

                var fileName = await _fileStorageService.UploadWarningFileAsync(dto.WarningId, type, dto.File);
                await _transgressionManager.SendEmailToTeamLeadAsync(dto);

                await _transgressionManager.SaveEvidenceAsync(
                    dto.WarningId,
                    fileName,
                    mediaType,
                    dto.File.Length,
                    null, 3, true
                );
            }

            await _transgressionManager.UpdateWarningStatusAsync(dto.WarningId, null, dto.Status);

            return Ok(new { success = true });
        }

        [Authorize(Roles = "Legal")]
        [HttpPost]
        public async Task<IActionResult> UpdateDueDate([FromBody] UpdateDueDateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var changedBy = User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(changedBy))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Unable to determine the logged-in user."
                });
            }

            var result = await _transgressionManager.UpdateDueDateWithAuditAsync(
                dto,
                changedBy);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new { success = true });
        }




        [HttpPost]
        [Authorize(Roles = "Legal")]
        public async Task<IActionResult> SetTeamLeadVisibility(
    [FromBody] SetTeamLeadVisibilityDto dto)
        {
            if (dto == null || dto.WarningId <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "A valid WarningId is required."
                });
            }

            var changedBy =
                User.Identity?.Name ?? "Unknown";

            var result =
                await _transgressionManager
                    .SetHideFromTeamLeadAsync(
                        dto.WarningId,
                        dto.HideFromTeamLead,
                        changedBy
                    );

            if (!result.HasValue)
            {
                return NotFound(new
                {
                    success = false,
                    message = "The warning could not be found."
                });
            }

            return Ok(new
            {
                success = true,
                hideFromTeamLead = result.Value,
                message = result.Value
                    ? "The issue is now hidden from Team Leads."
                    : "The issue is now visible to Team Leads."
            });
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}