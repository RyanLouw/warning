using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics;
using System.Security.Claims;
using WarningSystems.Core;
using WarningSystems.Core.Manager;
using WarningSystems.Core.Models.Enum;
using WarningSystems.Models.DTO;

namespace WarningSystems.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITransgressionManager _transgression;

        public HomeController(
         ILogger<HomeController> logger,
         ITransgressionManager transgression)
        {
            _logger = logger;
            _transgression = transgression;
        }

        [AuthorizeForScopes(Scopes = new[] { "User.Read.All" })]
        public async Task<IActionResult> Index()
        {
            var vm = await _transgression.BuildTaskListAsync();

            return View(vm);
        }

        [Authorize(Roles = "User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAbsenceDiscussion(
            [FromBody] CreateAbsenceDiscussionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Please complete all required fields." });

            var result = await _transgression.CreateAbsenceDiscussionAsync(dto);

            return result.Success
                ? Ok(new { success = true, warningId = result.WarningId })
                : BadRequest(new { success = false, message = result.Message });
        }

        public async Task<IActionResult> Redirect(int warningId, string status)
        {
            var target = await _transgression.RedirectPicker(status);

            return target switch
            {
                WarningRedirectTarget.TransgressionIndex =>
                    RedirectToAction("Index", "Transgression", new { id = warningId }),

                WarningRedirectTarget.TransgressionTeamLeadWarning =>
                    RedirectToAction("TeamLeadWarning", "Transgression", new { id = warningId }),

                WarningRedirectTarget.LegalIndex =>
                    RedirectToAction("Index", "Legal", new { id = warningId }),

                _ =>
                    RedirectToAction("Index", "Home")
            };
        }

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EmployeeDashboard(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
                return BadRequest();

            var model = await _transgression.GetEmployeeDashboardAsync(employeeId);

            if (model == null)
                return NotFound();

            if (!model.HasAccess)
                return Forbid();

            return PartialView("_EmployeeDashboardModal", model);
        }

        [AllowAnonymous]
        public IActionResult DebugRoles()
        {
            var result = new
            {
                Name = User.Identity?.Name,
                Authenticated = User.Identity?.IsAuthenticated,
                IsInRoleLegal = User.IsInRole("Legal"),
                IsInRoleAdmin = User.IsInRole("Admin"),
                RoleClaims = User.Claims
                    .Where(c =>
                        c.Type == ClaimTypes.Role ||
                        c.Type == "role" ||
                        c.Type == "roles" ||
                        c.Type.Contains("role"))
                    .Select(c => new { c.Type, c.Value })
                    .ToList(),
                AllClaims = User.Claims
                    .Select(c => new { c.Type, c.Value })
                    .OrderBy(c => c.Type)
                    .ToList()
            };

            return Json(result);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
