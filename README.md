# AgentCleanArchDemo

Projeto de referência em **.NET 10** demonstrando como integrar o **Microsoft Agent Framework (`Microsoft.Agents.AI`)** e **Ollama (`OllamaSharp`)** em uma arquitetura em camadas, mantendo o agente isolado da infraestrutura e usando **Razor Partial Views** para renderizar UI generativa.

## Arquitetura e dependências

```text
MyApp.Domain          Regras e entidades puras (sem dependências)
MyApp.Application     ──► MyApp.Domain (casos de uso, DTOs e contratos)
MyApp.Infrastructure  ──► MyApp.Application (repositório em memória)
MyApp.AI              ──► MyApp.Application (MAF, OllamaSharp, tools e Contracts)
MyApp.Web             ──► Application, Infrastructure, AI (Razor, controllers e API)
```

Principais decisões do desenho:

- **Isolamento da IA:** `MyApp.AI` não referencia `MyApp.Infrastructure` nem conhece HTML/Razor. As *tools* em `OrderTools` apenas delegam para `IOrderService` na `Application`.
- **Generative UI tipada:** `OrderTools` e `InteractionTools` registram blocos tipados (`TableBlock`, `CardBlock`, `AlertBlock`, `AskFormBlock`, `AskInputBlock`, `AskChoiceBlock`) em `ResponseBlockCollector` (escopo por requisição).
- **Renderização em Razor Partial Views:** `HomeController` (`POST /chat/turn`) mapeia cada `ResponseBlock` via *pattern matching* em `UiBlockMapper` para sua Partial View em `Views/Shared/UiBlocks/` e devolve `_BotMessage.cshtml` pronto em uma única resposta.
- **Atualização seletiva da barra lateral (`OrdersMutated`):** apenas *tools* de escrita (`CreateOrderAsync`, `CancelOrderAsync`) marcam `OrdersMutated = true`. Quando isso ocorre, a mesma resposta de `/chat/turn` inclui `<template data-orders-oob>` com `_OrdersList.cshtml` atualizado, evitando requisições extras.

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Ollama](https://ollama.com/) rodando localmente com um modelo que suporte *tool calling*:

```bash
ollama pull qwen3:8b
ollama serve
```

O endpoint e o modelo podem ser ajustados em `src/MyApp.Web/appsettings.json` (`AI:Ollama`).

## Como executar

```bash
dotnet run --project src/MyApp.Web
```

Acesse a interface web em:

```text
http://localhost:5201
```

## Endpoints principais

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/` | Interface de chat com lista inicial de pedidos |
| `POST` | `/chat/turn` | Executa um turno do agente e retorna a Partial View `_BotMessage.cshtml` |
| `POST` | `/agent/orders` | Versão JSON do agente (`{ message, sessionId }`) para uso via API |
| `GET` | `/orders` | Lista pedidos em JSON (fluxo REST tradicional sem IA) |

Exemplo via `curl`:

```bash
curl -X POST http://localhost:5201/agent/orders \
  -H "Content-Type: application/json" \
  -d '{"message": "Quais pedidos existem hoje? E tenta cancelar o ORD-1002."}'
```
