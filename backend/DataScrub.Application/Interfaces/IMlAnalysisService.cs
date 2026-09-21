using System.Collections.Generic;
using System.Threading.Tasks;
using DataScrub.Domain.Entities;

namespace DataScrub.Application.Interfaces
{
    // Python ML mikroservisine erişimi soyutlar.
    // Application katmanı Python'un FastAPI mi Flask mi olduğunu bilmez, sadece bu sözleşmeyi bilir.
    public interface IMlAnalysisService
    {
        // Dosyanın veri satırı ve kolon sayısı (başlık hariç).
        Task<(int Rows, int Columns)> InspectAsync(string filePath);

        Task<List<DetectedIssue>> DetectDuplicatesAsync(Guid datasetId, string filePath);
        Task<List<DetectedIssue>> DetectMissingValuesAsync(Guid datasetId, string filePath);
        Task<List<DetectedIssue>> DetectFormatErrorsAsync(Guid datasetId, string filePath);

        // Onaylanmış düzeltmeleri gerçek veriye uygulayıp temiz dosyanın yolunu döner.
        Task<string> ApplyCleaningAsync(string filePath, List<DetectedIssue> approvedIssues);
    }
}
