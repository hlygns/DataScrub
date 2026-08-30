using System;

namespace DataScrub.Domain.Entities
{
    // Python ML servisinin tespit ettiği sorun tipleri.
    public enum IssueType
    {
        Duplicate,
        MissingValue,
        FormatError
    }

    public enum ResolutionStatus
    {
        Pending,
        Approved,
        Rejected
    }

    // Bir satırda/hücrede tespit edilen tek bir veri kalitesi sorunu.
    // Örn: "12. satırdaki telefon numarası ile 45. satırdaki muhtemelen aynı kişi (skor: 0.91)"
    public class DetectedIssue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DatasetId { get; set; }
        public Dataset? Dataset { get; set; }

        public IssueType Type { get; set; }

        // Sorunun ilgili olduğu satır(lar). Duplicate için iki satır, diğerleri için tek satır.
        public int RowIndex { get; set; }
        public int? RelatedRowIndex { get; set; } // Duplicate eşleşen satır

        public string ColumnName { get; set; } = string.Empty;
        public string? OriginalValue { get; set; }
        public string? SuggestedValue { get; set; }

        // ML servisinin verdiği güven skoru (0.0 - 1.0). Kör AI değil, insan onaylı yaklaşım.
        public double ConfidenceScore { get; set; }

        public ResolutionStatus Resolution { get; set; } = ResolutionStatus.Pending;
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
