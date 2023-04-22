using System;

namespace EventBus.Messages.Events;

public class IntegrationBaseEvent
{
  public IntegrationBaseEvent()
  {
    Id = Guid.NewGuid();
    CreationDate = DateTime.UtcNow;
  }

  public IntegrationBaseEvent(Guid id, DateTime creationDate)
  {
    Id = id;
    CreationDate = creationDate;
  }

  public Guid Id { get; set; }
  public DateTime CreationDate { get; set; }
}