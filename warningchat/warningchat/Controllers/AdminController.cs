using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using System.Diagnostics;
using WarningSystems.Core;
using WarningSystems.Core.Manager;
using WarningSystems.Models.DTO;

namespace WarningSystems.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITransgressionManager _transgression;

        public AdminController(ILogger<HomeController> logger, ITransgressionManager transgression)
        {
            _logger = logger;
            _transgression = transgression;
        }

        [Authorize(Roles = "Admin")]
        [AuthorizeForScopes(Scopes = new[] { "User.Read.All" })]
        public async Task<IActionResult> Index()
        {
            var vm = await _transgression.BuildAdminView();
            return View(vm);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion(CreateQuestionDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please check the question details and try again.";
                return RedirectToAction("Index");
            }

            try
            {
                await _transgression.CreateQuestionAsync(dto);

                TempData["SuccessMessage"] = "Question created successfully.";
                return RedirectToAction("Index");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question");

                TempData["ErrorMessage"] = "Something went wrong while creating the question. Please try again or contact support.";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(UpdateQuestionDto dto)
        {
            if (ModelState.IsValid)
            {
                await _transgression.UpdateQuestionAsync(dto);
                return RedirectToAction("Index");
            }
            return BadRequest(ModelState);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CreateCategoryDto dto)
        {
            if (ModelState.IsValid)
            {
                await _transgression.CreateCategoryAsync(dto);
                return RedirectToAction("Index");
            }
            return BadRequest(ModelState);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] ="Please check the category details.";
                return RedirectToAction(nameof(Index));
            }
            await _transgression.UpdateCategoryAsync(dto);
            TempData["SuccessMessage"] = "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAvailableQuestions(int categoryId)
        {
            var data = await _transgression.GetAvailableQuestionsAsync(categoryId);
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetLinkedQuestions(int categoryId)
        {
            var data = await _transgression.GetLinkedQuestionsAsync(categoryId);
            return Json(data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SetCategoryQuestionLink(
     [FromBody] SetCategoryQuestionLinkDto dto)
        {
            await _transgression.SetCategoryQuestionLinkAsync(dto);

            return Ok(new
            {
                success = true
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}