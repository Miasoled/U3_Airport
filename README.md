# U3 Examen Airport

Aplicación web para administrar información de un aeropuerto. El proyecto utiliza **ASP.NET Core MVC**, **Entity Framework Core** y **PostgreSQL**.

## Estado actual

Hasta el momento se ha implementado:

- La estructura base de una aplicación ASP.NET Core MVC.
- La conexión a PostgreSQL mediante Entity Framework Core y el proveedor Npgsql.
- El contexto `AirportContext`, generado a partir de una base de datos existente.
- El mapeo de las entidades, propiedades, claves, índices y relaciones del dominio aeroportuario.
- Un CRUD completo de aerolíneas:
  - Listar aerolíneas.
  - Consultar detalles.
  - Crear registros.
  - Editar registros.
  - Eliminar registros con una vista de confirmación.
- Validación antifalsificación (`ValidateAntiForgeryToken`) en las operaciones que modifican datos.
- Manejo básico de registros inexistentes y conflictos de concurrencia.
- Vistas Razor con Bootstrap, jQuery y validación del lado del cliente.

## Modelo de datos

El contexto incluye las siguientes entidades:

| Entidad | Información representada |
| --- | --- |
| `Airline` | Aerolíneas |
| `Airplane` | Aviones |
| `AirplaneType` | Tipos de avión |
| `Airport` | Aeropuertos |
| `AirportGeo` | Ubicación geográfica de aeropuertos |
| `AirportReachable` | Alcance o conexiones entre aeropuertos |
| `Booking` | Reservas |
| `Employee` | Empleados |
| `Flight` | Vuelos |
| `FlightLog` | Historial de cambios de vuelos |
| `Flightschedule` | Horarios recurrentes de vuelos |
| `Passenger` | Pasajeros |
| `Passengerdetail` | Información adicional de pasajeros |
| `Weatherdatum` | Registros meteorológicos |

> Aunque todas estas entidades están mapeadas, actualmente solo `Airline` dispone de controlador y vistas CRUD.

## Tecnologías utilizadas

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core 10
- Npgsql para Entity Framework Core
- PostgreSQL
- Razor Views
- Bootstrap
- jQuery y jQuery Validation

El proyecto también contiene referencias a los paquetes de Entity Framework Core para SQL Server y ASP.NET Core Identity, aunque la configuración activa usa PostgreSQL y todavía no se ha implementado autenticación.

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- PostgreSQL
- Una base de datos con el esquema aeroportuario que corresponde a las entidades de `Models/` y al mapeo de `AirportContext`

## Configuración

1. Clona el repositorio y entra en la carpeta del proyecto.

2. Restaura las dependencias:

   ```bash
   dotnet restore
   ```

3. Configura la cadena `AirportConnection`. Para desarrollo se recomienda no guardar credenciales reales en el repositorio y usar Secret Manager:

   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:AirportConnection" "Host=localhost;Port=5432;Database=airport;Username=TU_USUARIO;Password=TU_CONTRASENA"
   ```

   La aplicación espera una cadena con este formato:

   ```text
   Host=localhost;Port=5432;Database=airport;Username=TU_USUARIO;Password=TU_CONTRASENA
   ```

> Este repositorio no contiene migraciones ni un script de creación de la base de datos. El esquema debe existir antes de ejecutar la aplicación.

## Ejecución

Inicia el proyecto con:

```bash
dotnet run
```

Con la configuración de desarrollo actual, la aplicación estará disponible en:

- `http://localhost:5240`
- `https://localhost:7055`

El módulo implementado de aerolíneas se encuentra en:

```text
/Airlines
```

## Comandos utilizados para construir el proyecto

Los siguientes comandos resumen cómo reproducir desde cero lo realizado hasta ahora. Deben ejecutarse desde PowerShell o una terminal compatible.

### 1. Crear el proyecto MVC

```bash
dotnet new mvc -n U3_Examen_Airport -f net10.0
cd U3_Examen_Airport
```

### 2. Instalar los paquetes

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.1
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.2
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 10.0.2
dotnet add package Microsoft.VisualStudio.Web.CodeGeneration.Design --version 10.0.2
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.2
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.2
dotnet add package Microsoft.AspNetCore.Identity.UI --version 10.0.2
```

PostgreSQL es el proveedor utilizado actualmente. Los paquetes de SQL Server e Identity están referenciados en el proyecto, pero todavía no forman parte de la funcionalidad activa.

### 3. Instalar las herramientas de scaffolding

```bash
dotnet tool install --global dotnet-ef
dotnet tool install --global dotnet-aspnet-codegenerator
```

Si ya estaban instaladas, pueden actualizarse con:

```bash
dotnet tool update --global dotnet-ef
dotnet tool update --global dotnet-aspnet-codegenerator
```

### 4. Generar el contexto y los modelos desde PostgreSQL

El proyecto sigue un enfoque **Database First**. Con la base de datos aeroportuaria ya creada, el contexto y las entidades se pueden generar mediante:

```bash
dotnet ef dbcontext scaffold "Host=localhost;Port=5432;Database=airport;Username=TU_USUARIO;Password=TU_CONTRASENA" Npgsql.EntityFrameworkCore.PostgreSQL --context AirportContext --context-dir Data --output-dir Models --no-onconfiguring
```

Si se necesita volver a generar archivos existentes después de cambiar el esquema, se puede añadir `--force`. Antes de hacerlo se deben respaldar las personalizaciones manuales:

```bash
dotnet ef dbcontext scaffold "Host=localhost;Port=5432;Database=airport;Username=TU_USUARIO;Password=TU_CONTRASENA" Npgsql.EntityFrameworkCore.PostgreSQL --context AirportContext --context-dir Data --output-dir Models --no-onconfiguring --force
```

### 5. Registrar PostgreSQL en la aplicación

La configuración aplicada en `Program.cs` equivale a registrar el contexto así:

```csharp
var connectionString =
    builder.Configuration.GetConnectionString("AirportConnection");

builder.Services.AddDbContext<AirportContext>(options =>
    options.UseNpgsql(connectionString));
```

La cadena de conexión puede guardarse para desarrollo con:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AirportConnection" "Host=localhost;Port=5432;Database=airport;Username=TU_USUARIO;Password=TU_CONTRASENA"
```

### 6. Generar el CRUD de aerolíneas

El controlador y las vistas Razor de `Airline` se pueden generar con el scaffolder de ASP.NET Core:

```bash
dotnet aspnet-codegenerator controller -name AirlinesController -m Airline -dc AirportContext --relativeFolderPath Controllers --useDefaultLayout --referenceScriptLibraries
```

Este comando genera `Controllers/AirlinesController.cs` y las vistas dentro de `Views/Airlines/`.

### 7. Restaurar, compilar y ejecutar

```bash
dotnet restore
dotnet build
dotnet run
```

Para ejecutar con recarga automática durante el desarrollo:

```bash
dotnet watch run
```

### 8. Comandos de comprobación útiles

```bash
dotnet --version
dotnet ef --version
dotnet list package
dotnet build --no-restore
```

## Estructura del proyecto

```text
U3_Examen_Airport/
├── Controllers/
│   ├── AirlinesController.cs
│   └── HomeController.cs
├── Data/
│   └── AirportContext.cs
├── Models/
│   └── Entidades del dominio aeroportuario
├── Views/
│   ├── Airlines/
│   ├── Home/
│   └── Shared/
├── wwwroot/
│   └── Archivos CSS, JavaScript y librerías del cliente
├── Program.cs
├── appsettings.json
└── U3_Examen_Airport.csproj
```

## Flujo del CRUD de aerolíneas

El controlador `AirlinesController` utiliza `AirportContext` para acceder a PostgreSQL de forma asíncrona. Sus rutas principales son:

| Método | Ruta | Acción |
| --- | --- | --- |
| GET | `/Airlines` | Lista las aerolíneas |
| GET | `/Airlines/Details/{id}` | Muestra una aerolínea |
| GET/POST | `/Airlines/Create` | Crea una aerolínea |
| GET/POST | `/Airlines/Edit/{id}` | Edita una aerolínea |
| GET/POST | `/Airlines/Delete/{id}` | Confirma y elimina una aerolínea |

## Próximos pasos sugeridos

- Agregar al menú de navegación un acceso al módulo de aerolíneas.
- Implementar controladores y vistas para las demás entidades.
- Incorporar validaciones de negocio y mensajes de error amigables.
- Mover todas las credenciales a variables de entorno o Secret Manager.
- Añadir autenticación y autorización si el sistema tendrá usuarios.
- Incluir pruebas automatizadas.
- Agregar migraciones o instrucciones para construir y cargar la base de datos.

## Nota sobre los datos

El mapeo de las tablas contiene referencias a **Flughafen DB**, obra de Stefan Pröll, Eva Zangerle y Wolfgang Gassler, publicada bajo licencia **CC BY 4.0**.
