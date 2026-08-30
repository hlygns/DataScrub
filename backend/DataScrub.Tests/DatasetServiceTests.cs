using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataScrub.Application.Interfaces;
using DataScrub.Application.Services;
using DataScrub.Domain.Entities;
using Moq;
using Xunit;

namespace DataScrub.Tests
{
    public class DatasetServiceTests
    {
        private static (Mock<IDatasetRepository> repo, Mock<IMlAnalysisService> mlService, DatasetService service)
            CreateServiceWithMocks()
        {
            var repoMock = new Mock<IDatasetRepository>();
            var mlServiceMock = new Mock<IMlAnalysisService>();
            var service = new DatasetService(repoMock.Object, mlServiceMock.Object);
            return (repoMock, mlServiceMock, service);
        }

        [Fact]
        public async Task AnalyzeDatasetAsync_WhenDatasetNotFound_ThrowsException()
        {
            var (repoMock, mlServiceMock, service) = CreateServiceWithMocks();
            var missingId = Guid.NewGuid();

            repoMock.Setup(r => r.GetByIdAsync(missingId))
                    .ReturnsAsync((Dataset?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.AnalyzeDatasetAsync(missingId));
        }

        [Fact]
        public async Task AnalyzeDatasetAsync_WhenDatasetExists_CallsAllThreeDetectionMethods()
        {
            var (repoMock, mlServiceMock, service) = CreateServiceWithMocks();
            var datasetId = Guid.NewGuid();
            var dataset = new Dataset { Id = datasetId, OriginalFilePath = "/fake/path.csv" };

            repoMock.Setup(r => r.GetByIdAsync(datasetId)).ReturnsAsync(dataset);
            mlServiceMock.Setup(m => m.DetectDuplicatesAsync(datasetId, dataset.OriginalFilePath))
                         .ReturnsAsync(new List<DetectedIssue>());
            mlServiceMock.Setup(m => m.DetectMissingValuesAsync(datasetId, dataset.OriginalFilePath))
                         .ReturnsAsync(new List<DetectedIssue>());
            mlServiceMock.Setup(m => m.DetectFormatErrorsAsync(datasetId, dataset.OriginalFilePath))
                         .ReturnsAsync(new List<DetectedIssue>());

            await service.AnalyzeDatasetAsync(datasetId);

            mlServiceMock.Verify(m => m.DetectDuplicatesAsync(datasetId, dataset.OriginalFilePath), Times.Once);
            mlServiceMock.Verify(m => m.DetectMissingValuesAsync(datasetId, dataset.OriginalFilePath), Times.Once);
            mlServiceMock.Verify(m => m.DetectFormatErrorsAsync(datasetId, dataset.OriginalFilePath), Times.Once);

            Assert.Equal(DatasetStatus.Analyzed, dataset.Status);
        }

        [Fact]
        public async Task ResolveIssueAsync_WhenIssueExists_UpdatesResolutionStatus()
        {
            var (repoMock, mlServiceMock, service) = CreateServiceWithMocks();
            var issueId = Guid.NewGuid();
            var issue = new DetectedIssue { Id = issueId, Resolution = ResolutionStatus.Pending };

            repoMock.Setup(r => r.GetIssueByIdAsync(issueId)).ReturnsAsync(issue);

            var result = await service.ResolveIssueAsync(issueId, approve: true);

            Assert.True(result);
            Assert.Equal(ResolutionStatus.Approved, issue.Resolution);
            Assert.NotNull(issue.ResolvedAt);
        }

        [Fact]
        public async Task ResolveIssueAsync_WhenIssueDoesNotExist_ReturnsFalse()
        {
            var (repoMock, mlServiceMock, service) = CreateServiceWithMocks();
            var missingIssueId = Guid.NewGuid();

            repoMock.Setup(r => r.GetIssueByIdAsync(missingIssueId))
                    .ReturnsAsync((DetectedIssue?)null);

            var result = await service.ResolveIssueAsync(missingIssueId, approve: true);

            Assert.False(result);
        }
    }
}