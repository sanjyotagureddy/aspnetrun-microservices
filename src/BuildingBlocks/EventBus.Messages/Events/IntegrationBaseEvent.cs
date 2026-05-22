namespace Shared.Messaging.Events;

public class IntegrationBaseEvent(Guid id, DateTime creationDate)
{
  public IntegrationBaseEvent() : this(Guid.NewGuid(), DateTime.UtcNow)
  {
  }

  public Guid Id { get; set; } = id;
  public DateTime CreationDate { get; set; } = creationDate;
}