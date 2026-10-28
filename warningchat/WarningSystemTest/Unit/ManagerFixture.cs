using Microsoft.Extensions.Options;
using FakeItEasy;
using WarningSystems.core.Models.DTO;
using WarningSystems.Core.Auth.Interface;
using WarningSystems.Core.DataAccess.AzureFileStorage;
using WarningSystems.Core.DataAccess.GraphDataAccess;
using WarningSystems.Core.DataAccess.SOPDataAccess;
using WarningSystems.Core.DataAccess.WarningSystemDataAccess;
using WarningSystems.Core.Manager;
using WarningSystems.Core.Services;
using WarningSystems.Core.Services.Interface;
using WarningSystems.Core.ViewModels;

namespace WarningSystemTest.Unit;

internal sealed class ManagerFixture
{
    public IWarningSystemDataAccess Data { get; } = A.Fake<IWarningSystemDataAccess>(options => options.Strict());
    public IGraphUserDataAccess Graph { get; } = A.Fake<IGraphUserDataAccess>(options => options.Strict());
    public ICurrentUserAccessor Me { get; } = A.Fake<ICurrentUserAccessor>();
    public ISOPDataAccess Sop { get; } = A.Fake<ISOPDataAccess>(options => options.Strict());
    public IAzureFileStorageDataAccess Storage { get; } = A.Fake<IAzureFileStorageDataAccess>(options => options.Strict());
    public IUserRoleService Roles { get; } = A.Fake<IUserRoleService>(options => options.Strict());
    public EmailSettings EmailSettings { get; } = new();
    public TransgressionManager Manager { get; }

    public ManagerFixture()
    {
        A.CallTo(() => Me.ObjectId).Returns("actor");
        A.CallTo(() => Me.IsAuthenticated).Returns(true);
        Manager = new(Data, Graph, Me, Sop, Storage, Roles,
            Options.Create(EmailSettings), Options.Create(new ApplicationOptions { BaseUrl = "https://example.test" }),
            new EmbeddedEmailTemplateRenderer());
    }
}
