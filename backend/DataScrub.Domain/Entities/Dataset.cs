using System;
using System.Collections.Generic;

namespace DataScrub.Domain.Entities
{
    public enum DatasetStatus
    {
        Uploaded,
        Processing,
        Analyzed,
        Cleaned,
        Failed
    }

    // Kullanıcının yüklediği bir dosyayı ve işlenme durumunu temsil eder.
    public class Dataset
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string OriginalFilePath { get; set; } = string.Empty;
        public string? CleanedFilePath { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
        public DatasetStatus Status { get; set; } = DatasetStatus.Uploaded;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        // Navigation
        public ICollection<DetectedIssue> Issues { get; set; } = new List<DetectedIssue>();
    }
}
