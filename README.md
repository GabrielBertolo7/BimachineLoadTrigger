# BimachineLoadTrigger

Cliente .NET para disparar cargas de dados no [BIMachine](https://www.bimachine.com.br/) via API e acompanhar o status até a conclusão.

## Contexto

O BIMachine possui uma franquia diária de transferência de dados (GB) associada ao plano contratado. A hipótese inicial deste projeto era que cargas disparadas via API ou manualmente pela interface **não** consumiriam essa franquia, ao contrário de cargas agendadas nativamente na plataforma, o que abriria espaço para um serviço próprio de agendamento que substituísse o agendamento nativo e evitasse o consumo de cota.

Essa hipótese foi testada disparando cargas reais via este client e acompanhando o painel de consumo do BIMachine em tempo real: **o consumo subiu proporcionalmente ao volume de dados transferido, independente do disparo ter sido via API.** Ou seja, a franquia é debitada pelo volume de dados movimentado, não pela origem do disparo: a premissa que motivou o projeto não se confirmou, e o plano de substituir o agendamento nativo por essa automação foi abandonado.

O código permanece aqui como referência de client HTTP para a API de cargas do BIMachine (autenticação, disparo, polling de status), caso seja útil para outro propósito no futuro.

## Estrutura da solução

```
src/
  BimachineLoadTrigger.Core/
    BimachineLoadClient.cs        Client HTTP para a API do BIMachine (disparo + status)
    IBimachineLoadClient.cs       Abstração implementada pelo client, usada por quem o consome
    Configuration/
      BimachineOptions.cs         Opções vindas de appsettings/user-secrets/variáveis de ambiente
    Constants/
      BimachineEndpoints.cs       Rotas da API, isoladas do código que faz a chamada HTTP
      BimachineMessages.cs        Mensagens de erro do client
    Models/
      ExecuteLoadResponse.cs, LoadStatusResponse.cs   DTOs das respostas da API
  BimachineLoadTrigger.Cli/       Aplicação de linha de comando que dispara uma carga e acompanha o status até concluir
tests/
  BimachineLoadTrigger.Core.Tests/   Testes unitários do client (xUnit)
```

A separação entre `Core` e `Cli` mantém o client HTTP reutilizável (testável isoladamente, sem depender de console/DI) mesmo que a interface de execução mude no futuro.

## Padrões

- `BimachineLoadClient` implementa `IBimachineLoadClient`; o `Cli` e os testes dependem da interface, não da implementação concreta, então trocar a implementação (ex: um client fake para testes de integração) não exige tocar em quem consome.
- `BimachineEndpoints` isola a construção das rotas da API (URL + query string) do código que efetivamente faz a chamada HTTP, então uma mudança na API (novo parâmetro, nova versão de rota) fica localizada num único lugar.
- Texto solto separado do código de orquestração: mensagens de erro do client ficam em `Constants/BimachineMessages.cs`, opções de configuração em `Configuration/BimachineOptions.cs`. As mensagens de log do `Program.cs` (`logger.LogInformation("...")`) ficam inline de propósito: é o padrão de logging estruturado do .NET, e mover o template pra outro arquivo quebraria a checagem de argumentos em tempo de compilação que o analisador de logging faz em cima do call site.

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- Uma chave de aplicação do BIMachine ([como gerar](https://support.bimachine.com/como-gerar-uma-chave-de-aplicacao/)), que requer usuário Master da conta
- O código do agendamento de carga (`loadCode`), encontrado na tela de administração de Origens/Dados da plataforma

## Configuração

As credenciais **não** ficam versionadas no repositório. Use [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) para desenvolvimento local:

```bash
cd src/BimachineLoadTrigger.Cli
dotnet user-secrets set "Bimachine:AppKey" "sua-chave-de-aplicacao"
dotnet user-secrets set "Bimachine:LoadCode" "codigo-do-agendamento"
```

Alternativamente, qualquer valor pode ser sobrescrito por variável de ambiente (útil para rodar em um agendador de tarefas futuramente, sem depender de user-secrets):

```bash
setx Bimachine__AppKey "sua-chave-de-aplicacao"
setx Bimachine__LoadCode "codigo-do-agendamento"
```

Configurações não sensíveis (URL base, intervalo/timeout do polling) ficam em [appsettings.json](src/BimachineLoadTrigger.Cli/appsettings.json).

## Executando

```bash
dotnet run --project src/BimachineLoadTrigger.Cli
```

O programa dispara a carga configurada, consulta o status periodicamente até ela terminar (ou até o timeout configurado) e encerra com código de saída `0` em caso de sucesso ou `1` em caso de erro/timeout.

## Testes

```bash
dotnet test
```

## Observações sobre a API

- A documentação pública do BIMachine ([API de Cargas](https://support.bimachine.com/api-cargas/)) não deixa claro se o endpoint de disparo exige apenas a `appKey` na URL ou também um token de autenticação (`/api/token-manager/`). Na prática, só `appKey` foi suficiente.
- **O `{schedulingCode}` do endpoint de status não é o `loadCode` usado para disparar a carga: é o `id` retornado pela chamada de `execute`.** A doc usa nomes parecidos para os dois parâmetros, mas são valores diferentes; confirmado testando manualmente contra a API real.
- Os campos `id`, `loadType` e `status` do endpoint de status podem vir como `null` (por exemplo, logo após o disparo, com `status: "NOT_STARTED"`); o client trata todos esses campos como opcionais.
- A carga é considerada concluída quando `endDate` é preenchido, e como erro quando `status` é `"ERROR"`.
- Disparar a mesma carga (`loadCode`) enquanto uma execução anterior ainda está em `NOT_STARTED`/em andamento resulta em `400 Bad Request`; a API não permite execuções concorrentes da mesma carga.
- Não confundir com o endpoint de exportação/envio de relatórios (`/api/export/schedulings/{id}/run`), que é uma funcionalidade diferente.
- Ao criar o agendamento na interface, use o tipo **"Api"** (não "Periódica") para garantir que ele só execute quando chamado por este client, sem rodar sozinho e contaminar o teste de consumo de franquia.

## Status

Arquivado. A hipótese que motivou o projeto (disparo via API não consome a franquia) foi testada e refutada: o consumo de dados é debitado independente da origem do disparo. O client funciona e está coberto por testes, mas não há planos de evoluir para um serviço de agendamento automático.
