# CQS i asynchroniczny messaging in-memory — plan implementacji

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dokończyć CQS i dodać asynchroniczny messaging między modułami na `System.Threading.Channels`, wzorując się na Confabie, oraz przenieść na CQS obszar `Hotel`.

**Architecture:** Układ jak w Confabie: `IMessageBroker` → `IAsyncMessageDispatcher` → `IMessageChannel` → `BackgroundDispatcher` → `IModuleClient` → `IEventDispatcher` / `ICommandDispatcher`. Bez RabbitMQ. Dispatchery są singletonami i tworzą scope na wywołanie. Komunikacja między modułami odbywa się przez kopie kontraktów tłumaczone przez JSON.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, EF Core 10, Scrutor 7, `System.Threading.Channels`, xUnit, Shouldly, NSubstitute.

**Spec:** Brak osobnej specyfikacji. Wymagania to sekcje „Global Constraints” i „Porównanie z Confabem”. Wzorzec: `/Users/maciejpawlowski/Code/Confab/src/Shared/Confab.Shared.Infrastructure/{Commands,Queries,Events,Modules,Messaging}`.

## Porównanie z Confabem

| Obszar | Confab | PSuite dziś | Decyzja |
|---|---|---|---|
| Komendy | Tylko `ICommand`, bez wyniku | Dodatkowo `ICommand<TResult>`, które przez dziedziczenie po `ICommand` psuje wybór przeciążenia | Zostaje PSuite: `ICommand<TResult>` dziedziczy po `IMessage` |
| Dispatchery | Singleton, `using` + `await` | Scoped, scope niezwalniany, brak `await` | Jak Confab, z `await using` |
| `CancellationToken` | Brak | Wszędzie | Zostaje PSuite |
| Nazwa metody query | `QueryAsync` | `DispatchAsync` | Zostaje PSuite |
| Skan handlerów | Scrutor 3.3 | Scrutor 7, domyślnie pomija klasy `internal` | PSuite: `publicOnly: false` |
| `EventDispatcher` | Wspólny scope, handlery równolegle | To samo | Odstępstwo: scope na handler, bezpieczne dla `DbContext` |
| `ModuleRegistry` | Kolekcje zainicjalizowane | `null`, więc NRE | Jak Confab |
| Skan typów wiadomości | `IsClass` | Brak filtra | Jak Confab, dodatkowo bez abstrakcyjnych |
| Broadcast komend | Tak | Tak, ale `GetMethod` trafia na dwa `SendAsync` | Jak Confab, tylko komendy bez wyniku; metoda wybierana po sygnaturze |
| Filtr `ReceiverType != messageType` | Tak | Tak | Jak Confab: każdy moduł ma własną kopię kontraktu |
| `ModuleSubscriber`, `UseModuleRequests` | Są | Brak | Jak Confab; PSuite ma też wariant bez odpowiedzi |
| `MessageBroker` | Channel lub moduł + RabbitMQ | Sam interfejs | Jak Confab, bez RabbitMQ |
| Channel | Unbounded | Brak | Odstępstwo: bounded, `Wait`, pojemność z opcji |
| Zamykanie aplikacji | Porzuca kolejkę | Brak | Odstępstwo: opróżnienie kolejki w `StopAsync` |
| Błąd handlera w tle | `try/catch` na wiadomość | Brak | Jak Confab, log z typem wiadomości |
| `useBackgroundDispatcher` | `false` w konfiguracji | Brak | `true`, bo asynchroniczność jest celem |

## Global Constraints

- SDK z `global.json`: `10.0.201`; projekty `net10.0`, `ImplicitUsings` i `Nullable` włączone.
- Bez nowych zależności produkcyjnych; bez RabbitMQ i Convey.
- Każda metoda asynchroniczna kontraktu przyjmuje `CancellationToken cancellationToken = default`.
- Publikacja eventu po udanym `SaveChangesAsync` używa `CancellationToken.None`, żeby rozłączenie klienta nie zgubiło eventu po zapisie.
- Eventy lokalne w module idą przez `IEventDispatcher`, a integracyjne między modułami przez `IMessageBroker`. Każdy moduł ma własną kopię kontraktu integracyjnego.
- Przy pełnym kanale publikujący czeka; utrata wiadomości przy awarii procesu jest akceptowana do czasu dodania outboxa.
- Komenda implementuje albo `ICommand`, albo `ICommand<TResult>`, nigdy oba. Komendy z wynikiem nie są broadcastowane, bo mają jednego odbiorcę; między modułami idą przez `IModuleClient.SendAsync`.
- Handlery, komendy, query i eventy w `Core` są `internal sealed`; komendy, query i eventy to rekordy.
- Styl: file-scoped namespaces, primary constructors.
- Praca na gałęzi `in-memory-messaging`.
- Paczki testowe (restore sprawdzony): `Microsoft.NET.Test.Sdk` 17.14.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `Shouldly` 4.3.0, `NSubstitute` 5.3.0, `Microsoft.EntityFrameworkCore.InMemory` 10.0.5.
- Nazwy testów w stylu Confaba: `given_x_should_y`.

## Review Focus

1. Handler rzuca wyjątek w tle: wiadomość jest logowana, a następna przetworzona (Task 4).
2. Zamknięcie aplikacji z niepustą kolejką: wszystkie wiadomości trafiają do odbiorców (Task 4).
3. Pełny kanał: publikujący czeka, a anulowanie requestu przerywa oczekiwanie (Task 4).
4. Handler rzucający synchronicznie: do `GlobalExceptionHandler` dociera oryginalny wyjątek, nie `TargetInvocationException` (Task 1).
5. Asynchroniczny handler używający scoped zależności: scope żyje do końca handlera (Task 1).

## Poza zakresem

Outbox (planowany na później), retry i dead-letter, propagacja kontekstu użytkownika przez kanał, dekorator transakcyjny i domain events z Confaba, migracja `Room` i `Employee`, naprawa ignorowanego `id` z trasy w `PUT` (zachowanie bez zmian).

---

### Task 1: Projekt testowy oraz dispatchery komend i query

**Files:**
- Create: `tests/Shared/PSuite.Shared.Infrastructure.Tests/PSuite.Shared.Infrastructure.Tests.csproj`
- Create: `tests/Shared/PSuite.Shared.Infrastructure.Tests/ScopedProbe.cs`
- Create: `tests/Shared/PSuite.Shared.Infrastructure.Tests/Commands/CommandDispatcherTests.cs`
- Create: `tests/Shared/PSuite.Shared.Infrastructure.Tests/Queries/QueryDispatcherTests.cs`
- Modify: `PSuite.sln`
- Modify: `src/Shared/PSuite.Infrastructure/Extensions.cs:17` (dodatkowy `InternalsVisibleTo`)
- Modify: `src/Shared/PSuite.Abstractions/Commands/ICommand.cs:7`
- Modify: `src/Shared/PSuite.Infrastructure/Commands/CommandDispatcher.cs`, `Commands/Extensions.cs`
- Modify: `src/Shared/PSuite.Infrastructure/Queries/QueryDispatcher.cs`, `Queries/Extensions.cs`

**Interfaces:**
- Produces: `public sealed class ScopedProbe : IDisposable { public bool Disposed { get; } }`
- Produces: `public interface ICommand<TResult> : IMessage;` (bez dziedziczenia po `ICommand`)
- Produces: `ICommandDispatcher` z dwoma niezmienionymi przeciążeniami: `Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : class, ICommand` oraz `Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)`
- Produces: `ICommandHandler<TCommand>` i `ICommandHandler<TCommand, TResult>` bez zmian sygnatur
- Produces: `ICommandDispatcher` i `IQueryDispatcher` zarejestrowane jako singletony przez `AddCommands(IList<Assembly>)` i `AddQueries(IList<Assembly>)`
- Produces: `IQueryDispatcher.DispatchAsync<TResult>` bez zmiany sygnatury

- [ ] **Step 1: Utwórz projekt testowy**

Csproj: `net10.0`, `IsPackable=false`, paczki z Global Constraints oprócz EF InMemory, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, referencja do `PSuite.Shared.Infrastructure.csproj`. W `Extensions.cs` dodaj `[assembly: InternalsVisibleTo("PSuite.Shared.Infrastructure.Tests")]`.

```bash
dotnet sln PSuite.sln add tests/Shared/PSuite.Shared.Infrastructure.Tests --solution-folder tests
```

- [ ] **Step 2: Napisz testy, które nie przechodzą**

Provider budowany z `ValidateScopes = true`. Handlery testowe rejestrowane przez `AddCommands` / `AddQueries` z assembly testów.

```csharp
// CommandDispatcherTests
[Fact] given_internal_handler_should_invoke_it();
    // await dispatcher.SendAsync(new InternalCmd()); InternalCmdHandler.Handled.ShouldBeTrue();
[Fact] given_async_handler_scope_should_stay_open_until_it_finishes();
    // handler po await Task.Yield() zapisuje probe.Disposed
    // DisposedDuringHandler.ShouldBeFalse(); LastProbe.Disposed.ShouldBeTrue();
[Fact] given_cancellation_token_should_pass_it_to_handler();
[Fact] given_missing_handler_should_throw();
    // await Should.ThrowAsync<InvalidOperationException>(() => dispatcher.SendAsync(new NoHandlerCmd()));
[Fact] dispatcher_should_resolve_from_root_provider();
    // root.GetRequiredService<ICommandDispatcher>().ShouldNotBeNull();
[Fact] given_result_command_inferred_overload_should_return_result();
    // Guid result = await dispatcher.SendAsync(new CreateThing(id), token); result.ShouldBe(id);
[Fact] given_result_command_typed_as_interface_should_return_result();
    // ICommand<Guid> command = new CreateThing(id); (await dispatcher.SendAsync(command)).ShouldBe(id);
[Fact] given_result_command_should_pass_cancellation_token_to_handler();
[Fact] given_result_command_scope_should_stay_open_until_handler_finishes();
[Fact] given_result_handler_throwing_synchronously_should_surface_original_exception();
    // await Should.ThrowAsync<TestDomainException>(() => dispatcher.SendAsync(new ThrowingResultCmd()));
[Fact] given_result_command_without_handler_should_throw();
    // await Should.ThrowAsync<InvalidOperationException>(() => dispatcher.SendAsync(new NoHandlerResultCmd()));

// QueryDispatcherTests
[Fact] given_internal_handler_should_return_result();
[Fact] given_async_handler_scope_should_stay_open_until_it_finishes();
[Fact] given_handler_throwing_synchronously_should_surface_original_exception();
    // await Should.ThrowAsync<TestDomainException>(() => dispatcher.DispatchAsync(new ThrowingQuery()));
[Fact] given_missing_handler_should_throw();
[Fact] dispatcher_should_resolve_from_root_provider();
```

`given_missing_handler_should_throw` dla query oczekuje `InvalidOperationException`.

- [ ] **Step 3: Uruchom testy i potwierdź niepowodzenie**

Run: `dotnet test tests/Shared/PSuite.Shared.Infrastructure.Tests`
Expected: FAIL, m.in. `given_internal_handler_should_invoke_it` (brak rejestracji), test scope'u oraz błąd kompilacji `CS0029` w `given_result_command_inferred_overload_should_return_result`. Błąd kompilacji zatrzymuje cały przebieg, więc pozostałe testy zobaczysz na czerwono dopiero po Step 4.

- [ ] **Step 4: Rozdziel hierarchię komend**

W `ICommand.cs:7`: `public interface ICommand<TResult> : IMessage;`. Dzięki temu przeciążenie z ograniczeniem `ICommand` odpada dla komend z wynikiem i kompilator wybiera `SendAsync<TResult>`.

- [ ] **Step 5: Popraw dispatchery i rejestrację**

- `CommandDispatcher` i `QueryDispatcher` stają się `internal sealed`, metody `async`, scope przez `await using var scope = serviceProvider.CreateAsyncScope()`, handler przez `GetRequiredService`.
- Przeciążenie z wynikiem w `CommandDispatcher` oraz `QueryDispatcher` wołają `method.Invoke(handler, BindingFlags.DoNotWrapExceptions, null, [message, cancellationToken], null)` i awaitują wynik jako `Task<TResult>`.
- W obu `Extensions`: `AddSingleton` dla dispatchera oraz `publicOnly: false` w każdym `AddClasses`; w `Commands/Extensions.cs` zostają oba skany (`ICommandHandler<>` i `ICommandHandler<,>`).

- [ ] **Step 6: Uruchom testy**

Run: `dotnet test tests/Shared/PSuite.Shared.Infrastructure.Tests`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add PSuite.sln tests/Shared src/Shared
git commit -m "fix: align command and query dispatchers with Confab"
```

### Task 2: Izolacja handlerów eventów

**Files:**
- Modify: `src/Shared/PSuite.Infrastructure/Events/EventDispatcher.cs`
- Modify: `src/Shared/PSuite.Infrastructure/Events/Extensions.cs`
- Create: `tests/Shared/PSuite.Shared.Infrastructure.Tests/Events/EventDispatcherTests.cs`

**Interfaces:**
- Consumes: `ScopedProbe` z Task 1
- Produces: `IEventDispatcher` jako singleton przez `AddEvents(IList<Assembly>)`

- [ ] **Step 1: Napisz testy, które nie przechodzą**

```csharp
[Fact] given_two_handlers_each_should_get_own_scoped_dependency();
    // ReferenceEquals(HandlerA.Probe, HandlerB.Probe).ShouldBeFalse();
[Fact] given_internal_handler_should_invoke_it();
[Fact] given_failing_handler_other_handlers_should_still_run();
    // await Should.ThrowAsync<TestDomainException>(...); SurvivingHandler.Handled.ShouldBeTrue();
[Fact] given_no_handlers_should_complete();
[Fact] given_async_handler_scope_should_stay_open_until_it_finishes();
[Fact] dispatcher_should_resolve_from_root_provider();
```

- [ ] **Step 2: Uruchom testy i potwierdź niepowodzenie**

Run: `dotnet test tests/Shared/PSuite.Shared.Infrastructure.Tests --filter FullyQualifiedName~EventDispatcherTests`
Expected: FAIL na współdzielonej zależności i rejestracji `internal`.

- [ ] **Step 3: Zaimplementuj scope na handler w `PublishAsync`**

`EventDispatcher` jako `internal sealed`, w `Extensions` `AddSingleton` i `publicOnly: false`. Algorytm:

```csharp
int count;
await using (var probe = serviceProvider.CreateAsyncScope())
    count = probe.ServiceProvider.GetServices<IEventHandler<TEvent>>().Count();

await Task.WhenAll(Enumerable.Range(0, count).Select(async index =>
{
    await using var scope = serviceProvider.CreateAsyncScope();
    var handler = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>().ElementAt(index);
    await handler.HandleAsync(@event, cancellationToken);
}));
```

- [ ] **Step 4: Uruchom testy**

Run: `dotnet test tests/Shared/PSuite.Shared.Infrastructure.Tests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/Shared/PSuite.Infrastructure/Events tests/Shared
git commit -m "fix: isolate event handlers in separate scopes"
```

### Task 3: Komunikacja między modułami

**Files:**
- Create: `src/Shared/PSuite.Infrastructure/Modules/ModuleSubscriber.cs`
- Modify: `src/Shared/PSuite.Infrastructure/Modules/ModuleRegistry.cs:5-6`
- Modify: `src/Shared/PSuite.Infrastructure/Modules/ModuleClient.cs:19`
- Modify: `src/Shared/PSuite.Infrastructure/Modules/Extensions.cs`
- Modify: `src/Shared/PSuite.Infrastructure/Extensions.cs`
- Create: `tests/Shared/PSuite.Shared.Infrastructure.Tests/Modules/ModuleCommunicationTests.cs`

**Interfaces:**
- Consumes: `AddCommands`, `AddEvents`, `ScopedProbe`
- Produces: `internal sealed class ModuleSubscriber(IModuleRegistry moduleRegistry, IServiceProvider serviceProvider) : IModuleSubscriber`
- Produces: `public static IModuleSubscriber UseModuleRequests(this IApplicationBuilder app)` w `public static class Extensions`
- Produces: singletony `IModuleClient`, `IModuleSerializer`, `IModuleSubscriber`, `IModuleRegistry`

- [ ] **Step 1: Napisz testy, które nie przechodzą**

Kontrakty w dwóch namespace'ach testowych: `ModuleA.ThingHappened(Guid Id)` i `ModuleB.ThingHappened(Guid Id)`, oba `IEvent`, oraz analogiczne `GetThing` / `ThingDto`. Provider z `ValidateScopes = true`.

```csharp
[Fact] registry_should_resolve_from_root_provider();
[Fact] given_event_should_deliver_translated_copy_to_other_module();
    // await client.PublishAsync(new ModuleA.ThingHappened(id)); ModuleBHandler.Received.Id.ShouldBe(id);
[Fact] given_event_publisher_own_handler_should_not_be_invoked();
[Fact] given_no_receivers_publish_should_complete();
[Fact] given_command_should_deliver_translated_copy_to_other_module();
[Fact] given_result_command_publish_should_not_invoke_its_handler();
    // await client.PublishAsync(new ModuleA.CreateThing(id)); ModuleBResultHandler.Handled.ShouldBeFalse();
[Fact] given_result_command_subscription_send_should_return_translated_result();
    // subscriber.Subscribe<ModuleB.CreateThing, ModuleB.ThingDto>("b/create",
    //     (c, sp, ct) => sp.GetRequiredService<ICommandDispatcher>().SendAsync(c, ct));
    // ModuleB.CreateThing : ICommand<ModuleB.ThingDto>
    // (await client.SendAsync<ModuleA.ThingDto>("b/create", new ModuleA.CreateThing(id))).Id.ShouldBe(id);
[Fact] given_subscription_send_should_translate_request_and_response();
    // subscriber.Subscribe<ModuleB.GetThing, ModuleB.ThingDto>("b/thing", (r, sp, ct) => Task.FromResult(new ModuleB.ThingDto(r.Id)));
    // (await client.SendAsync<ModuleA.ThingDto>("b/thing", new ModuleA.GetThing(id))).Id.ShouldBe(id);
[Fact] given_subscription_without_response_send_should_invoke_action();
[Fact] given_unknown_path_send_should_throw();
    // (await Should.ThrowAsync<InvalidOperationException>(...)).Message.ShouldContain("b/missing");
[Fact] given_subscription_action_scope_should_be_disposed_after_it_finishes();
```

- [ ] **Step 2: Uruchom testy i potwierdź niepowodzenie**

Run: `dotnet test tests/Shared/PSuite.Shared.Infrastructure.Tests --filter FullyQualifiedName~ModuleCommunicationTests`
Expected: FAIL — `ModuleSubscriber` nie istnieje, a `UseModuleRequests` nie jest dostępne.

- [ ] **Step 3: Zaimplementuj `ModuleSubscriber`**

Oba przeciążenia wołają `AddRequestAction`; każde wywołanie działa w `await using` scope z `CreateAsyncScope()`. Wariant bez odpowiedzi rejestruje `typeof(object)` jako typ odpowiedzi i zwraca `null!`.

- [ ] **Step 4: Podepnij rejestrację**

- `ModuleRegistry`: pola `= []`.
- `ModuleClient.cs:19`: `return result is null ? null! : TranslateType<TResult>(result);`
- `Modules/Extensions.cs`: klasa `public static`; odkomentuj rejestracje `IModuleClient` i `IModuleSerializer`; zarejestruj `IModuleSubscriber`; dodaj `UseModuleRequests`; filtry typów `t is { IsClass: true, IsAbstract: false } && ...`.
- `Modules/Extensions.cs:59`: wybierz przeciążenie bez wyniku po sygnaturze: `commandDispatcherType.GetMethod(nameof(commandDispatcher.SendAsync), 1, [Type.MakeGenericMethodParameter(0), typeof(CancellationToken)])!`.
- `Extensions.cs`: `services.AddModuleRequests(assemblyList);` po `AddModuleInfo`.

- [ ] **Step 5: Uruchom testy**

Run: `dotnet test tests/Shared/PSuite.Shared.Infrastructure.Tests`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add src/Shared/PSuite.Infrastructure tests/Shared
git commit -m "feat: wire module requests and broadcasts"
```

### Task 4: Asynchroniczny messaging na Channels

**Files:**
- Create w `src/Shared/PSuite.Infrastructure/Messaging/`: `Brokers/MessageBroker.cs`, `Dispatchers/IAsyncMessageDispatcher.cs`, `Dispatchers/AsyncMessageDispatcher.cs`, `Dispatchers/IMessageChannel.cs`, `Dispatchers/MessageChannel.cs`, `Dispatchers/BackgroundDispatcher.cs`, `Dispatchers/MessagingOptions.cs`, `Extensions.cs`
- Modify: `src/Shared/PSuite.Infrastructure/PSuite.Shared.Infrastructure.csproj:26-28` (usuń wpis `Folder`)
- Modify: `src/Shared/PSuite.Infrastructure/Extensions.cs`
- Modify: `src/Bootstrapper/PSuite.Bootstrapper/appsettings.json`
- Create w `tests/Shared/PSuite.Shared.Infrastructure.Tests/Messaging/`: `MessageBrokerTests.cs`, `BackgroundDispatcherTests.cs`, `MessagingRegistrationTests.cs`

**Interfaces:**
- Consumes: `IModuleClient.PublishAsync(object, CancellationToken)` i rejestracje z Task 3
- Produces:

```csharp
public sealed class MessagingOptions { public bool UseBackgroundDispatcher { get; set; } public int Capacity { get; set; } = 1000; }
public interface IMessageChannel { ChannelReader<IMessage> Reader { get; } ChannelWriter<IMessage> Writer { get; } }
internal sealed class MessageChannel(MessagingOptions options) : IMessageChannel;
internal interface IAsyncMessageDispatcher { Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default) where TMessage : class, IMessage; }
internal sealed class MessageBroker(IModuleClient moduleClient, IAsyncMessageDispatcher asyncMessageDispatcher, MessagingOptions messagingOptions) : IMessageBroker;
internal sealed class BackgroundDispatcher(IMessageChannel messageChannel, IModuleClient moduleClient, ILogger<BackgroundDispatcher> logger) : BackgroundService;
internal static IServiceCollection AddMessaging(this IServiceCollection services);
```

- [ ] **Step 1: Napisz testy, które nie przechodzą**

`IModuleClient` to mock NSubstitute, a `MessageChannel` jest prawdziwy.

```csharp
// MessageBrokerTests
[Fact] given_background_disabled_should_publish_through_module_client();
    // await moduleClient.Received(1).PublishAsync(message, token);
[Fact] given_background_enabled_should_write_to_channel_only();
    // channel.Reader.TryRead(out var written).ShouldBeTrue(); written.ShouldBe(message);
    // await moduleClient.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
[Fact] given_null_or_empty_messages_should_do_nothing();
[Fact] given_array_with_null_should_publish_remaining();
[Fact] given_full_channel_publish_should_honour_cancellation();
    // Capacity = 1; drugi publish z CTS 50 ms: await Should.ThrowAsync<OperationCanceledException>(...);

// BackgroundDispatcherTests
[Fact] given_failing_message_should_process_next();
[Fact] given_stop_with_queued_messages_should_deliver_all();
    // 3 wiadomości, moduleClient z await Task.Delay(50), po StopAsync: otrzymano 3

// MessagingRegistrationTests (IConfiguration z AddInMemoryCollection)
[Fact] given_enabled_should_register_hosted_dispatcher();
    // services.ShouldContain(d => d.ImplementationType == typeof(BackgroundDispatcher));
[Fact] given_missing_section_should_not_register_hosted_dispatcher();
[Fact] given_capacity_in_config_should_bind_it();
[Fact] published_event_should_reach_other_module_handler_through_channel();
    // wszystkie Add* + uruchomiony BackgroundDispatcher; broker.PublishAsync(new ModuleA.ThingHappened(id));
    // await handlerCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));
```

- [ ] **Step 2: Uruchom testy i potwierdź niepowodzenie**

Run: `dotnet test tests/Shared/PSuite.Shared.Infrastructure.Tests --filter FullyQualifiedName~Messaging`
Expected: FAIL — typy nie istnieją.

- [ ] **Step 3: Zaimplementuj kanał i broker**

- `MessageChannel`: `Channel.CreateBounded<IMessage>(new BoundedChannelOptions(options.Capacity) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true })`.
- `AsyncMessageDispatcher`: `WriteAsync(message, cancellationToken)`.
- `MessageBroker`: wersja z pojedynczą wiadomością deleguje do tablicowej. Tablicowa ignoruje `null`, a potem działa jak w Confabie, bez `IBusPublisher`.

- [ ] **Step 4: Zaimplementuj `BackgroundDispatcher`**

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await foreach (var message in messageChannel.Reader.ReadAllAsync(CancellationToken.None))
    {
        try { await moduleClient.PublishAsync(message, CancellationToken.None); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to dispatch message {MessageType}.", message.GetType().Name);
        }
    }
}

public override async Task StopAsync(CancellationToken cancellationToken)
{
    messageChannel.Writer.TryComplete();
    await base.StopAsync(cancellationToken);
}
```

- [ ] **Step 5: Zarejestruj messaging**

`AddMessaging`: singletony `IMessageBroker`, `IMessageChannel`, `IAsyncMessageDispatcher`, opcje z `services.GetOptions<MessagingOptions>("messaging")`, a `AddHostedService<BackgroundDispatcher>()` tylko przy `UseBackgroundDispatcher`. W `AddInfrastructure` wywołaj po `AddEvents`. W `appsettings.json`:

```json
"messaging": { "useBackgroundDispatcher": true, "capacity": 1000 }
```

- [ ] **Step 6: Uruchom testy**

Run: `dotnet test tests/Shared/PSuite.Shared.Infrastructure.Tests`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/Shared/PSuite.Infrastructure src/Bootstrapper/PSuite.Bootstrapper/appsettings.json tests/Shared
git commit -m "feat: add in-memory asynchronous messaging with channels"
```

### Task 5: Pionowy wycinek `Hotel` na CQS

**Files:**
- Create w `src/Modules/Configuration/PSuite.Modules.Configuration.Core/`: `Commands/{CreateHotel,UpdateHotel,DeleteHotel}.cs`, `Commands/Handlers/{CreateHotelHandler,UpdateHotelHandler,DeleteHotelHandler}.cs`, `Queries/{GetHotels,GetHotel}.cs`, `Queries/Handlers/{GetHotelsHandler,GetHotelHandler}.cs`, `Events/HotelCreated.cs`
- Delete: `Services/HotelService.cs`, `Services/IHotelService.cs`
- Modify: `Core/Extensions.cs:11` (dodatkowy `InternalsVisibleTo`), `Core/Extensions.cs:38` (usuń rejestrację)
- Modify: `src/Modules/Configuration/PSuite.Modules.Configuration.Api/Endpoints/HotelApi.cs`
- Create: `tests/Modules/Configuration/PSuite.Modules.Configuration.Tests/PSuite.Modules.Configuration.Tests.csproj`, `Hotels/HotelCommandHandlersTests.cs`, `Hotels/HotelQueryHandlersTests.cs`
- Modify: `PSuite.sln`

**Interfaces:**
- Consumes: `ICommandDispatcher`, `IQueryDispatcher`, `IMessageBroker` z wcześniejszych tasków
- Produces:

```csharp
internal sealed record CreateHotel(string? Name) : ICommand<Guid>;
internal sealed record UpdateHotel(Guid Id, string? Name) : ICommand;
internal sealed record DeleteHotel(Guid Id) : ICommand;
internal sealed record GetHotels : IQuery<IEnumerable<HotelDto>>;
internal sealed record GetHotel(Guid Id) : IQuery<HotelDetailsDto>;
internal sealed record HotelCreated(Guid Id, string? Name) : IEvent;
internal sealed class CreateHotelHandler(ConfigurationDbContext dbContext, IMessageBroker messageBroker) : ICommandHandler<CreateHotel, Guid>;
```

Pozostałe handlery przyjmują tylko `ConfigurationDbContext`.

- [ ] **Step 1: Utwórz projekt testowy**

Paczki z Global Constraints, referencja do `Core`, w `Core/Extensions.cs` dodaj `[assembly: InternalsVisibleTo("PSuite.Modules.Configuration.Tests")]`.

```bash
dotnet sln PSuite.sln add tests/Modules/Configuration/PSuite.Modules.Configuration.Tests --solution-folder tests
```

- [ ] **Step 2: Napisz testy, które nie przechodzą**

`ConfigurationDbContext` z `UseInMemoryDatabase(Guid.NewGuid().ToString())`, `IMessageBroker` jako mock.

```csharp
[Fact] given_create_should_persist_hotel_return_id_and_publish_event();
    // var id = await handler.HandleAsync(new CreateHotel("Grand"), token);
    // var hotel = await dbContext.Hotels.SingleAsync(); hotel.Id.ShouldBe(id); hotel.Name.ShouldBe("Grand");
    // await broker.Received(1).PublishAsync(Arg.Is<IMessage>(m => m is HotelCreated), CancellationToken.None);
[Fact] given_save_failure_should_not_publish_event();
    // zwolniony dbContext: await Should.ThrowAsync<ObjectDisposedException>(...); broker.DidNotReceiveWithAnyArgs()
[Fact] given_update_should_rename_hotel();
[Fact] given_missing_hotel_update_should_throw(); // HotelNotFoundException
[Fact] given_delete_should_remove_hotel();
[Fact] given_hotel_with_room_delete_should_throw(); // CannotDeleteHotelException
[Fact] given_missing_hotel_delete_should_throw(); // HotelNotFoundException
[Fact] given_hotels_get_all_should_return_dtos();
[Fact] given_hotel_with_rooms_get_should_return_details();
[Fact] given_missing_hotel_get_should_throw(); // HotelNotFoundException
```

- [ ] **Step 3: Uruchom testy i potwierdź niepowodzenie**

Run: `dotnet test tests/Modules/Configuration/PSuite.Modules.Configuration.Tests`
Expected: FAIL — typy nie istnieją.

- [ ] **Step 4: Przenieś logikę do handlerów**

Logika 1:1 z `HotelService`, z przekazaniem `cancellationToken` do zapytań EF i `SaveChangesAsync`. `CreateHotelHandler` po `SaveChangesAsync` woła `messageBroker.PublishAsync(new HotelCreated(hotel.Id, hotel.Name), CancellationToken.None)` i zwraca `hotel.Id`.

- [ ] **Step 5: Przepnij endpointy i usuń serwis**

Trasy, `WithName` i `RequireProtectedResource` bez zmian. Każdy lambda przyjmuje dodatkowo `CancellationToken ct`. Jedyna zmiana kontraktu: `POST` zwraca `id` utworzonego hotelu w ciele odpowiedzi 200.

| Endpoint | Wywołanie |
|---|---|
| `POST ""` | `commands.SendAsync(new CreateHotel(request.Name), ct)` |
| `PUT "/{id:guid}"` | `commands.SendAsync(new UpdateHotel(request.Id, request.Name), ct)` |
| `DELETE "/{id:guid}"` | `commands.SendAsync(new DeleteHotel(id), ct)` |
| `GET ""` | `queries.DispatchAsync(new GetHotels(), ct)` |
| `GET "/{id:guid}"` | `queries.DispatchAsync(new GetHotel(id), ct)` |

- [ ] **Step 6: Uruchom build i wszystkie testy**

Run: `dotnet build PSuite.sln -nologo -v q`
Expected: `0 Error(s)` i brak `CS0649` / `CS8618` dla `ModuleRegistry`.

Run: `dotnet test PSuite.sln`
Expected: PASS we wszystkich projektach.

- [ ] **Step 7: Sprawdź start aplikacji**

Uruchom `src/Bootstrapper/PSuite.Bootstrapper` w środowisku Development, z zależnościami z `docker/`, jeśli są wymagane.
Expected: host startuje bez błędów walidacji DI, `GET /` zwraca `PSuite API!`, a w logach nie ma błędów `BackgroundDispatcher`. Jeśli dostępny jest token Keycloak: `POST configuration-module/hotels` zapisuje hotel.

- [ ] **Step 8: Commit**

```bash
git add PSuite.sln src/Modules/Configuration tests/Modules
git commit -m "feat: move hotel configuration to CQS"
```
