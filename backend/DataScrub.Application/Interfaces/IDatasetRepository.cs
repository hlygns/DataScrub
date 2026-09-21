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
        Task<List<DetectedIssue>> GetIssuesByIdsAsync(IEnumerable<Guid> issueIds);
        Task UpdateIssueAsync(DetectedIssue issue);
        Task UpdateIssuesAsync(IEnumerable<DetectedIssue> issues);
        Task<List<DetectedIssue>> GetApprovedIssuesAsync(Guid datasetId);
        Task<List<Dataset>> GetAllWithIssuesAsync();
     
    }

}
