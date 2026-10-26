using CivicVoice.Application.DTOs;
using CivicVoice.Application.Interfaces;
using CivicVoice.Application.Services;
using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CivicVoice.Tests;

public class GrievanceServiceTests
{
    private readonly Mock<IGrievanceRepository> _grievanceRepoMock = new();
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IWardRepository> _wardRepoMock = new();
    private readonly Mock<IAssignmentRepository> _assignmentRepoMock = new();
    private readonly Mock<IStatusHistoryRepository> _historyRepoMock = new();
    private readonly Mock<ILogger<GrievanceService>> _loggerMock = new();

    private GrievanceService CreateService()
    {
        return new GrievanceService(
            _grievanceRepoMock.Object,
            _categoryRepoMock.Object,
            _wardRepoMock.Object,
            _assignmentRepoMock.Object,
            _historyRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateGrievance_WithValidData_ReturnsSuccessAndCalculatesSla()
    {
        // Arrange
        var service = CreateService();
        var category = new Category
        {
            CategoryId = 1,
            Name = "Water Leakage",
            DefaultSlaHours = 24,
            DepartmentId = 10,
            Department = new Department { DepartmentId = 10, Name = "Water Department", Email = "water@civic.local" }
        };

        _wardRepoMock.Setup(w => w.ExistsAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        _categoryRepoMock.Setup(c => c.GetByIdWithDepartmentAsync(1, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(category);

        var dto = new CreateGrievanceDto
        {
            WardId = 1,
            CategoryId = 1,
            Description = "Main water pipe burst flooding road near market."
        };

        // Act
        var result = await service.CreateGrievanceAsync(dto, "citizen-123");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(GrievanceStatus.Submitted, result.Data.Status);
        Assert.Equal("citizen-123", result.Data.CitizenId);
        // SLA should be exactly CreatedAt + 24 hours
        Assert.Equal(result.Data.CreatedAt.AddHours(24), result.Data.DueAt);
        _grievanceRepoMock.Verify(r => r.AddAsync(It.IsAny<Grievance>(), It.IsAny<CancellationToken>()), Times.Once);
        _grievanceRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateGrievance_WithEmptyDescription_ReturnsValidationFailure(string? description)
    {
        // Arrange
        var service = CreateService();
        var dto = new CreateGrievanceDto
        {
            WardId = 1,
            CategoryId = 1,
            Description = description!
        };

        // Act
        var result = await service.CreateGrievanceAsync(dto, "citizen-123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("complaint", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateGrievance_WithShortDescription_ReturnsValidationFailure()
    {
        // Arrange
        var service = CreateService();
        var dto = new CreateGrievanceDto
        {
            WardId = 1,
            CategoryId = 1,
            Description = "Too short" // 9 chars < 10
        };

        // Act
        var result = await service.CreateGrievanceAsync(dto, "citizen-123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("at least 10 characters", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateGrievance_WithInvalidWard_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        _wardRepoMock.Setup(w => w.ExistsAsync(999, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(false);

        var dto = new CreateGrievanceDto
        {
            WardId = 999,
            CategoryId = 1,
            Description = "Pothole on the avenue causing accidents."
        };

        // Act
        var result = await service.CreateGrievanceAsync(dto, "citizen-123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("ward", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateGrievance_WithInvalidCategory_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        _wardRepoMock.Setup(w => w.ExistsAsync(1, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        _categoryRepoMock.Setup(c => c.GetByIdWithDepartmentAsync(999, It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Category?)null);

        var dto = new CreateGrievanceDto
        {
            WardId = 1,
            CategoryId = 999,
            Description = "Broken streetlight on junction 4."
        };

        // Act
        var result = await service.CreateGrievanceAsync(dto, "citizen-123");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("category", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateGrievance_RoutesToCorrectDepartment()
    {
        // Arrange
        var service = CreateService();
        var category = new Category
        {
            CategoryId = 4,
            Name = "Pothole Repair",
            DefaultSlaHours = 48,
            DepartmentId = 2,
            Department = new Department { DepartmentId = 2, Name = "Roads & Infrastructure", Email = "roads@civic.local" }
        };

        _wardRepoMock.Setup(w => w.ExistsAsync(2, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(true);
        _categoryRepoMock.Setup(c => c.GetByIdWithDepartmentAsync(4, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(category);

        var dto = new CreateGrievanceDto
        {
            WardId = 2,
            CategoryId = 4,
            Description = "Dangerous deep pothole on road curve near school."
        };

        // Act
        var result = await service.CreateGrievanceAsync(dto, "citizen-456");

        // Assert
        Assert.True(result.Success);
        // Verify assignment created for DepartmentId 2
        _assignmentRepoMock.Verify(a => a.AddAsync(It.Is<Assignment>(assign => assign.DepartmentId == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_ValidTransition_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();
        var grievance = new Grievance
        {
            GrievanceId = 10,
            Status = GrievanceStatus.Submitted,
            CitizenId = "citizen-1"
        };

        _grievanceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(grievance);

        // Act: Submitted -> Assigned is valid
        var result = await service.UpdateStatusAsync(10, GrievanceStatus.Assigned, "Officer-1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(GrievanceStatus.Assigned, grievance.Status);
        _grievanceRepoMock.Verify(r => r.Update(grievance), Times.Once);
        _grievanceRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_InvalidTransition_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var grievance = new Grievance
        {
            GrievanceId = 10,
            Status = GrievanceStatus.Submitted,
            CitizenId = "citizen-1"
        };

        _grievanceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(grievance);

        // Act: Submitted -> Resolved directly is invalid (must go through Assigned & InProgress)
        var result = await service.UpdateStatusAsync(10, GrievanceStatus.Resolved, "Officer-1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot transition from Submitted to Resolved", result.ErrorMessage);
        Assert.Equal(GrievanceStatus.Submitted, grievance.Status);
    }

    [Fact]
    public async Task UpdateStatus_ResolvedGrievanceCannotMoveBackToInProgress_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var grievance = new Grievance
        {
            GrievanceId = 15,
            Status = GrievanceStatus.Resolved,
            CitizenId = "citizen-1"
        };

        _grievanceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(15, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(grievance);

        // Act
        var result = await service.UpdateStatusAsync(15, GrievanceStatus.InProgress, "Officer-1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot transition from Resolved to InProgress", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateStatus_NonExistentGrievance_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        _grievanceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(999, It.IsAny<CancellationToken>()))
                          .ReturnsAsync((Grievance?)null);

        // Act
        var result = await service.UpdateStatusAsync(999, GrievanceStatus.InProgress, "Officer-1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateStatus_WhenResolved_RaisesGrievanceResolvedEvent()
    {
        // Arrange
        var service = CreateService();
        var grievance = new Grievance
        {
            GrievanceId = 20,
            Status = GrievanceStatus.InProgress,
            CitizenId = "citizen-event-test"
        };

        _grievanceRepoMock.Setup(r => r.GetByIdWithDetailsAsync(20, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(grievance);

        bool eventRaised = false;
        int eventGrievanceId = 0;
        string eventCitizenId = string.Empty;

        service.GrievanceResolved += (sender, args) =>
        {
            eventRaised = true;
            eventGrievanceId = args.GrievanceId;
            eventCitizenId = args.CitizenId;
        };

        // Act: InProgress -> Resolved is valid and should fire GrievanceResolved
        var result = await service.UpdateStatusAsync(20, GrievanceStatus.Resolved, "Officer-1");

        // Assert
        Assert.True(result.Success);
        Assert.True(eventRaised, "GrievanceResolved delegate/event was expected to be raised.");
        Assert.Equal(20, eventGrievanceId);
        Assert.Equal("citizen-event-test", eventCitizenId);
    }
}
