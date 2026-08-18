using BimachineLoadTrigger.Core;
using BimachineLoadTrigger.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

builder.Configuration.AddUserSecrets<Program>(optional: true);

builder.Services
    .AddOptions<BimachineOptions>()
    .Bind(builder.Configuration.GetSection(BimachineOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IBimachineLoadClient, BimachineLoadClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BimachineOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var client = host.Services.GetRequiredService<IBimachineLoadClient>();
var options = host.Services.GetRequiredService<IOptions<BimachineOptions>>().Value;

try
{
    logger.LogInformation("Disparando carga {LoadCode}...", options.LoadCode);
    var execution = await client.ExecuteLoadAsync(options.LoadCode);
    logger.LogInformation("Carga disparada. Id de execução retornado pela API: {ExecutionId}", execution.Id);

    logger.LogInformation("Aguardando conclusão (timeout de {TimeoutMinutes} min)...", options.PollingTimeoutMinutes);
    var status = await client.WaitForCompletionAsync(execution.Id);

    if (status.IsError)
    {
        logger.LogError("Carga finalizada com ERRO. Log da API: {Log}", status.Log);
        return 1;
    }

    logger.LogInformation("Carga finalizada. Status: {Status}. Log da API: {Log}", status.Status, status.Log);
    return 0;
}
catch (OperationCanceledException)
{
    logger.LogError("Timeout aguardando a conclusão da carga.");
    return 1;
}
catch (HttpRequestException ex)
{
    logger.LogError(ex, "Falha de comunicação com a API do BIMachine.");
    return 1;
}
