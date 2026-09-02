using WarningSystems.Core.DataAccess.SOPDataAccess.Context.Entity;

namespace WarningSystems.Core.DataAccess.SOPDataAccess;

public interface ISOPDataAccess
{
    public Task<List<SopDocument>> GetActiveDocumentsAsync();
}