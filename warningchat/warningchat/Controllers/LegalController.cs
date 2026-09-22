using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics;
using WarningSystems.core.Models.DTO;
using WarningSystems.Core;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess.Context.Entities;
using WarningSystems.Core.Manager;
using WarningSystems.Core.Models.Enum;
using WarningSystems.Core.ViewModels;
using WarningSystems.Models.DTO;

namespace WarningSystems.Controllers
{
    [Authorize]
    public class LegalController : Controller
    {
        private const long MaximumEmailAttachmentBytes = 3 * 1024 * 1024;
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

            if (vm.IssueStatusGroup == IssueStatusGroup.Draft)
                return Forbid();

            if (vm.IssueStatusGroup == IssueStatusGroup.LegalReview)
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
            var savedFile = false;
            foreach (var file in dto.Files)
            {
                if (file is null || file.Length is 0)
                {
                    continue;
                }

                var Mediatype = _fileStorageService.GetAttachmentType(file);
                var type = "LegalEvidence";

                var fileName = await _fileStorageService.UploadWarningFileAsync(dto.WarningId, type, file);
                var notes = savedFile ? null : dto.Notes;
                await _transgressionManager.SaveEvidenceAsync(dto.WarningId, fileName, Mediatype, file.Length, notes, 3, true);
                savedFile = true;
            }

            if (!savedFile)
            {
                if (string.IsNullOrWhiteSpace(dto.Notes))
                    return BadRequest(new { success = false, message = "Add a file, a note, or both." });

                await _transgressionManager.SaveEvidenceAsync(
                    dto.WarningId,
                    null,
                    null,
                    null,
                    dto.Notes,
                    3,
                    true);
            }

            return Ok(new { success = true });
        }




        [Authorize(Roles = "Legal")]
        [HttpPost]
        public async Task<IActionResult> UpdateIssueDetails([FromBody] UpdateLegalIssueDetailsDto dto)
        {
            if (dto is null || !ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid update request." });

            var changedBy = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(changedBy))
                return Unauthorized(new { success = false, message = "Unable to determine the logged-in user." });

            var result = await _transgressionManager.UpdateLegalIssueDetailsAsync(dto, changedBy);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

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
            if (dto.IssueTypeId <= 0) return BadRequest("Issue type required.");

            try
            {
                if (dto.File is not null && dto.File.Length is > 0)
                {
                    var mediaType = _fileStorageService.GetAttachmentType(dto.File);
                    var attachmentType = "LegalDecision";

                    var fileName = await _fileStorageService.UploadWarningFileAsync(
                        dto.WarningId, attachmentType, dto.File);

                    await _transgressionManager.SaveEvidenceAsync(
                        dto.WarningId, fileName, mediaType, dto.File.Length, null, 3, true);
                }

                await _transgressionManager.ApplyLegalDecisionAsync(
                    dto.WarningId, dto.IssueTypeId, dto.IssueSubTypeId);
                await _transgressionManager.SendEmailToTeamLeadAsync(dto);

                return Ok(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Legal")]
        [HttpPost]
        public async Task<IActionResult> ValidateTransgression(
            [FromBody] NotifyLegalIssueCompletedDto dto)
        {
            if (dto is null || dto.WarningId is 0)
                return BadRequest(new { success = false, message = "Invalid warning id." });

            try
            {
                await _transgressionManager.ValidateWarningAsync(dto.WarningId);
                return Ok(new { success = true, status = "Validated" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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
        public async Task<IActionResult> SetTeamLeadVisibility([FromBody] SetTeamLeadVisibilityDto dto)
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
                await _transgressionManager.SetHideFromTeamLeadAsync(dto.WarningId, dto.HideFromTeamLead, changedBy);

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

        [Authorize(Roles = "Legal")]
        [AuthorizeForScopes(Scopes = new[] { "User.Read.All", "Mail.Send" })]
        [HttpPost]
        public async Task<IActionResult> RequestTeamLeadInformation(
           [FromForm] RequestTeamLeadInformationVm request)
        {
            if (request.WarningId <= 0)
                return BadRequest(new { success = false, message = "A valid warning id is required." });

            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { success = false, message = "Please add a message for the team lead." });

            if (request.Message.Trim().Length > 1000)
                return BadRequest(new { success = false, message = "The message cannot be longer than 1,000 characters." });

            if (request.File is { Length: > MaximumEmailAttachmentBytes })
            {
                return BadRequest(new
                {
                    success = false,
                    message = "The supporting document must be 3 MB or smaller so it can be attached to the email."
                });
            }

            try
            {
               
                await _transgressionManager.SendMoreInformationRequiredEmailAsync(
                    request.WarningId,
                    request.Message.Trim(),
                    request.File);

                return Ok(new { success = true, message = "The request was sent to the team lead." });
            }
            catch (MicrosoftIdentityWebChallengeUserException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to request more information from the team lead for warning {WarningId}.",
                    request.WarningId);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "The email could not be sent. Please try again or contact support if the problem continues."
                });
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
