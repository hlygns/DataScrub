using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataScrub.Application.Interfaces;
using DataScrub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataScrub.Infrastructure.Data
{
    public class DatasetRepository : IDatasetRepository
    {
        private readonly ApplicationDbContext _context;

        public DatasetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Dataset?> GetByIdAsync(Guid id) =>
            await _context.Datasets.FirstOrDefaultAsync(d => d.Id == id);

        // Stajında öğrendiğin N+1 dersini burada uyguluyoruz: Issues'ı ayrı ayrı
        // sorgulamak yerine tek sorguda Include ile getiriyoruz.
        public async Task<Dataset?> GetByIdWithIssuesAsync(Guid id) =>
            await _context.Datasets
                .Include(d => d.Issues)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task<Dataset> AddAsync(Dataset dataset)
        {
            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync();
            return dataset;
        }

        public async Task UpdateAsync(Dataset dataset)
        {
            _context.Datasets.Update(dataset);
            await _context.SaveChangesAsync();
        }

        public async Task AddIssuesAsync(IEnumerable<DetectedIssue> issues)
        {
            _context.DetectedIssues.AddRange(issues);
            await _context.SaveChangesAsync();
        }

        public async Task<DetectedIssue?> GetIssueByIdAsync(Guid issueId) =>
            await _context.DetectedIssues.FirstOrDefaultAsync(i => i.Id == issueId);

        // Toplu onay için: N tane Id'yi tek sorguda çekiyoruz (Id başına ayrı sorgu = N+1).
        public async Task<List<DetectedIssue>> GetIssuesByIdsAsync(IEnumerable<Guid> issueIds)
        {
            var ids = issueIds.ToList();
            if (ids.Count == 0) return new List<DetectedIssue>();

            return await _context.DetectedIssues
                .Where(i => ids.Contains(i.Id))
                .ToListAsync();
        }

        public async Task UpdateIssueAsync(DetectedIssue issue)
        {
            _context.DetectedIssues.Update(issue);
            await _context.SaveChangesAsync();
        }

        // Tek SaveChanges: ya hepsi yazılır ya hiçbiri. Kayıt başına round-trip yok.
        public async Task UpdateIssuesAsync(IEnumerable<DetectedIssue> issues)
        {
            _context.DetectedIssues.UpdateRange(issues);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DetectedIssue>> GetApprovedIssuesAsync(Guid datasetId) =>
            await _context.DetectedIssues
                .Where(i => i.DatasetId == datasetId && i.Resolution == ResolutionStatus.Approved)
                .ToListAsync();
        public async Task<List<Dataset>> GetAllWithIssuesAsync() =>
            await _context.Datasets
                .Include(d => d.Issues)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();
    }
    
}
