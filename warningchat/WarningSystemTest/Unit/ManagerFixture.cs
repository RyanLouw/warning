using Microsoft.Extensions.Options;
using Moq;
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
    public Mock<IWarningSystemDataAccess> Data { get; } = new(MockBehavior.Strict);
    public Mock<IGraphUserDataAccess> Graph { get; } = new(MockBehavior.Strict);
    public Mock<ICurrentUserAccessor> Me { get; } = new();
    public Mock<ISOPDataAccess> Sop { get; } = new(MockBehavior.Strict);
    public Mock<IAzureFileStorageDataAccess> Storage { get; } = new(MockBehavior.Strict);
    public Mock<IUserRoleService> Roles { get; } = new(MockBehavior.Strict);
    public EmailSettings EmailSettings { get; } = new();
    public TransgressionManager Manager { get; }

    public ManagerFixture()
    {
        Me.SetupGet(x => x.ObjectId).Returns("actor");
        Me.SetupGet(x => x.IsAuthenticated).Returns(true);
        Manager = new(Data.Object, Graph.Object, Me.Object, Sop.Object, Storage.Object, Roles.Object,
            Options.Create(EmailSettings), Options.Create(new ApplicationOptions { BaseUrl = "https://example.test" }),
            new EmbeddedEmailTemplateRenderer());
    }
}
