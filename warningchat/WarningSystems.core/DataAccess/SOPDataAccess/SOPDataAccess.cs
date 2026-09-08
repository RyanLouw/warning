using Microsoft.EntityFrameworkCore;
using WarningSystems.Core.DataAccess.SOPDataAccess.Context.Entity;

namespace WarningSystems.Core.DataAccess.SOPDataAccess;

public class SOPDataAccess : ISOPDataAccess
{
    private readonly SopDbContext _context;

    public SOPDataAccess(SopDbContext context)
    {
        _context = context;
    }

    public async Task<List<SopDocument>> GetActiveDocumentsAsync()
    {
        return await _context.SopDocuments
            .AsNoTracking()
            .Where(d => d.Status != 2)
            .OrderBy(d => d.SOPDocumentCategoryId)
            .ThenBy(d => d.DocumentName)
            .Select(d => new SopDocument
            {
                SOPDocumentId = d.SOPDocumentId,
                DocumentName = d.DocumentName,
                SOPDocumentCategoryId = d.SOPDocumentCategoryId
            })
            .ToListAsync();
    }
}