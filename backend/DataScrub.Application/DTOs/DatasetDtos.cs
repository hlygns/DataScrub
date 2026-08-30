using System;
using System.Collections.Generic;

namespace DataScrub.Application.DTOs
{
    public class DatasetUploadResponse
    {
        public Guid DatasetId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class IssueDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public int RowIndex { get; set; }
        public int? RelatedRowIndex { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string? OriginalValue { get; set; }
        public string? SuggestedValue { get; set; }
        public double ConfidenceScore { get; set; }
        public string Resolution { get; set; } = string.Empty;
    }

    public class ResolveIssueRequest
    {
        // true = öneriyi kabul et, false = reddet, orijinal veriyi koru
        public bool Approve { get; set; }
    }

    public class DatasetSummaryDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public int TotalIssues { get; set; }
        public int PendingIssues { get; set; }
        public int ResolvedIssues { get; set; }
        public List<IssueDto> Issues { get; set; } = new();
    }
}
