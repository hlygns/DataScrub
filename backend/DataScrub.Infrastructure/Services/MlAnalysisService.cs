using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DataScrub.Application.Interfaces;
using DataScrub.Domain.Entities;

namespace DataScrub.Infrastructure.Services
{
    // Python FastAPI servisine REST ile konuşan implementasyon.
    // Application katmanı IMlAnalysisService interface'ini bilir, bu sınıfın Python
    // kullandığını bilmez - yarın Python yerine başka bir servis koyarsak Application değişmez.
    public class MlAnalysisService : IMlAnalysisService
    {
        private readonly HttpClient _httpClient;

        public MlAnalysisService(HttpClient httpClient)
        {
            _httpClient = httpClient; // BaseAddress Program.cs'de appsettings'den set edilecek
        }

        public async Task<List<DetectedIssue>> DetectDuplicatesAsync(Guid datasetId, string filePath)
        {
            var response = await _httpClient.PostAsJsonAsync("/detect-duplicates",
                new { dataset_id = datasetId, file_path = filePath });
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MlIssueResponse>();
            return MapToIssues(result, IssueType.Duplicate, datasetId);
        }

        public async Task<List<DetectedIssue>> DetectMissingValuesAsync(Guid datasetId, string filePath)
        {
            var response = await _httpClient.PostAsJsonAsync("/detect-missing",
                new { dataset_id = datasetId, file_path = filePath });
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MlIssueResponse>();
            return MapToIssues(result, IssueType.MissingValue, datasetId);
        }

        public async Task<List<DetectedIssue>> DetectFormatErrorsAsync(Guid datasetId, string filePath)
        {
            var response = await _httpClient.PostAsJsonAsync("/fix-format",
                new { dataset_id = datasetId, file_path = filePath });
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MlIssueResponse>();
            return MapToIssues(result, IssueType.FormatError, datasetId);
        }

        public async Task<string> ApplyCleaningAsync(string filePath, List<DetectedIssue> approvedIssues)
        {
            var resolutions = approvedIssues.Select(issue => new
            {
                type = issue.Type.ToString(),
                row_index = issue.RowIndex,
                related_row_index = issue.RelatedRowIndex,
                column_name = issue.ColumnName,
                suggested_value = issue.SuggestedValue
            });

            var response = await _httpClient.PostAsJsonAsync("/apply-cleaning",
                new { file_path = filePath, resolutions });
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApplyCleaningResponse>();
            return result?.CleanedFilePath ?? throw new InvalidOperationException("ML servisi temiz dosya yolu döndürmedi.");
        }

        private class ApplyCleaningResponse
        {
            [JsonPropertyName("cleaned_file_path")]
            public string CleanedFilePath { get; set; } = string.Empty;
        }

        private static List<DetectedIssue> MapToIssues(MlIssueResponse? response, IssueType type, Guid datasetId)
        {
            var issues = new List<DetectedIssue>();
            if (response?.Issues == null) return issues;

            foreach (var item in response.Issues)
            {
                issues.Add(new DetectedIssue
                {
                    DatasetId = datasetId,
                    Type = type,
                    RowIndex = item.RowIndex,
                    RelatedRowIndex = item.RelatedRowIndex,
                    ColumnName = item.ColumnName,
                    OriginalValue = item.OriginalValue,
                    SuggestedValue = item.SuggestedValue,
                    ConfidenceScore = item.ConfidenceScore
                });
            }
            return issues;
        }

        // Python servisinin JSON response'una karşılık gelen DTO'lar
        private class MlIssueResponse
        {
            [JsonPropertyName("issues")]
            public List<MlIssueItem> Issues { get; set; } = new();
        }

        private class MlIssueItem
        {
            [JsonPropertyName("row_index")]
            public int RowIndex { get; set; }

            [JsonPropertyName("related_row_index")]
            public int? RelatedRowIndex { get; set; }

            [JsonPropertyName("column_name")]
            public string ColumnName { get; set; } = string.Empty;

            [JsonPropertyName("original_value")]
            public string? OriginalValue { get; set; }

            [JsonPropertyName("suggested_value")]
            public string? SuggestedValue { get; set; }

            [JsonPropertyName("confidence_score")]
            public double ConfidenceScore { get; set; }
        }
    }
}
