namespace BuildingBlocks.Sagas;

/// <summary>
/// Minimal saga starter example showing where orchestration logic would live.
/// Real implementations should persist saga state and handle retries/compensation.
/// </summary>
public sealed class SagaStarter
{
    public async Task StartOrderSagaAsync(string orderId)
    {
        // 1. publish OrderCreated event to the bus
        // 2. wait for payment confirmation (subscribe or webhook)
        // 3. on success publish OrderPaid -> Fulfillment
        // 4. on failure issue compensating actions (release inventory, refund)

        await Task.CompletedTask;
    }
}
