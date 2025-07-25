# MessageBus

MessageBus é um framework simples em .NET para publicar e consumir mensagens de maneira desacoplada.
Este repositório contem a implementação base e uma integração com o **Google Cloud Pub/Sub**.

## Objetivo

Fornecer uma abstração agnóstica de transporte, permitindo publicar e consumir eventos utilizando provedores diferentes.
Atualmente o projeto disponibiliza o pacote `MessageBus.PubSub` com integração ao GCP Pub/Sub, mas pode ser estendido para outros brokers como Kafka.

## Instalação

1. Adicione a referência para os pacotes NuGet.
   ```bash
   dotnet add package MessageBus
   dotnet add package MessageBus.PubSub
   ```
2. Configure o Pub/Sub no `appsettings.json` ou via código.

Exemplo de configuração:
```json
{
  "MessageBus": {
    "PubSub": {
      "ProjectId": "meu-projeto",
      "JsonCredentials": "<conteúdo do json>",
      "Publishing": {
        "Prefix": "demo",
        "MessageRetentionDurationDays": 7,
        "Topics": [ "demo-topic" ]
      },
      "Subscription": {
        "Sufix": "leitor",
        "MessageRetentionDurationDays": 7
      }
    }
  }
}
```

Em `Program.cs`:
```csharp
var builder = WebApplication.CreateBuilder(args);

// registra o message bus usando a configuração do appsettings
builder.Services.AddPubSub(builder.Configuration);

var app = builder.Build();
```

Para publicar mensagens:
```csharp
var bus = app.Services.GetRequiredService<IMessageBus>();
await bus.Publish("demo-topic", new MeuEvento { Data = "teste" });
```

Para consumir, defina um consumer e registre-o:
```csharp
class MeuEventoConsumer : IMessageConsumer<MeuEvento>
{
    public Task Handler(MessageContext<MeuEvento> context, CancellationToken ct)
    {
        // lógica do consumidor
        return Task.CompletedTask;
    }
}

builder.Services
    .AddPubSub(builder.Configuration)
    .AddConsumer<MeuEvento, MeuEventoConsumer>("demo-topic");
```

## Extensibilidade

O pacote `MessageBus` define as interfaces e classes base utilizadas pela integração com o Pub/Sub.
Para suportar outro broker (• exemplo: Kafka) crie um novo projeto que referencie `MessageBus` e implemente:

1. Uma classe derivada de `MessageBus` para publicar mensagens.
2. Uma classe derivada de `MessageConsumer<TEvent,TConsumer>` para processar mensagens do broker.
3. Extensões de `IServiceCollection` semelhantes a `AddPubSub` para registrar sua implementação e dependências.

Dessa forma é possível adicionar novos provedores mantendo a mesma API para publicação/consumo.

## Pipeline de CI/CD

Este repositório inclui um fluxo de trabalho do GitHub Actions (`.github/workflows/ci.yml`) que:
1. Restaura as dependências e executa `dotnet test` em todos os commits.
2. Em commits com tag no formato `v*.*.*`, cria pacotes NuGet e publica-os no repositório configurado.

Certifique-se de definir o segredo `NUGET_API_KEY` no repositório para habilitar a publicação automática.

## Execução dos testes

Para executar os testes locais utilize:
```bash
dotnet test --verbosity normal
```
Os testes de integração com o Pub/Sub são executados apenas se houver configuração de emulador (`PUBSUB_EMULATOR_HOST`) ou credenciais (`GOOGLE_APPLICATION_CREDENTIALS`).

## Licença

Este projeto está disponível sob a licença MIT.
