namespace OctopusEx.WebCore.Tests.Controllers;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OctopusEx.WebCore.DomainCore.Abstractions.Interfaces.Services;
using OctopusEx.WebCore.DomainCore.APICommon;
using OctopusEx.WebCore.DomainCore.Implementations.Controllers;
using ValidationResult = OctopusEx.WebCore.DomainCore.APICommon.ValidationResult;
using DeleteCheckResult = OctopusEx.WebCore.DomainCore.APICommon.DeleteCheckResult;

// ---------------------------------------------------------------------------
// Test fixture types
// ---------------------------------------------------------------------------

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class TestDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class TestCreateDto
{
    public string Name { get; set; } = "";
}

public class TestUpdateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>
/// Concrete test controller subclass to exercise the abstract CURDControllerBase.
/// </summary>
public class TestCrudController
    : CURDControllerBase<TestEntity, int, TestDto, TestCreateDto, TestUpdateDto>
{
    public TestCrudController(
        ICrudService<TestEntity, int, TestDto, TestCreateDto, TestUpdateDto> service)
        : base(service)
    {
    }

    protected override int GetEntityIdFromDto(TestDto dto) => dto.Id;
}

// ===========================================================================
// A. Shared type tests — ValidationResult
// ===========================================================================

public class ValidationResultTests
{
    [Fact]
    public void ValidationResult_Success_HasIsSuccessTrue()
    {
        // Arrange & Act
        var result = ValidationResult.Success;

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidationResult_Success_HasIsValidTrue()
    {
        // Arrange & Act
        var result = ValidationResult.Success;

        // Assert — IsValid is an alias for IsSuccess
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidationResult_Fail_SetsIsSuccessFalseAndMessage()
    {
        // Arrange & Act
        var result = ValidationResult.Fail("something went wrong");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("something went wrong");
    }

    [Fact]
    public void ValidationResult_IsValid_SetterUpdatesIsSuccess()
    {
        // Arrange
        var result = new ValidationResult { IsSuccess = false };

        // Act — set via the IsValid alias
        result.IsValid = true;

        // Assert — IsSuccess should reflect the change (bidirectional alias)
        result.IsSuccess.Should().BeTrue();
        result.IsValid.Should().BeTrue();
    }
}

// ===========================================================================
// A. Shared type tests — DeleteCheckResult
// ===========================================================================

public class DeleteCheckResultTests
{
    [Fact]
    public void DeleteCheckResult_Allowed_HasCanDeleteTrue()
    {
        // Arrange & Act
        var result = DeleteCheckResult.Allowed;

        // Assert
        result.CanDelete.Should().BeTrue();
    }

    [Fact]
    public void DeleteCheckResult_NotAllowed_SetsCanDeleteFalseAndReason()
    {
        // Arrange & Act
        var result = DeleteCheckResult.NotAllowed("referenced by other records");

        // Assert
        result.CanDelete.Should().BeFalse();
        result.Reason.Should().Be("referenced by other records");
    }
}

// ===========================================================================
// B. Controller exception-handling & query tests
// ===========================================================================

public class CrudControllerBaseTests
{
    private readonly Mock<ICrudService<TestEntity, int, TestDto, TestCreateDto, TestUpdateDto>>
        _mockService;

    private readonly TestCrudController _controller;

    public CrudControllerBaseTests()
    {
        _mockService = new Mock<ICrudService<TestEntity, int, TestDto, TestCreateDto, TestUpdateDto>>();
        _controller = new TestCrudController(_mockService.Object);

        // Provide a minimal HttpContext so controller helpers function correctly
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // --- GetAsync ---

    [Fact]
    public async Task GetAsync_EntityExists_Returns200WithDto()
    {
        // Arrange
        var dto = new TestDto { Id = 1, Name = "Alpha" };
        _mockService
            .Setup(s => s.GetAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<BaseResponse<TestDto>>().Subject;
        response.Code.Should().Be(0);
        response.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(1);
        response.Data.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetAsync_EntityNotFound_Returns404()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TestDto?)null);

        // Act
        var result = await _controller.GetAsync(99, CancellationToken.None);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var response = notFoundResult.Value.Should().BeOfType<BaseResponse<TestDto>>().Subject;
        response.Code.Should().NotBe(0);
    }

    [Fact]
    public async Task GetAsync_ServiceThrowsException_Returns500()
    {
        // Arrange
        _mockService
            .Setup(s => s.GetAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database connection lost"));

        // Act
        var result = await _controller.GetAsync(1, CancellationToken.None);

        // Assert — ExecuteAsync catches and delegates to HandleException → 500
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var response = objectResult.Value.Should().BeOfType<BaseResponse>().Subject;
        response.Message.Should().Contain("database connection lost");
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ServiceThrowsArgumentException_Returns400()
    {
        // Arrange
        var createDto = new TestCreateDto { Name = "Beta" };
        _mockService
            .Setup(s => s.CreateAsync(It.IsAny<TestCreateDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("name already exists"));

        // Act
        var result = await _controller.CreateAsync(createDto, CancellationToken.None);

        // Assert — HandleWriteException maps ArgumentException → 400
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var response = badRequestResult.Value.Should().BeOfType<BaseResponse<TestDto>>().Subject;
        response.Message.Should().Contain("name already exists");
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ServiceThrowsKeyNotFoundException_Returns404()
    {
        // Arrange
        var updateDto = new TestUpdateDto { Id = 42, Name = "Updated" };
        _mockService
            .Setup(s => s.UpdateAsync(It.IsAny<TestUpdateDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("ID 42 not found"));

        // Act
        var result = await _controller.UpdateAsync(42, updateDto, CancellationToken.None);

        // Assert — HandleWriteException maps KeyNotFoundException → 404
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var response = notFoundResult.Value.Should().BeOfType<BaseResponse<TestDto>>().Subject;
        response.Message.Should().Contain("ID 42 not found");
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ServiceThrowsInvalidOperationException_Returns400()
    {
        // Arrange
        _mockService
            .Setup(s => s.DeleteAsync(5, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("entity is in use"));

        // Act
        var result = await _controller.DeleteAsync(5, CancellationToken.None);

        // Assert — HandleWriteException maps InvalidOperationException → 400
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var response = badRequestResult.Value.Should().BeOfType<BaseResponse<bool>>().Subject;
        response.Message.Should().Contain("entity is in use");
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_Returns200WithList()
    {
        // Arrange
        var dtos = new List<TestDto>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" },
            new() { Id = 3, Name = "C" }
        };
        _mockService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtos);

        // Act
        var result = await _controller.GetAllAsync(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<BaseResponse<List<TestDto>>>().Subject;
        response.Code.Should().Be(0);
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCount(3);
    }

    // --- ExistsAsync ---

    [Fact]
    public async Task ExistsAsync_Returns200WithBool()
    {
        // Arrange
        _mockService
            .Setup(s => s.ExistsAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ExistsAsync(7, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<BaseResponse<bool>>().Subject;
        response.Code.Should().Be(0);
        response.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_EntityDoesNotExist_Returns200WithFalse()
    {
        // Arrange
        _mockService
            .Setup(s => s.ExistsAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ExistsAsync(99, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);

        var response = okResult.Value.Should().BeOfType<BaseResponse<bool>>().Subject;
        response.Code.Should().Be(0);
        response.Data.Should().BeFalse();
    }
}
