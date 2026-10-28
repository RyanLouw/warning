using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using FakeItEasy;
using WarningSystems.Controllers;
using WarningSystems.Core.Manager;
using WarningSystems.Core.ViewModels;
using WarningSystems.Models.DTO;
using WarningSystems.Models.Validation;

namespace WarningSystemTest.Unit;

[Trait("Category", "Unit")]
public class HomeControllerTests
{
    private readonly ITransgressionManager _manager = A.Fake<ITransgressionManager>(options => options.Strict());
    private HomeController Create(params string[] roles) => new(NullLogger<HomeController>.Instance, _manager)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "UnitTest"))
        } }
    };

    [Theory]
    [InlineData("User", "Draft", "Transgression", "Index")]
    [InlineData("User", " draft ", "Transgression", "Index")]
    [InlineData("User", "New", "Transgression", "TeamLeadWarning")]
    [InlineData("User", "Completed", "Transgression", "TeamLeadWarning")]
    [InlineData("Legal", "New", "Legal", "Index")]
    [InlineData("Legal", "Completed", "Legal", "Index")]
    [InlineData("Legal", "Draft", "Home", "Index")]
    [InlineData("Admin", "New", "Home", "Index")]
    public void Email_links_redirect_by_role_and_status(string role, string status, string controller, string action)
    {
        var result = Assert.IsType<RedirectToActionResult>(Create(role).Redirect(42, status));
        Assert.Equal(controller, result.ControllerName);
        Assert.Equal(action, result.ActionName);
        if (controller != "Home") Assert.Equal(42, result.RouteValues!["id"]);
    }

    [Fact]
    public void Legal_and_user_draft_redirects_to_wizard()
    {
        var result = Assert.IsType<RedirectToActionResult>(Create("User", "Legal").Redirect(42, "Draft"));
        Assert.Equal("Transgression", result.ControllerName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Invalid_email_link_returns_home(int id) =>
        Assert.Equal("Home", Assert.IsType<RedirectToActionResult>(Create("User").Redirect(id, "Draft")).ControllerName);

    [Fact]
    public async Task Index_renders_manager_model()
    {
        var model = new TaskListVM();
        A.CallTo(() => _manager.BuildTaskListAsync()).Returns(model);
        Assert.Same(model, Assert.IsType<ViewResult>(await Create().Index()).Model);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Employee_dashboard_requires_id(string id) =>
        Assert.IsType<BadRequestResult>(await Create().EmployeeDashboard(id));

    [Fact]
    public async Task Employee_dashboard_checks_access()
    {
        A.CallTo(() => _manager.GetEmployeeDashboardAsync("employee")).Returns(new EmployeeDashboardVm { HasAccess = false });
        Assert.IsType<ForbidResult>(await Create().EmployeeDashboard("employee"));
    }

    [Fact]
    public async Task Employee_dashboard_renders_authorised_data()
    {
        var model = new EmployeeDashboardVm { HasAccess = true };
        A.CallTo(() => _manager.GetEmployeeDashboardAsync("employee")).Returns(model);
        var result = Assert.IsType<PartialViewResult>(await Create().EmployeeDashboard("employee"));
        Assert.Equal("_EmployeeDashboardModal", result.ViewName);
        Assert.Same(model, result.Model);
    }

    [Fact]
    public async Task Discussion_invalid_model_is_rejected_before_manager()
    {
        var controller = Create("User");
        controller.ModelState.AddModelError("EmployeeId", "Required");
        Assert.IsType<BadRequestObjectResult>(await controller.CreateAbsenceDiscussion(new()));
        Assert.Empty(Fake.GetCalls(_manager));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Discussion_returns_status_matching_result(bool success)
    {
        var dto = new CreateAbsenceDiscussionDto();
        A.CallTo(() => _manager.CreateAbsenceDiscussionAsync(dto)).Returns(success ? SaveIssueStepResult.Ok(42) : SaveIssueStepResult.Fail("Rejected"));
        var result = await Create("User").CreateAbsenceDiscussion(dto);
        if (success) Assert.IsType<OkObjectResult>(result);
        else Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Error_contains_request_id()
    {
        var controller = Create();
        controller.HttpContext.TraceIdentifier = "test-trace";
        var model = Assert.IsType<WarningSystems.Core.ErrorViewModel>(Assert.IsType<ViewResult>(controller.Error()).Model);
        Assert.Equal("test-trace", model.RequestId);
        Assert.True(model.ShowRequestId);
    }
}
