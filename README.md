# AirportSystem

[English](#english) | [Español](#español)

Airport management system / Sistema de gestión aeroportuaria.

---

## English

Airport management system with **two applications that share the same information**:

- a **console application** (`ConsoleApp1`), and
- an **Avalonia desktop UI** (`AirportSystem.UI`).

Both use the same models, services and data file, so changes made in one app show up in the other.

### Access credentials

| User     | Password   |
|----------|------------|
| `Admin`  | `Password` |

- Case-sensitive.
- The console allows **3 attempts** and masks the password with `*`.
- The desktop UI shows a login window before opening the main system.

> This is a demo login: credentials are hard-coded in `AuthService`. It is not suitable for production.

### Requirements

- **.NET 10 SDK**
- Windows, Linux or macOS with desktop support (the app is cross-platform).
- PostgreSQL is **not** required to run the app: persistence is a JSON file. EF Core/Npgsql packages are referenced for a future database migration.

### Build and run

From the repository root:

```powershell
dotnet build AirportSystem.slnx
```

Run the console:

```powershell
dotnet run --project AirportSystem/ConsoleApp1.csproj
```

Run the desktop UI:

```powershell
dotnet run --project AirportSystem.UI/AirportSystem.UI.csproj
```

You can also run the compiled binaries directly:

```powershell
AirportSystem\bin\Debug\net10.0\ConsoleApp1.exe
AirportSystem.UI\bin\Debug\net10.0\AirportSystem.UI.exe
```

#### Linux and macOS

`dotnet run` works the same on Linux and macOS. To run a published build, use the framework-dependent `.dll` with `dotnet` (the native launcher is also generated for the target platform):

```bash
dotnet build AirportSystem.slnx
dotnet run --project AirportSystem/ConsoleApp1.csproj
dotnet run --project AirportSystem.UI/AirportSystem.UI.csproj

# After publishing for your runtime identifier (for example linux-x64 or osx-arm64):
dotnet ConsoleApp1.dll
dotnet AirportSystem.UI.dll
```

On Linux the desktop UI requires a graphical session with X11 or Wayland. In WSL, use WSLg with `DISPLAY` configured. On Debian/Ubuntu-based systems, install `libice6` and `libsm6` if Avalonia reports a missing `libICE.so.6` / `libSM.so.6`. The included `app.manifest` is applied only on Windows.

### Projects

| Project | Type | Description |
|---------|------|-------------|
| `AirportSystem/ConsoleApp1.csproj` | `Exe` | Console application. Contains models, services and persistence. |
| `AirportSystem.UI/AirportSystem.UI.csproj` | `WinExe` | Avalonia UI. References the console project to reuse the logic. |
| `AirportSystem.slnx` | Solution | Groups both projects. |

Main technologies: **.NET 10**, **Avalonia 12.1.6**, **CommunityToolkit.Mvvm**, **System.Text.Json**, **EF Core + Npgsql** (prepared, not currently used).

### Persistence and synchronization

Data is saved in one shared path:

```text
Windows: %LOCALAPPDATA%\AirportSystem\airport_data.json
Linux:   $XDG_DATA_HOME/AirportSystem/airport_data.json (or ~/.local/share/AirportSystem/airport_data.json)
macOS:   ~/Library/Application Support/AirportSystem/airport_data.json
```

The path is displayed in the console menu.

Features:

- **Atomic writes** using a temporary `.tmp` file followed by replacement.
- Automatic retries when another process is reading/writing.
- The console reloads the file when it detects changes made by the UI.
- The UI reloads when the window is activated and before every write operation.
- If no file exists, sample data is seeded.
- If the file is corrupted, it is kept and a warning is shown; it is not overwritten automatically.

For testing, the path can be redirected with the `AIRPORT_DATA_PATH` environment variable:

```powershell
$env:AIRPORT_DATA_PATH = "C:\Temp\airport_data.json"
```

```bash
export AIRPORT_DATA_PATH=/tmp/airport_data.json
```

### Features

#### Flights

- Register a flight with number, airline, origin, destination, times, base price, gate, terminal and seat layout.
- Search by origin/destination.
- Edit and delete flights.
- View the seat map (`X` = booked, `.` = available).
- Simulate time and update statuses: `Scheduled`, `Boarding`, `Departed`, `Arrived`, `Cancelled`.

#### Passengers

- Register, edit and delete passengers.
- List passengers with their booking count.

#### Bookings and check-in

- Create a booking by assigning flight, passenger and seat.
- Edit a booking to change flight, passenger or seat.
- Cancel or delete a booking (frees the seat).
- Perform check-in.
- The console prints a **boarding pass** after check-in.

#### Integrity rules

- Deleting a flight cancels its bookings.
- Deleting a passenger cancels their bookings and frees their seats.
- Deleting a booking frees the seat.
- Editing a flight with bookings preserves the seat map and updates the flight number on those bookings.
- Editing a booking moves the seat, frees the previous one and clears check-in if anything changed.
- A flight cannot have an arrival earlier than its departure.
- Bookings cannot be created/edited on cancelled or already arrived flights, nor on occupied seats.

### Console menu

```text
1. List all flights / register new flight
2. Search flights
3. List all passengers / register new passenger
4. Book a ticket
5. Cancel booking
6. View my bookings
7. Check-in
8. Advance time (simulate)
9. Show seat map for a flight
10. Edit flight
11. Edit passenger
12. Edit booking
13. Delete flight (cancels its bookings)
14. Delete passenger (frees seats)
15. Delete booking (frees seat)
0. Save & Exit
```

### Desktop UI

The main window has three tabs:

- **Vuelos / Flights**: list, search, seat map, create/edit form and edit/delete buttons.
- **Pasajeros / Passengers**: list and create/edit form with edit/delete buttons.
- **Reservas y check-in / Bookings and check-in**: flight/passenger/seat selection, booking, editing, check-in and deletion.

A confirmation dialog appears before any deletion, explaining which records will be affected.

### Code structure

```text
AirportSystem/
├── Models/
│   ├── Flight.cs          # Flight, seat map, available seats
│   ├── Passenger.cs
│   ├── Booking.cs
│   └── Enums/FlightStatus.cs
├── Services/
│   ├── AirportService.cs  # CRUD, searches, bookings, check-in, business rules
│   └── AuthService.cs     # Shared login
├── Utilities/
│   ├── DataStorage.cs     # Shared JSON and synchronization
│   └── AppDbContext.cs    # EF Core/Npgsql (prepared, not currently used)
└── Program.cs             # Console menu and login

AirportSystem.UI/
├── App.axaml.cs           # Startup: login -> MainWindow
├── ViewModels/
│   └── MainViewModel.cs   # UI commands and state
└── Views/
    ├── LoginWindow.axaml(.cs)
    ├── MainWindow.axaml(.cs)
    └── ConfirmWindow.axaml(.cs)
```

### Known limitations and suggested improvements

1. **Security**: plain-text password and fixed credentials. Use salted hashes, roles and temporary lockout.
2. **Sensitive configuration**: `AppDbContext.cs` contains a PostgreSQL password in code; move it to configuration or an environment variable.
3. **Persistence**: JSON is simple but has no transactions. EF Core/Npgsql is already available to migrate to PostgreSQL or SQLite.
4. **Unit tests**: there is no test project; adding xUnit for `AirportService`, `AuthService` and `DataStorage` is recommended.
5. **Reporting/UX**: filters by date/status, export boarding pass to PDF and passenger lists per flight.

### Windows execution notes

If Avalonia/Skia does not open due to **Smart App Control** restrictions, you may need to allow the executable or temporarily disable that protection on the machine running the app.

---

## Español

Sistema de gestión aeroportuaria con **dos aplicaciones que comparten la misma información**:

- una **aplicación de consola** (`ConsoleApp1`), y
- una **interfaz gráfica Avalonia** (`AirportSystem.UI`).

Ambas usan los mismos modelos, servicios y archivo de datos, así que los cambios hechos en una aparecen en la otra.

### Credenciales de acceso

| Usuario | Contraseña |
|---------|------------|
| `Admin` | `Password` |

- Distingue mayúsculas y minúsculas.
- La consola permite **3 intentos** y muestra la contraseña enmascarada con `*`.
- La interfaz gráfica muestra una ventana de login antes de abrir el sistema.

> Es un login de demostración: las credenciales están fijas en `AuthService`. No es seguro para producción.

### Requisitos

- **.NET 10 SDK**
- Windows, Linux o macOS con soporte de escritorio (la aplicación es multiplataforma).
- No se necesita PostgreSQL para usar la aplicación: la persistencia actual es un archivo JSON. Los paquetes EF Core/Npgsql están referenciados para una futura migración a base de datos.

### Compilación y ejecución

Desde la carpeta raíz del repositorio:

```powershell
dotnet build AirportSystem.slnx
```

Ejecutar la consola:

```powershell
dotnet run --project AirportSystem/ConsoleApp1.csproj
```

Ejecutar la interfaz gráfica:

```powershell
dotnet run --project AirportSystem.UI/AirportSystem.UI.csproj
```

También puedes ejecutar los binarios ya compilados:

```powershell
AirportSystem\bin\Debug\net10.0\ConsoleApp1.exe
AirportSystem.UI\bin\Debug\net10.0\AirportSystem.UI.exe
```

#### Linux y macOS

`dotnet run` funciona igual en Linux y macOS. Para ejecutar una publicación, usa el `.dll` con `dotnet` (el lanzador nativo también se genera para la plataforma de destino):

```bash
dotnet build AirportSystem.slnx
dotnet run --project AirportSystem/ConsoleApp1.csproj
dotnet run --project AirportSystem.UI/AirportSystem.UI.csproj

# Tras publicar para tu identificador de entorno (por ejemplo linux-x64 u osx-arm64):
dotnet ConsoleApp1.dll
dotnet AirportSystem.UI.dll
```

En Linux la interfaz gráfica necesita una sesión gráfica con X11 o Wayland. En WSL, usa WSLg con `DISPLAY` configurado. En sistemas basados en Debian/Ubuntu instala `libice6` y `libsm6` si Avalonia avisa de que falta `libICE.so.6` / `libSM.so.6`. El `app.manifest` incluido solo se aplica en Windows.

### Proyectos

| Proyecto | Tipo | Descripción |
|----------|------|-------------|
| `AirportSystem/ConsoleApp1.csproj` | `Exe` | Aplicación de consola. Contiene modelos, servicios y persistencia. |
| `AirportSystem.UI/AirportSystem.UI.csproj` | `WinExe` | Interfaz Avalonia. Referencia al proyecto de consola para reutilizar la lógica. |
| `AirportSystem.slnx` | Solución | Agrupa ambos proyectos. |

Tecnologías principales: **.NET 10**, **Avalonia 12.1.6**, **CommunityToolkit.Mvvm**, **System.Text.Json**, **EF Core + Npgsql** (preparado, sin uso activo).

### Persistencia y sincronización

Los datos se guardan en una ruta única compartida:

```text
Windows: %LOCALAPPDATA%\AirportSystem\airport_data.json
Linux:   $XDG_DATA_HOME/AirportSystem/airport_data.json (o ~/.local/share/AirportSystem/airport_data.json)
macOS:   ~/Library/Application Support/AirportSystem/airport_data.json
```

La ruta se muestra en el menú de la consola.

Características:

- Escritura **atómica** mediante un archivo temporal `.tmp` y reemplazo posterior.
- Reintentos automáticos si otro proceso está leyendo/escribiendo.
- La consola recarga el archivo si detecta cambios hechos desde la UI.
- La UI recarga al activarse la ventana y antes de cada operación que escribe.
- Si no existe archivo, se cargan datos de ejemplo.
- Si el archivo está corrupto, se conserva y se avisa; no se sobrescribe automáticamente.

Para pruebas se puede redirigir la ruta con la variable de entorno `AIRPORT_DATA_PATH`:

```powershell
$env:AIRPORT_DATA_PATH = "C:\Temp\airport_data.json"
```

```bash
export AIRPORT_DATA_PATH=/tmp/airport_data.json
```

### Funcionalidad

#### Vuelos

- Registrar vuelo con número, aerolínea, origen, destino, fechas, precio, puerta, terminal y distribución de asientos.
- Buscar por origen/destino.
- Editar y eliminar vuelos.
- Ver mapa de asientos (`X` = ocupado, `.` = libre).
- Simular el avance del tiempo y actualizar estados: `Scheduled`, `Boarding`, `Departed`, `Arrived`, `Cancelled`.

#### Pasajeros

- Registrar, editar y eliminar pasajeros.
- Listar pasajeros con su número de reservas.

#### Reservas y check-in

- Crear reserva asignando vuelo, pasajero y asiento.
- Editar reserva para cambiar vuelo, pasajero o asiento.
- Cancelar o eliminar reserva (libera el asiento).
- Hacer check-in.
- La consola muestra una **tarjeta de embarque** tras el check-in.

#### Reglas de integridad

- Eliminar un vuelo cancela sus reservas.
- Eliminar un pasajero cancela sus reservas y libera sus asientos.
- Eliminar una reserva libera el asiento.
- Editar un vuelo con reservas conserva el mapa de asientos y actualiza el número de vuelo en esas reservas.
- Editar una reserva mueve el asiento, libera el anterior y quita el check-in si cambia algo.
- No se permiten vuelos con llegada anterior a la salida.
- No se pueden crear/editar reservas en vuelos cancelados o ya llegados, ni en asientos ocupados.

### Menú de la consola

```text
1. List all flights / register new flight
2. Search flights
3. List all passengers / register new passenger
4. Book a ticket
5. Cancel booking
6. View my bookings
7. Check-in
8. Advance time (simulate)
9. Show seat map for a flight
10. Edit flight
11. Edit passenger
12. Edit booking
13. Delete flight (cancels its bookings)
14. Delete passenger (frees seats)
15. Delete booking (frees seat)
0. Save & Exit
```

### Interfaz gráfica

La ventana principal tiene tres pestañas:

- **Vuelos**: listado, búsqueda, mapa de asientos, formulario de alta/edición y botones de editar/eliminar.
- **Pasajeros**: listado y formulario de alta/edición con botones de editar/eliminar.
- **Reservas y check-in**: selección de vuelo/pasajero/asiento, reserva, edición, check-in y eliminación.

Antes de cualquier borrado aparece una ventana de confirmación que indica qué registros se verán afectados.

### Estructura del código

```text
AirportSystem/
├── Models/
│   ├── Flight.cs          # Vuelo, mapa de asientos, plazas libres
│   ├── Passenger.cs
│   ├── Booking.cs
│   └── Enums/FlightStatus.cs
├── Services/
│   ├── AirportService.cs  # CRUD, búsquedas, reservas, check-in, reglas de negocio
│   └── AuthService.cs     # Login compartido
├── Utilities/
│   ├── DataStorage.cs     # JSON compartido y sincronización
│   └── AppDbContext.cs    # EF Core/Npgsql (preparado, sin uso activo)
└── Program.cs             # Menú de consola y login

AirportSystem.UI/
├── App.axaml.cs           # Arranque: login -> MainWindow
├── ViewModels/
│   └── MainViewModel.cs   # Comandos y estado de la UI
└── Views/
    ├── LoginWindow.axaml(.cs)
    ├── MainWindow.axaml(.cs)
    └── ConfirmWindow.axaml(.cs)
```

### Limitaciones conocidas y mejoras sugeridas

1. **Seguridad**: contraseña en texto plano y credenciales fijas. Usar hash con salt, roles y bloqueo temporal.
2. **Configuración sensible**: `AppDbContext.cs` contiene una contraseña de PostgreSQL escrita en el código; debería moverse a configuración o variable de entorno.
3. **Persistencia**: el JSON es simple pero no tiene transacciones. El proyecto ya tiene EF Core/Npgsql para migrar a PostgreSQL o SQLite.
4. **Pruebas unitarias**: no hay proyecto de tests; conviene añadir xUnit para `AirportService`, `AuthService` y `DataStorage`.
5. **Reporting/UX**: filtros por fecha/estado, exportar tarjeta de embarque a PDF y listados de pasajeros por vuelo.

### Notas de ejecución en Windows

Si Avalonia/Skia no abre por las restricciones de **Smart App Control**, puede ser necesario permitir el ejecutable o desactivar temporalmente esa protección en el equipo donde se ejecuta la aplicación.
