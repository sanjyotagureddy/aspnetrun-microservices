using System.Reflection;
using Common.SharedKernel.Results;
using Inventory.Api.Contracts;
using Inventory.Api.Features.Inventory.GetBatch;
using Inventory.Api.Features.Inventory.GetByProductId;
using Inventory.Api.Features.Inventory.Initialize;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Inventory.Api.Tests.Features.Inventory;

public sealed class EndpointHandleTests
{
    [Fact]
    public async Task GetByProductId_HandleAsync_ShouldReturnOk()
    {
        Guid productId = Guid.NewGuid();
        Mock<IMediator> mediator = new();
        mediator
            .Setup(x => x.Send(It.IsAny<GetInventoryByProductIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InventoryResponse>.Success(new InventoryResponse(productId, 7)));

        MethodInfo method = typeof(GetInventoryByProductIdEndpoint).GetMethod("HandleAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        Task<IResult> task = (Task<IResult>)method.Invoke(null, [mediator.Object, productId, CancellationToken.None])!;

        IResult result = await task;

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ((IStatusCodeHttpResult)result).StatusCode);
    }

    [Fact]
    public async Task GetBatch_HandleAsync_ShouldReturnOk()
    {
        Mock<IMediator> mediator = new();
        InventoryBatchRequest request = new([Guid.NewGuid()]);
        mediator
            .Setup(x => x.Send(It.IsAny<GetInventoryBatchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InventoryBatchResponse>.Success(new InventoryBatchResponse(new Dictionary<Guid, int>())));

        MethodInfo method = typeof(GetInventoryBatchEndpoint).GetMethod("HandleAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        Task<IResult> task = (Task<IResult>)method.Invoke(null, [mediator.Object, request, CancellationToken.None])!;

        IResult result = await task;

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ((IStatusCodeHttpResult)result).StatusCode);
    }

    [Fact]
    public async Task Initialize_HandleAsync_ShouldReturnNoContent_WhenSuccess()
    {
        Mock<IMediator> mediator = new();
        InitializeInventoryRequest request = new(5);
        mediator
            .Setup(x => x.Send(It.IsAny<InitializeInventoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        MethodInfo method = typeof(InitializeInventoryEndpoint).GetMethod("HandleAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        Task<IResult> task = (Task<IResult>)method.Invoke(null, [mediator.Object, Guid.NewGuid(), request, CancellationToken.None])!;

        IResult result = await task;

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, ((IStatusCodeHttpResult)result).StatusCode);
    }

    [Fact]
    public async Task Initialize_HandleAsync_ShouldReturnBadRequest_WhenFailure()
    {
        Mock<IMediator> mediator = new();
        InitializeInventoryRequest request = new(5);
        mediator
            .Setup(x => x.Send(It.IsAny<InitializeInventoryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("failed"));

        MethodInfo method = typeof(InitializeInventoryEndpoint).GetMethod("HandleAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        Task<IResult> task = (Task<IResult>)method.Invoke(null, [mediator.Object, Guid.NewGuid(), request, CancellationToken.None])!;

        IResult result = await task;

        Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, ((IStatusCodeHttpResult)result).StatusCode);
    }
}
