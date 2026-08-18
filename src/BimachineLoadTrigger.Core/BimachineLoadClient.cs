using System.Net.Http.Json;
using BimachineLoadTrigger.Core.Configuration;
using BimachineLoadTrigger.Core.Constants;
using BimachineLoadTrigger.Core.Models;
using Microsoft.Extensions.Options;

namespace BimachineLoadTrigger.Core;

public sealed class BimachineLoadClient(HttpClient httpClient, IOptions<BimachineOptions> options) : IBimachineLoadClient
{
    private readonly BimachineOptions _options = options.Value;

    public async Task<ExecuteLoadResponse> ExecuteLoadAsync(string loadCode, CancellationToken cancellationToken = default)
    {
        var requestUri = BimachineEndpoints.ExecuteLoad(loadCode, _options.AppKey);

        using var response = await httpClient.PostAsync(requestUri, content: null, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ExecuteLoadResponse>(cancellationToken)
            ?? throw new InvalidOperationException(BimachineMessages.EmptyExecuteLoadResponse);
    }

    /// <summary>
    /// O "schedulingCode" do endpoint de status, apesar do nome, é o id de execução retornado por <see cref="ExecuteLoadAsync"/>, não o loadCode original.
    /// </summary>
    public async Task<LoadStatusResponse> GetLoadStatusAsync(long executionId, CancellationToken cancellationToken = default)
    {
        var requestUri = BimachineEndpoints.Status(executionId, _options.AppKey);

        using var response = await httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<LoadStatusResponse>(cancellationToken)
            ?? throw new InvalidOperationException(BimachineMessages.EmptyLoadStatusResponse);
    }

    /// <summary>
    /// Consulta o status periodicamente até a carga terminar (endDate presente) ou até estourar o timeout configurado.
    /// </summary>
    public async Task<LoadStatusResponse> WaitForCompletionAsync(long executionId, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(_options.PollingTimeoutMinutes));

        while (true)
        {
            var status = await GetLoadStatusAsync(executionId, timeoutCts.Token);
            if (status.IsFinished)
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), timeoutCts.Token);
        }
    }
}
