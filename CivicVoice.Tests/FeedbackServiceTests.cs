using CivicVoice.Application.Interfaces;
using CivicVoice.Application.Services;
using CivicVoice.Domain.Entities;
using CivicVoice.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CivicVoice.Tests;

public class FeedbackServiceTests
{
    private readonly Mock<IFeedbackRepository> _feedbackRepoMock = new();
    private readonly Mock<IGrievanceRepository> _grievanceRepoMock = new();
    private readonly Mock<ILogger<FeedbackService>> _loggerMock = new();

    private FeedbackService CreateService()
    {
        return new FeedbackService(
            _feedbackRepoMock.Object,
            _grievanceRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SubmitFeedback_OnResolvedGrievanceByOwner_Succeeds()
    {
        // Arrange
        var service = CreateService();
        var grievance = new Grievance
        {
            GrievanceId = 1,
            CitizenId = "citizen-alice",
            Status = GrievanceStatus.Resolved
        };

        _grievanceRepoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(grievance);
        _feedbackRepoMock.Setup(f => f.GetByGrievanceAndCitizenAsync(1, "citizen-alice", It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Feedback?)null);

        // Act
        var result = await service.SubmitFeedbackAsync(1, "citizen-alice", 5, "Excellent resolution!");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data.Rating);
        _feedbackRepoMock.Verify(f => f.AddAsync(It.IsAny<Feedback>(), It.IsAny<CancellationToken>()), Times.Once);
        _feedbackRepoMock.Verify(f => f.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitFeedback_OnNonResolvedGrievance_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var grievance = new Grievance
        {
            GrievanceId = 2,
            CitizenId = "citizen-alice",
            Status = GrievanceStatus.InProgress // Not resolved yet!
        };

        _grievanceRepoMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(grievance);

        // Act
        var result = await service.SubmitFeedbackAsync(2, "citizen-alice", 4, "Too early");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Feedback can only be submitted for resolved grievances", result.ErrorMessage);
    }

    [Fact]
    public async Task SubmitFeedback_ByDifferentCitizen_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var grievance = new Grievance
        {
            GrievanceId = 3,
            CitizenId = "citizen-alice", // Belongs to Alice
            Status = GrievanceStatus.Resolved
        };

        _grievanceRepoMock.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(grievance);

        // Act: Bob tries to submit feedback for Alice's grievance
        var result = await service.SubmitFeedbackAsync(3, "citizen-bob", 5, "Nice job!");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("You can only provide feedback for your own grievances", result.ErrorMessage);
    }

    [Fact]
    public async Task SubmitFeedback_WhenDuplicate_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var grievance = new Grievance
        {
            GrievanceId = 4,
            CitizenId = "citizen-alice",
            Status = GrievanceStatus.Resolved
        };

        var existingFeedback = new Feedback
        {
            FeedbackId = 99,
            GrievanceId = 4,
            CitizenId = "citizen-alice",
            Rating = 4
        };

        _grievanceRepoMock.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>()))
                          .ReturnsAsync(grievance);
        _feedbackRepoMock.Setup(f => f.GetByGrievanceAndCitizenAsync(4, "citizen-alice", It.IsAny<CancellationToken>()))
                         .ReturnsAsync(existingFeedback); // Already exists!

        // Act
        var result = await service.SubmitFeedbackAsync(4, "citizen-alice", 5, "Submitting second time");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already submitted feedback", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(10)]
    public async Task SubmitFeedback_WithInvalidRating_ReturnsValidationFailure(int invalidRating)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SubmitFeedbackAsync(5, "citizen-alice", invalidRating, "Bad rating value");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Rating must be between 1 and 5", result.ErrorMessage);
    }
}
