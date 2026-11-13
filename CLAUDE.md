# PSuite

System do zarządzania hotelem zbudowany jako **modularny monolit** na .NET 10 (ASP.NET Core Minimal API, EF Core 10 + PostgreSQL, Keycloak, Redis). Dokumentacja i plany są po polsku; kod, nazwy i komentarze w kodzie po angielsku.

> **Każdy moduł ma własny `CLAUDE.md`** w katalogu `src/Modules/<Moduł>/CLAUDE.md`. Przed pracą w module przeczytaj go. Ten plik opisuje tylko rzeczy wspólne dla całego repozytorium.

## Model domeny

Model domeny (wynik Event Stormingu, wszystkie konteksty i decyzje zespołu) jest w `docs/domain.md`. Czytaj odpowiednią sekcję przed pracą nad logiką biznesową.

- `[Decyzja]` i `[Tablica]` są wiążące (pierwszeństwo: Decyzja > Tablica > Wniosek). `[Wniosek]` wymaga potwierdzenia. `[Otwarte]` i `[Hot spot]` — nie implementuj, zapytaj.
- Dla modułów zrealizowanych źródłem prawdy jest kod; rozbieżność oznacza aktualizację dokumentu, nie kodu.
- Projekt to PoC — wybieraj najprostsze rozwiązanie spełniające decyzje; nie implementuj niczego spoza zakresu PoC.
- Nowe decyzje zespołu dopisuj do `docs/domain.md` zgodnie z jego sekcją „Jak używać tego dokumentu z agentem”.

## Komendy

```bash
dotnet build PSuite.sln
dotnet test PSuite.sln
dotnet test PSuite.sln --filter "FullyQualifiedName~HotelCommandHandlersTests"
docker compose -f docker/docker-compose.yml up -d          # postgres:5432, keycloak:8080, redis:6379
dotnet run --project src/Bootstrapper/PSuite.Bootstrapper --launch-profile https   # https://localhost:7272
dotnet run --project src/APIGateway/PSuite.APIGateway
```

Migracje EF (projektem startowym jest zawsze Bootstrapper, bo tylko on ma `Microsoft.EntityFrameworkCore.Design`). Migracje **nie są** aplikowane przy starcie aplikacji:

```bash
dotnet ef migrations add <Nazwa> \
  --project <projekt modułu z DbContext> \
  --startup-project src/Bootstrapper/PSuite.Bootstrapper \
  --context <Moduł>DbContext --output-dir <katalog migracji modułu>
dotnet ef database update --project <projekt modułu z DbContext> \
  --startup-project src/Bootstrapper/PSuite.Bootstrapper --context <Moduł>DbContext
```

Konkretny projekt i katalog migracji podaje `CLAUDE.md` modułu.

Build generuje dużo ostrzeżeń `CS1591` (brak XML doc) — to znany szum, nie trzeba ich naprawiać przy okazji.

## Struktura

```
src/
  Bootstrapper/PSuite.Bootstrapper   # host: ładuje moduły, składa pipeline, Scalar/OpenAPI w Development
  APIGateway/PSuite.APIGateway       # BFF: YARP + OIDC (cookie), sesje w Redis, proxy na Bootstrapper
  Shared/PSuite.Abstractions         # kontrakty: ICommand/IQuery/IEvent, IModule, IModuleClient, IMessageBroker, PSuiteException
  Shared/PSuite.Infrastructure       # implementacje: dispatchery, messaging, auth, cache, Postgres, walidacja, wyjątki
  Modules/<Moduł>/
    PSuite.Modules.<Moduł>[.*]       # projekty modułu — struktura wybierana per moduł (patrz „Struktura modułu”)
    CLAUDE.md                        # opis modułu, w tym jego struktury projektów
tests/
  Shared/PSuite.Shared.Infrastructure.Tests
  Modules/<Moduł>/PSuite.Modules.<Moduł>.Tests
docker/                              # docker-compose dla postgres, keycloak, redis
docs/domain.md                       # model domeny (Event Storming, decyzje zespołu)
docs/superpowers/plans/              # plany implementacji (po polsku)
```

Moduły obecnie:

| Moduł | Struktura | Opis |
|---|---|---|
| Configuration | uproszczona: `Api` + `Core` | `src/Modules/Configuration/CLAUDE.md` |

## Architektura

### Struktura modułu
Każdy moduł **sam wybiera strukturę projektów i organizację kodu**, dopasowaną do swojej złożoności — np. uproszczoną (`Api` + `Core`, jak Configuration), vertical slices (np. planowany Reservations: katalog na feature z komendą, handlerem, walidatorem i endpointem razem) albo warstwy w stylu clean architecture. Strukturę opisuje sekcja **„Struktura”** w `CLAUDE.md` modułu i to ona jest wiążąca dla ścieżek, nazw katalogów i podziału na projekty. Nie przenoś struktury jednego modułu do innego.

Niezależnie od struktury każdy moduł spełnia wspólny kontrakt:
- wszystkie projekty modułu nazywają się `PSuite.Modules.<Moduł>` lub `PSuite.Modules.<Moduł>.<Coś>` (od tego zależy ładowanie i włączanie modułu),
- dokładnie jedna klasa `IModule` w module,
- plik `module.<moduł>.appsettings.json` w projekcie z klasą `IModule`,
- własny `DbContext` i schemat bazy, brak referencji do innych modułów,
- konwencje z sekcji poniżej (CQS, messaging, wyjątki, walidacja, autoryzacja, testy).

### Ładowanie modułów
- `ModuleLoader` (Bootstrapper) wczytuje z katalogu wyjściowego wszystkie `PSuite.Modules.*.dll` i pomija moduł, jeśli `<nazwa>:module:enabled` nie jest `true`. Nazwa to segment po `PSuite.Modules.` w nazwie dll — dlatego nazewnictwo projektów jest obowiązkowe przy każdej strukturze.
- `module.<moduł>.appsettings.json` (+ wariant środowiskowy) jest kopiowany do outputu (`CopyToOutputDirectory=Always`). `AddModulesConfiguration` dokleja go do konfiguracji.
- Bootstrapper ma `ProjectReference` do projektu z klasą `IModule` — pozostałe projekty modułu trafiają do outputu przez referencje przechodnie.
- Klasa implementująca `IModule` (`Register`, `Use`, `RegisterEndpoints`) jest tworzona przez `Activator.CreateInstance` — musi mieć publiczny konstruktor bezparametrowy.

### Rejestracja infrastruktury
`AddInfrastructure` skanuje wszystkie załadowane assembly (Scrutor, `publicOnly: false`), więc **typy `internal` są rejestrowane automatycznie**:
- `ICommandHandler<>`, `ICommandHandler<,>`, `IQueryHandler<,>`, `IEventHandler<>` — scoped,
- walidatory FluentValidation (`AbstractValidator<T>`), także internal.

Działa to dla każdej struktury modułu, bo ładowane są wszystkie dll `PSuite.Modules.*`. Nie rejestruj handlerów ręcznie. Serwisy, `DbContext` i klienci HTTP rejestruje moduł w `IModule.Register` (zwykle przez metodę rozszerzającą — gdzie leży, mówi `CLAUDE.md` modułu).

### CQS
- Komenda: `ICommand` (bez wyniku) **albo** `ICommand<TResult>` — nigdy oba. Wysyłka przez `ICommandDispatcher.SendAsync`.
- Query: `IQuery<TResult>`, wysyłka przez `IQueryDispatcher.DispatchAsync`.
- Dispatchery są singletonami i tworzą scope na każde wywołanie.

### Komunikacja między modułami
- **Moduły nie referencjonują się nawzajem** (żadnych `ProjectReference` między modułami). Każdy moduł trzyma własną kopię kontraktu; `IModuleClient` tłumaczy typy przez serializację JSON (dopasowanie po nazwie typu).
- Zapytanie synchroniczne: `IModuleClient.SendAsync(path, request)`; moduł udostępnia je przez `app.UseModuleRequests().Subscribe(...)` w `IModule.Use`.
- Eventy integracyjne: `IMessageBroker.PublishAsync`. Przy `messaging:useBackgroundDispatcher = true` (domyślnie) idą przez bounded `Channel` i `BackgroundDispatcher`; przy pełnym kanale publikujący czeka. Komendy bez wyniku też są broadcastowane.
- Eventy lokalne w module: `IEventDispatcher`.
- Event publikuj **po** udanym `SaveChangesAsync`, z własnym `CancellationToken` (nie tokenem requestu), żeby rozłączenie klienta nie zgubiło eventu. Outboxa jeszcze nie ma — utrata wiadomości przy awarii procesu jest akceptowana.

### HTTP, walidacja, błędy, autoryzacja
- Endpointy: Minimal API mapowane z `IModule.RegisterEndpoints` pod `BasePath` modułu (`MapGroup($"{basePath}/<zasób>")`). Gdzie leżą pliki endpointów — wg struktury modułu.
- Walidacja: walidator FluentValidation (`AbstractValidator<T>`) + `.WithFluentValidation<TDto>()` na endpoincie (zwraca 400 ValidationProblem).
- Błędy domenowe: dziedzicz po `PSuiteException` z `ExceptionCategory` → `GlobalExceptionHandler` mapuje na ProblemDetails: `NotFound`→404, `AlreadyExists`→409, `ValidationError`→422, `ExternalServiceIntegration`→503. `errorCode` to snake_case nazwy klasy bez `Exception`.
- Autoryzacja: Keycloak (JWT bearer + `Keycloak.AuthServices`). Endpointy chronione przez `.RequireProtectedResource("<zasób>", "<zasób>:read|write")`.

### Baza danych
- Jedna baza Postgres (`psuite`), **osobny schemat na moduł** (`modelBuilder.HasDefaultSchema("<moduł>")`), osobny `DbContext` na moduł, rejestrowany przez `services.AddPostgres<TDbContext>()`.
- Konfiguracje encji przez `IEntityTypeConfiguration<T>`, ładowane `ApplyConfigurationsFromAssembly`.
- Id generowane w aplikacji: `Guid.CreateVersion7()` w konstruktorze encji + `ValueGeneratedNever()`.

## Konwencje kodu

- File-scoped namespaces, primary constructors, `Nullable` i `ImplicitUsings` włączone.
- Typy modułu domyślnie `internal` (publiczna jest tylko klasa `IModule` i to, czego wymaga host); handlery `internal sealed`; komendy, query, eventy i DTO to `record`. Inne projekty modułu i testy widzą internale przez `InternalsVisibleTo`.
- Każda asynchroniczna metoda kontraktu przyjmuje `CancellationToken cancellationToken = default` i przekazuje go dalej.
- Encje: prywatny konstruktor bezparametrowy dla EF + publiczny konstruktor domenowy (`[SetsRequiredMembers]` przy `required`).
- Bez nowych zależności produkcyjnych bez uzgodnienia.

## Testy

- **Obecnie: tylko testy jednostkowe.** xUnit + Shouldly + NSubstitute; handlery testowane bezpośrednio, `DbContext` na `Microsoft.EntityFrameworkCore.InMemory` (świeża baza na test), zależności zewnętrzne mockowane.
- **Docelowo: testy integracyjne** (TODO na później — jeszcze nie ma infrastruktury ani decyzji co do narzędzi). Do tego czasu nie dodawaj testów integracyjnych ani nowych paczek testowych pod nie; nowe testy pisz jako jednostkowe.
- Nazwy testów: `given_<warunek>_should_<efekt>`.
- Testy modułu w `tests/Modules/<Moduł>/PSuite.Modules.<Moduł>.Tests`; referencjonują wyłącznie projekty własnego modułu (które — wg `CLAUDE.md` modułu).

## Pułapki

- Pliki `appsettings.Development.json` zawierają sekrety dev (Keycloak, Postgres) — to celowe dla środowiska lokalnego; nie przenoś ich do `appsettings.json` i nie dodawaj sekretów produkcyjnych.
- `AddModulesConfiguration` szuka `module.*.appsettings.{Environment}.json`. Na Linuksie (Docker) wielkość liter ma znaczenie, a pliki modułów mają małe `development` — wariant środowiskowy nie zostanie wtedy wczytany.
- `GetOptions<T>()` i rejestracja modułów (np. `AddCore()` w Configuration) budują tymczasowy `ServiceProvider`, żeby odczytać konfigurację — konfiguracja musi być kompletna przed `Register` modułów.

## Dodawanie nowego modułu

1. **Wybierz strukturę** modułu (uproszczona, vertical slices, warstwowa…) — jeśli nie jest oczywista, zapytaj użytkownika.
2. Utwórz projekty `src/Modules/<Moduł>/PSuite.Modules.<Moduł>[.*]` wg wybranej struktury i dodaj je do `PSuite.sln`.
3. W projekcie hosta modułu: klasa `<Moduł>Module : IModule`, `module.<moduł>.appsettings.json` z `"<moduł>": { "module": { "name": "...", "enabled": true } }`, `CopyToOutputDirectory=Always`.
4. Rejestracja zależności modułu, `<Moduł>DbContext` z własnym schematem, `InternalsVisibleTo` dla pozostałych projektów modułu i testów.
5. Dodaj `ProjectReference` z Bootstrappera do projektu z klasą `IModule`.
6. Utwórz `tests/Modules/<Moduł>/PSuite.Modules.<Moduł>.Tests`.
7. **Utwórz `src/Modules/<Moduł>/CLAUDE.md`** wg układu z Configuration — z sekcją „Struktura” opisującą wybrany układ projektów, gdzie trafiają nowe rzeczy oraz projekt i katalog migracji — i dopisz moduł do tabeli w sekcji „Struktura” powyżej.

Po zmianach, które unieważniają opis modułu (nowe endpointy, encje, eventy, integracje), zaktualizuj jego `CLAUDE.md` w tym samym commicie.

## Git i workflow

- Commity w stylu `feat: …`, `quickfix: …`; zmiany przez PR do `main` z gałęzi tematycznej.
- Większe zmiany: plan w `docs/superpowers/plans/RRRR-MM-DD-<temat>.md`.
