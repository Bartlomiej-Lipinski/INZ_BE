# Mates

Opis projektu
-------------
Mates to aplikacja webowa napisana w .NET (wersja net9.0), z warstwą API oraz warstwą dostępu do danych zrealizowaną przy pomocy Entity Framework Core. Projekt zawiera implementację funkcjonalności związanych z użytkownikami, wydarzeniami, wyzwaniami, komentarzami i innymi domenowymi obiektami.

Celem repozytorium jest dostarczenie pełnej aplikacji serwerowej wraz z testami uruchamianymi w katalogu `Mates.Tests`.

Technologie
-----------
- Język i runtime: .NET 9 (net9.0)
- Web API: ASP.NET Core (minimal hosting model, `Program.cs`)
- ORM: Entity Framework Core (migracje znajdują się w katalogu `Migrations`)
- Testy: xUnit (projekt testowy `Mates.Tests` zawiera testy integracyjne/E2E oraz scenariusze funkcjonalne)
- Opcjonalnie: SQLite (zalecany dla testów integracyjnych w trybie in-memory), Docker dla izolowanych środowisk DB

Główne założenia architektury
-----------------------------
Projekt jest zorganizowany w warstwy (konwencja katalogów):
- `Features/` - domenowe funkcjonalności podzielone według koncepcji (Auth, Challenges, Comments, Events, Groups, ...). Zawiera implementacje endpointów, handlerów, modeli DTO itp.
- `Infrastructure/` - konfiguracja infrastruktury: dostęp do danych, konfiguracje EF Core, serwisy zewnętrzne.
- `Shared/` - wspólne rozszerzenia, odpowiedzi, walidatory i pomocnicze klasy wykorzystywane w wielu miejscach.
- `Migrations/` - migracje EF Core generowane dla kontekstu aplikacji.
- `uploads/` oraz `wwwroot/uploads/` - katalogi przechowywania plików użytkownika/assetów.

Zasady projektowe i konwencje
----------------------------
- Testy integracyjne/E2E uruchamiają całą aplikację (WebApplicationFactory / TestServer) i wykonują zapytania HTTP lub operacje na bazie.
- Testy jednostkowe (jeśli dodawane) powinny izolować zależności (np. przez Moq) i nie polegać na rzeczywistej bazie danych.
- Preferowane są deterministyczne, odtwarzalne dane testowe (użycie `TestDataFactory` w projekcie testowym).

Uruchomienie lokalne — wymagania
--------------------------------
- .NET 9 SDK zainstalowany lokalnie (dotnet --version powinno wskazać wersję kompatybilną z net9.0).
- (Opcjonalnie) dotnet-ef jeśli chcesz wykonywać migracje z linii poleceń: `dotnet tool install --global dotnet-ef`.
- (Opcjonalnie) SQLite / Docker, jeżeli chcesz użyć kontenera DB do testów integracyjnych.

Szybkie komendy (Windows, cmd.exe)
----------------------------------
Aby zbudować rozwiązanie i uruchomić aplikację lokalnie:

```cmd
cd {ścieżka usera}\RiderProjects\INZ_BE
dotnet build
dotnet run --project Mates\Mates.csproj
```

Jeśli chcesz ustawić środowisko (np. Development) przed uruchomieniem w `cmd.exe`:

```cmd
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project Mates\Mates.csproj
```

Uruchamianie testów
-------------------
Aby uruchomić testy dla całego rozwiązania (w tym `Mates.Tests`):

```cmd
cd {ścieżka usera}\RiderProjects\INZ_BE
dotnet test
```

Możesz uruchomić testy tylko w projekcie testowym:

```cmd
dotnet test Mates.Tests\Mates.Tests.csproj
```

Migracje i baza danych
----------------------
Aby zastosować migracje do lokalnej bazy (np. podczas developmentu), użyj `dotnet ef` lub innego narzędzia migracji. Przykład (wymaga zainstalowanego `dotnet-ef`):

```cmd
cd {ścieżka usera}\RiderProjects\INZ_BE\Mates
set ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update --project Mates.csproj
```

(Uwaga) W zależności od konfiguracji połączenia w `appsettings.json` może być potrzebne dostosowanie connection string - sprawdź plik `appsettings.Development.json` lub zmienne środowiskowe.

Konfiguracja i pliki konfiguracyjne
-----------------------------------
- `appsettings.json` i `appsettings.Development.json` - ustawienia aplikacji i połączenia do DB.
- `Program.cs` - punkt wejścia aplikacji i konfiguracja middleware.

Debugowanie w IDE (Rider / Visual Studio)
----------------------------------------
- Otwórz rozwiązanie `Mates.sln` w IDE.
- Upewnij się, że projekt startowy to `Mates` i uruchom debug (F5) lub uruchom bez debugowania (Ctrl+F5).
- Aby debugować testy, w Riderze/VS wybierz test i uruchom w trybie debugowania.

Testowanie — krótki przewodnik
------------------------------
- Katalog `Mates.Tests` zawiera głównie testy integracyjne i funkcjonalne (WebApplicationFactory/TestServer). Są to priorytetowe testy, ponieważ weryfikują mapowanie EF Core, ograniczenia DB i zachowanie API.
- Dodając nowe testy:
  - Preferuj odizolowane środowisko DB (SQLite in-memory lub kontener DB w CI).
  - Używaj `TestDataFactory` do tworzenia danych testowych.
  - Resetuj stan DB między testami, aby uniknąć zależności między testami.

CI / integracja (zalecenia)
---------------------------
- Uruchamiaj `dotnet test` w pipeline dla każdej gałęzi/PR.
- Dla testów integracyjnych przygotuj krok tworzący bazę testową (kontener Docker lub lokalna instancja) i ustaw connection string przez zmienne środowiskowe.

Dalsze informacje i rozwój
--------------------------
- Dodawaj testy jednostkowe dla logiki biznesowej oraz testy wydajnościowe (BenchmarkDotNet / k6) tam, gdzie występują krytyczne ścieżki.
- Dokumentuj nowe konwencje w tym README (np. format nazw testów, zasady migracji DB w CI).

Kontakt / Wsparcie
------------------
W razie pytań dotyczących uruchomienia projektu lub testów, najlepiej skontaktować się z autorem projektu/maintainerem lub otworzyć issue w repozytorium z opisem problemu.

---
Plik README zaktualizowany lokalnie - zawiera instrukcje uruchomienia, testów i migracji. Jeśli chcesz, mogę także dodać przykładowy skrypt batch lub plik powershell do wygodnego uruchamiania środowiska developerowego.
