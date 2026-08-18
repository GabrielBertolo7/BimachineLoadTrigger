namespace BimachineLoadTrigger.Core.Constants;

/// <summary>
/// Monta as rotas relativas da API de cargas do BIMachine, isolado do cliente HTTP que as consome.
/// </summary>
internal static class BimachineEndpoints
{
    private const string SchedulingsBasePath = "api/origins/schedulings";

    public static string ExecuteLoad(string loadCode, string appKey) =>
        $"{SchedulingsBasePath}/{Uri.EscapeDataString(loadCode)}/execute?appKey={Uri.EscapeDataString(appKey)}";

    public static string Status(long executionId, string appKey) =>
        $"{SchedulingsBasePath}/{executionId}/status?appKey={Uri.EscapeDataString(appKey)}";
}
