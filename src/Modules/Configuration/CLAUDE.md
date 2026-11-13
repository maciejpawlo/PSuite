# Moduł Configuration

Konfiguracja struktury hotelu: hotele, pokoje i pracownicy (z kontami w Keycloak). Nie obejmuje rezerwacji ani dostępności pokoi.

## Struktura

**Uproszczona: `Api` + `Core`** — moduł to głównie CRUD, nie dokładaj warstw ani slice'ów.

- `Api`: tylko `ConfigurationModule`, endpointy (`Endpoints/<Obszar>Api.cs`) i `module.configuration.appsettings*.json`. Bez logiki.
- `Core`: cała reszta, podział katalogów techniczny (`Commands/` + `Handlers/`, `Queries/` + `Handlers/`, `Events/`, `DTO/` z walidatorem w tym samym pliku, `Exceptions/`, `Entities/`, `DAL/`). Rejestracja DI w `AddCore()` (`Core/Extensions.cs`).
- Testy referencjonują tylko `Core`.
- Migracje: projekt `PSuite.Modules.Configuration.Core`, katalog `DAL/Migrations`, kontekst `ConfigurationDbContext`.

## Kierunek

- Hotel jest na CQS; Room i Employee to stan przejściowy na serwisach (`IRoomService`, `IEmployeeService`).
- Nowa funkcjonalność → CQS. Nie rozbudowuj serwisów; przy większej zmianie w Room/Employee rozważ migrację obszaru wzorem Hotel.
- Eventy publikuj jak `HotelCreated` w `CreateHotelHandler`: po `SaveChangesAsync`, z własnym tokenem z timeoutem.

## Keycloak

- Pracownik = użytkownik w Keycloak; `Employee.UserId` to jego id. Tworzenie, edycja i usuwanie idą przez Admin REST API (`Keycloak/`).
- Token admina (`client_credentials`) jest cache'owany w Redisie — bez Redisa operacje na pracownikach nie działają.
- Wymagania realmu `PSuite`: service account klienta może zarządzać użytkownikami i rolami, istnieje rola `employee`, włączona edycja username.

## Testy

Tylko jednostkowe (integracyjne — TODO). Helper `TestDb.Create()` jest w `Hotels/HotelCommandHandlersTests.cs`; `IKeycloakService` mockuj przez NSubstitute.

## Znane problemy (nie naprawiaj przy okazji bez uzgodnienia)

- `PUT /{id}` ignoruje `id` z trasy i używa `Id` z body.
- Operacje na pracownikach nie są atomowe między Keycloak a bazą (możliwe osierocone konta).
- `RoomService.GetByIdAsync` nie robi `Include(x => x.Hotel)`, a `ToDto()` czyta `room.Hotel.Id` — możliwy `NullReferenceException`.
