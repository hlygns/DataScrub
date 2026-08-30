using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataScrub.Domain.Entities;

namespace DataScrub.Application.Interfaces
{
    public interface IDatasetRepository
    {
        Task<Dataset?> GetByIdAsync(Guid id);
        Task<Dataset?> GetByIdWithIssuesAsync(Guid id);
        Task<Dataset> AddAsync(Dataset dataset);
        Task UpdateAsync(Dataset dataset);
        Task AddIssuesAsync(IEnumerable<DetectedIssue> issues);
        Task<DetectedIssue?> GetIssueByIdAsync(Guid issueId);
        Task UpdateIssueAsync(DetectedIssue issue);
        Task<List<DetectedIssue>> GetApprovedIssuesAsync(Guid datasetId);
    }
}
