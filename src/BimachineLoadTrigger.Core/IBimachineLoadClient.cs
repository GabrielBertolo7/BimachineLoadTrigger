using BimachineLoadTrigger.Core.Models;

namespace BimachineLoadTrigger.Core;

public interface IBimachineLoadClient
{
    Task<ExecuteLoadResponse> ExecuteLoadAsync(string loadCode, CancellationToken cancellationToken = default);

    Task<LoadStatusResponse> GetLoadStatusAsync(long executionId, CancellationToken cancellationToken = default);

    Task<LoadStatusResponse> WaitForCompletionAsync(long executionId, CancellationToken cancellationToken = default);
}
