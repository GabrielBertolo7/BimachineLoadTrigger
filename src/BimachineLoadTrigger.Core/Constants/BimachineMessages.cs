namespace BimachineLoadTrigger.Core.Constants;

/// <summary>
/// Mensagens de erro usadas por <see cref="BimachineLoadClient"/>, isoladas do código de orquestração HTTP.
/// </summary>
internal static class BimachineMessages
{
    public const string EmptyExecuteLoadResponse = "A API retornou uma resposta vazia ao disparar a carga.";

    public const string EmptyLoadStatusResponse = "A API retornou uma resposta vazia ao consultar o status da carga.";
}
