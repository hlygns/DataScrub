using System;
using System.IO;
using System.Threading.Tasks;
using DataScrub.Application.DTOs;
using DataScrub.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DataScrub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatasetsController : ControllerBase
    {
        private readonly DatasetService _datasetService;
        private readonly IWebHostEnvironment _env;

        public DatasetsController(DatasetService datasetService, IWebHostEnvironment env)
        {
            _datasetService = datasetService;
            _env = env;
        }

        // POST /api/datasets/upload
        [HttpPost("upload")]
        public async Task<ActionResult<DatasetUploadResponse>> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Dosya boş olamaz.");

            var allowedExtensions = new[] { ".csv", ".xlsx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (Array.IndexOf(allowedExtensions, extension) < 0)
                return BadRequest("Sadece .csv veya .xlsx dosyaları desteklenir.");

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
            Directory.CreateDirectory(uploadsFolder);

            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Not: Gerçek projede satır/kolon sayısını burada dosyayı okuyarak hesaplarız.
            // Basitlik için şimdilik 0 veriyoruz, analiz aşamasında Python servisi hesaplayacak.
            var dataset = await _datasetService.UploadDatasetAsync(file.FileName, filePath, rowCount: 0, columnCount: 0);

            return Ok(new DatasetUploadResponse
            {
                DatasetId = dataset.Id,
                FileName = dataset.FileName,
                Status = dataset.Status.ToString()
            });
        }

        // POST /api/datasets/{id}/analyze
        [HttpPost("{id}/analyze")]
        public async Task<IActionResult> Analyze(Guid id)
        {
            try
            {
                await _datasetService.AnalyzeDatasetAsync(id);
                return Ok(new { message = "Analiz tamamlandı." });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // GET /api/datasets/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DatasetSummaryDto>> GetSummary(Guid id)
        {
            var summary = await _datasetService.GetDatasetSummaryAsync(id);
            if (summary == null) return NotFound();
            return Ok(summary);
        }

        // GET /api/datasets/{id}/export
        [HttpGet("{id}/export")]
        public async Task<IActionResult> Export(Guid id)
        {
            try
            {
                var cleanedFilePath = await _datasetService.ExportCleanedDatasetAsync(id);

                if (!System.IO.File.Exists(cleanedFilePath))
                    return NotFound("Temiz dosya bulunamadı.");

                var fileName = Path.GetFileName(cleanedFilePath);
                var contentType = fileName.EndsWith(".xlsx")
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "text/csv";

                var fileBytes = await System.IO.File.ReadAllBytesAsync(cleanedFilePath);
                return File(fileBytes, contentType, fileName);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // POST /api/datasets/issues/{issueId}/resolve
        [HttpPost("issues/{issueId}/resolve")]
        public async Task<IActionResult> ResolveIssue(Guid issueId, [FromBody] ResolveIssueRequest request)
        {
            var success = await _datasetService.ResolveIssueAsync(issueId, request.Approve);
            if (!success) return NotFound();
            return Ok(new { message = request.Approve ? "Öneri onaylandı." : "Öneri reddedildi." });
        }
    }
}
