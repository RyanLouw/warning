using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using WarningSystems.Core.Auth;
using WarningSystems.Core.Auth.Interface;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.Services;
using WarningSystems.Core.Services.Interface;

namespace WarningSystemTest.Unit;

[Trait("Category", "Unit")]
public class AuthenticationTests
{
    [Fact]
    public void Missing_context_is_anonymous()
    {
        var accessor = new CurrentUserAccessor(new HttpContextAccessor());
        Assert.Null(accessor.Principal);
        Assert.False(accessor.IsAuthenticated);
        Assert.Null(accessor.UpnOrEmail);
        Assert.Null(accessor.DisplayName);
        Assert.Null(accessor.ObjectId);
        Assert.Equal("unknown", accessor.AuditUser);
        Assert.Throws<InvalidOperationException>(() => accessor.RequireAuditUser());
    }

    [Theory]
    [InlineData("preferred_username")]
    [InlineData(ClaimTypes.Upn)]
    [InlineData(ClaimTypes.Email)]
    [InlineData(ClaimTypes.Name)]
    public void Email_claim_fallbacks(string claimType)
    {
        var accessor = Accessor([new(claimType, "person@example.test")]);
        Assert.Equal("person@example.test", accessor.UpnOrEmail);
        Assert.Equal("person@example.test", accessor.RequireAuditUser());
    }

    [Theory]
    [InlineData("oid")]
    [InlineData("http://schemas.microsoft.com/identity/claims/objectidentifier")]
    [InlineData(ClaimTypes.NameIdentifier)]
    [InlineData("sub")]
    public void Object_id_claim_fallbacks(string claimType)
    {
        var accessor = Accessor([new(claimType, "object-id")]);
        Assert.Equal("object-id", accessor.ObjectId);
        Assert.Equal("object-id", accessor.AuditUser);
    }

    [Fact]
    public void Preferred_claims_take_precedence()
    {
        var accessor = Accessor([new("preferred_username", "preferred"), new(ClaimTypes.Email, "email"),
            new("oid", "oid"), new("sub", "sub"), new("name", "Display"), new(ClaimTypes.Name, "Fallback")]);
        Assert.Equal("preferred", accessor.UpnOrEmail);
        Assert.Equal("oid", accessor.ObjectId);
        Assert.Equal("Display", accessor.DisplayName);
    }

    [Fact]
    public void Display_name_falls_back_to_email() =>
        Assert.Equal("email", Accessor([new(ClaimTypes.Email, "email")]).DisplayName);

    [Theory]
    [InlineData("App.Admin", "Admin")]
    [InlineData("App.Legal", "Legal")]
    [InlineData("App.Chairperson", "Chairperson")]
    [InlineData("App.User", "User")]
    public async Task App_roles_are_normalised_idempotently(string source, string expected)
    {
        var principal = Accessor([new(ClaimTypes.Role, source)]).Principal!;
        var transform = new AppRoleClaimsTransformation();
        await transform.TransformAsync(principal);
        await transform.TransformAsync(principal);
        Assert.True(principal.IsInRole(expected));
        Assert.Single(principal.FindAll(ClaimTypes.Role), c => c.Value == expected);
    }

    [Fact]
    public async Task Anonymous_roles_are_not_added()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "App.Admin")]));
        await new AppRoleClaimsTransformation().TransformAsync(principal);
        Assert.DoesNotContain(principal.Claims, c => c.Value == "Admin");
    }

    [Fact]
    public async Task Group_roles_respect_exclusions_and_case_insensitive_group_ids()
    {
        var me = new Mock<ICurrentUserAccessor>();
        me.SetupGet(x => x.IsAuthenticated).Returns(true);
        me.SetupGet(x => x.ObjectId).Returns("me");
        var graph = new Mock<IGraphUserDataAccess>(MockBehavior.Strict);
        graph.Setup(x => x.GetMatchedGroupIdsAsync("me", It.IsAny<IEnumerable<string>>())).ReturnsAsync(new[] { "GROUP-A", "group-b" });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Roles:Legal"] = "group-a", ["Roles:Admin"] = "group-b", ["Roles:User"] = "group-c", ["RoleExclusions:0"] = "ADMIN"
        }).Build();
        var service = new UserRoleService(configuration, me.Object, graph.Object);
        Assert.Equal(new[] { "Legal" }, await service.GetCurrentUserRolesAsync());
        Assert.True(await service.CurrentUserIsInRoleAsync("LEGAL"));
        Assert.False(await service.CurrentUserIsInRoleAsync("Admin"));
    }

    [Theory]
    [InlineData(false, "me")]
    [InlineData(true, null)]
    [InlineData(true, " ")]
    public async Task Anonymous_or_missing_object_id_never_queries_Graph(bool authenticated, string? id)
    {
        var me = new Mock<ICurrentUserAccessor>();
        me.SetupGet(x => x.IsAuthenticated).Returns(authenticated);
        me.SetupGet(x => x.ObjectId).Returns(id);
        var graph = new Mock<IGraphUserDataAccess>(MockBehavior.Strict);
        var service = new UserRoleService(new ConfigurationBuilder().Build(), me.Object, graph.Object);
        Assert.Empty(await service.GetCurrentUserRolesAsync());
        graph.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Group_claim_transformation_is_idempotent()
    {
        var roles = new Mock<IUserRoleService>();
        roles.Setup(x => x.GetCurrentUserRolesAsync()).ReturnsAsync(new[] { "Legal", "Legal", "User" });
        var principal = Accessor([new(ClaimTypes.Role, "Legal")]).Principal!;
        var transform = new RoleClaimsTransformation(roles.Object);
        await transform.TransformAsync(principal);
        await transform.TransformAsync(principal);
        Assert.Equal(2, principal.FindAll(ClaimTypes.Role).Count());
    }

    private static CurrentUserAccessor Accessor(Claim[] claims) => new(new HttpContextAccessor {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "UnitTest")) }
    });
}
