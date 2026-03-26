using MedFlow.Domain.Entities;

namespace MedFlow.Application.Interfaces.Workflow;

public interface IWorkflowTriggerService
{
    Task TriggerFromEventAsync(EventLog eventLog, CancellationToken cancellationToken = default);
}
