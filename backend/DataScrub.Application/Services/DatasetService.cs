using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataScrub.Application.DTOs;
using DataScrub.Application.Interfaces;
using DataScrub.Domain.Entities;

namespace DataScrub.Application.Services
{
    // Controller'lar HTTP ile ilgilenir, bu servis iş mantığıyla ilgilenir.
    // Bu ayrım (separation of concerns) SOLID'in "S" (Single Responsibility) prensibinin somut hali.
    public class DatasetService
    {
        private readonly IDatasetRepository _repository;
        private readonly IMlAnalysisService _mlService;

        public DatasetService(IDatasetRepository repository, IMlAnalysisService mlService)
        {
            _repository = repository;
            _mlService = mlService;
        }

        public async Task<Dataset> UploadDatasetAsync(string fileName, string filePath, int rowCount, int columnCount)
        {
            var dataset = new Dataset
            {
                FileName = fileName,
                OriginalFilePath = filePath,
                RowCount = rowCount,
                ColumnCount = columnCount,
                Status = DatasetStatus.Uploaded
            };

            return await _repository.AddAsync(dataset);
        }

        // Python ML servisini çağırıp sonuçları DB'ye yazan orkestrasyon metodu.
        // Gerçek projede bu Hangfire ile background job olarak çalıştırılmalı (büyük dosyalarda HTTP timeout olmasın diye).
        public async Task AnalyzeDatasetAsync(Guid datasetId)
        {
            var dataset = await _repository.GetByIdAsync(datasetId)
                ?? throw new InvalidOperationException($"Dataset bulunamadı: {datasetId}");

            // Yükleme sırasında bilinmeyen satır/kolon sayısını burada dolduruyoruz.
            var (rows, columns) = await _mlService.InspectAsync(dataset.OriginalFilePath);
            dataset.RowCount = rows;
            dataset.ColumnCount = columns;

            dataset.Status = DatasetStatus.Processing;
            await _repository.UpdateAsync(dataset);

            var allIssues = new List<DetectedIssue>();

            // Üç analiz türünü paralel çalıştırıyoruz - performans için önemli bir tasarım kararı
            var duplicatesTask = _mlService.DetectDuplicatesAsync(datasetId, dataset.OriginalFilePath);
            var missingTask = _mlService.DetectMissingValuesAsync(datasetId, dataset.OriginalFilePath);
            var formatTask = _mlService.DetectFormatErrorsAsync(datasetId, dataset.OriginalFilePath);

            await Task.WhenAll(duplicatesTask, missingTask, formatTask);

            allIssues.AddRange(await duplicatesTask);
            allIssues.AddRange(await missingTask);
            allIssues.AddRange(await formatTask);

            await _repository.AddIssuesAsync(allIssues);

            dataset.Status = DatasetStatus.Analyzed;
            dataset.ProcessedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(dataset);
        }

        public async Task<DatasetSummaryDto?> GetDatasetSummaryAsync(Guid datasetId)
        {
            var dataset = await _repository.GetByIdWithIssuesAsync(datasetId);
            if (dataset == null) return null;

            return new DatasetSummaryDto
            {
                Id = dataset.Id,
                FileName = dataset.FileName,
                Status = dataset.Status.ToString(),
                RowCount = dataset.RowCount,
                TotalIssues = dataset.Issues.Count,
                PendingIssues = dataset.Issues.Count(i => i.Resolution == ResolutionStatus.Pending),
                ResolvedIssues = dataset.Issues.Count(i => i.Resolution != ResolutionStatus.Pending),
                Issues = dataset.Issues.Select(MapIssue).ToList()
            };
        }
        public async Task<List<DatasetSummaryDto>> GetAllDatasetsAsync() 
        {
            var datasets = await _repository.GetAllWithIssuesAsync();
            return datasets.Select(d => new DatasetSummaryDto
           {
           Id = d.Id,
           FileName = d.FileName,
           Status = d.Status.ToString(),
           RowCount = d.RowCount,
           TotalIssues = d.Issues.Count,
           PendingIssues = d.Issues.Count(i => i.Resolution == ResolutionStatus.Pending),
           ResolvedIssues = d.Issues.Count(i => i.Resolution != ResolutionStatus.Pending)
           }).ToList();
        }

        public async Task<bool> ResolveIssueAsync(Guid issueId, bool approve)
        {
            var issue = await _repository.GetIssueByIdAsync(issueId);
            if (issue == null) return false;

            issue.Resolution = approve ? ResolutionStatus.Approved : ResolutionStatus.Rejected;
            issue.ResolvedAt = DateTime.UtcNow;

            await _repository.UpdateIssueAsync(issue);
            return true;
        }

        // Toplu onay/ret. Var olmayan Id'ler sessizce atlanır - kullanıcı arayüzünde
        // filtrelenmiş bir listeyi onaylarken araya silinmiş bir kayıt girerse
        // tüm işlemin patlaması yerine geri kalanı uygulanır.
        public async Task<int> ResolveIssuesBulkAsync(IEnumerable<Guid> issueIds, bool approve)
        {
            var issues = await _repository.GetIssuesByIdsAsync(issueIds);
            if (issues.Count == 0) return 0;

            var resolution = approve ? ResolutionStatus.Approved : ResolutionStatus.Rejected;
            var now = DateTime.UtcNow;

            foreach (var issue in issues)
            {
                issue.Resolution = resolution;
                issue.ResolvedAt = now;
            }

            await _repository.UpdateIssuesAsync(issues);
            return issues.Count;
        }

        // Kullanıcının onayladığı düzeltmeleri gerçek veriye uygulayıp temiz dosyanın yolunu döner.
        // Not: sadece Approved (onaylanmış) issue'lar dikkate alınıyor - Pending ya da Rejected
        // olanlar orijinal veride hiç değiştirilmiyor. Bu, "kör AI" değil "insan onaylı" akışın garantisi.
        public async Task<string> ExportCleanedDatasetAsync(Guid datasetId)
        {
            var dataset = await _repository.GetByIdAsync(datasetId)
                ?? throw new InvalidOperationException($"Dataset bulunamadı: {datasetId}");

            var approvedIssues = await _repository.GetApprovedIssuesAsync(datasetId);

            var cleanedFilePath = await _mlService.ApplyCleaningAsync(dataset.OriginalFilePath, approvedIssues);

            dataset.CleanedFilePath = cleanedFilePath;
            dataset.Status = DatasetStatus.Cleaned;
            await _repository.UpdateAsync(dataset);

            return cleanedFilePath;
        }

        private static IssueDto MapIssue(DetectedIssue issue) => new()
        {
            Id = issue.Id,
            Type = issue.Type.ToString(),
            RowIndex = issue.RowIndex,
            RelatedRowIndex = issue.RelatedRowIndex,
            ColumnName = issue.ColumnName,
            OriginalValue = issue.OriginalValue,
            SuggestedValue = issue.SuggestedValue,
            ConfidenceScore = issue.ConfidenceScore,
            Resolution = issue.Resolution.ToString()
        };
    }
}
