# U3 Examen Airport — Tipo 4

Aplicación web desarrollada con ASP.NET Core MVC y .NET 10 para consultar vuelos, crear reservas y gestionar la reprogramación de vuelos. Utiliza PostgreSQL, Entity Framework Core, ASP.NET Core Identity y PayPal Sandbox mediante Orders API v2.

## Funcionalidades principales

### Reservas

- Búsqueda de vuelos por origen, destino y fecha.
- Creación de reservas asociadas al usuario autenticado.
- Asociación del usuario con el pasajero mediante su correo electrónico.
- Asignación y comprobación de disponibilidad de asientos.
- Cálculo del precio estimado en el servidor.
- Prevención de reservas duplicadas para un mismo pasajero y vuelo.

### Reprogramación de vuelos

- Consulta de las reservas pertenecientes al usuario.
- Búsqueda manual mediante número de reserva y pasaporte.
- Filtros por número de vuelo y rango de fechas.
- Ordenamiento por fecha de salida, precio o reserva más reciente.
- Paginación física con `CountAsync()`, `Skip()` y `Take()`.
- Búsqueda de vuelos alternativos con el mismo origen y destino.
- Selección y validación de un nuevo asiento.
- Comparación entre el vuelo actual y el vuelo alternativo.
- Cálculo en el servidor del precio original, nuevo precio, diferencia tarifaria, penalización y total.
- Creación transaccional de la solicitud, la orden y su detalle.

### Pagos

- Integración con PayPal Sandbox.
- Creación y captura de órdenes mediante PayPal Orders API v2.
- Registro de pagos e historiales de transacción.
- Actualización de la reserva únicamente después de un pago aprobado.
- Generación de comprobantes.
- Manejo de los estados `Pendiente`, `Aprobado`, `Cancelado`, `Rechazado` y `Fallido`.

Los estados se almacenan actualmente como valores `string`; el proyecto no declara una enumeración C# para representarlos.

### Administración

- Panel administrativo con filtros, métricas y operaciones recientes.
- Agrupación y conteo de órdenes por estado.
- Total y promedio de pagos aprobados.
- CRUD de aerolíneas, aeropuertos, reservas, vuelos y pasajeros.
- Acceso restringido al rol `Administrador`.

## Navegación, roles y autorizaciones

El navbar mantiene visibles los módulos principales aunque no exista una sesión iniciada. Esto permite identificar las funciones disponibles y su relación con los roles, pero no concede acceso a operaciones protegidas.

- `Cliente`: puede crear y consultar sus reservas, reprogramar vuelos, pagar, consultar comprobantes y revisar su historial.
- `Administrador`: puede acceder al panel general y a los CRUD de catálogos.

Las acciones privadas utilizan `[Authorize]` y los módulos administrativos utilizan `[Authorize(Roles = "Administrador")]`. Si un visitante abre un enlace protegido, Identity solicita autenticación o impide el acceso según corresponda.

Los roles se crean al iniciar la aplicación mediante `IdentitySeeder`.

## Consultas LINQ y Entity Framework Core

El proyecto utiliza consultas LINQ para filtrar, proyectar, ordenar, agrupar, validar y paginar información. Entre los operadores empleados se encuentran:

- `Where()`: filtra vuelos, reservas, pagos y órdenes.
- `Select()`: recupera datos específicos, como rutas y asientos ocupados.
- `OrderBy()` y `OrderByDescending()`: ordenan vuelos, reservas e historiales.
- `GroupBy()`: agrupa órdenes por estado para obtener métricas.
- `Count()` y `CountAsync()`: cuentan registros y permiten calcular páginas.
- `SumAsync()` y `AverageAsync()`: calculan totales y promedios de pagos.
- `Any()` y `AnyAsync()`: verifican la existencia de reservas, asientos o transacciones.
- `Include()` y `ThenInclude()`: cargan relaciones necesarias.
- `AsNoTracking()`: optimiza consultas de solo lectura.
- `Skip()` y `Take()`: implementan paginación física.

La lista de reservas del usuario recupera entidades `Booking` con su relación `Flight`. Aunque el proyecto utiliza `Select()` en otras consultas, actualmente no proyecta las reservas hacia un `BookingSummaryViewModel`.

## Arquitectura de datos

La aplicación utiliza dos contextos sobre la misma base de datos PostgreSQL:

- `AirportContext`: administra las tablas originales de Airport mediante Database First.
- `ApplicationDbContext`: administra Identity y las tablas propias de solicitudes de cambio, órdenes, pagos e historiales.

La confirmación del pago se registra primero mediante `ApplicationDbContext`. Posteriormente se actualizan `bookings.flight_id` y `bookings.seat` mediante `AirportContext`. No se simula una transacción distribuida entre ambos contextos.

## Flujo principal de reprogramación

1. El cliente inicia sesión y abre **Reprogramar vuelo**.
2. Selecciona una reserva propia o realiza una búsqueda manual.
3. Escoge un vuelo alternativo con el mismo origen y destino.
4. Selecciona un asiento disponible.
5. Compara el vuelo actual, el nuevo vuelo y los valores calculados.
6. La aplicación crea la solicitud de cambio y la orden en estado `Pendiente`.
7. El cliente completa el pago mediante PayPal Sandbox.
8. PayPal confirma o rechaza la transacción.
9. Si el pago es aprobado, la aplicación actualiza el vuelo y el asiento de la reserva.
10. El cliente puede consultar el comprobante y su historial.

## Requisitos

- .NET SDK 10.
- PostgreSQL con la base de datos Airport proporcionada para el examen.
- Credenciales de PayPal Sandbox.

## Configuración local

`appsettings.Example.json` contiene un ejemplo de la cadena de conexión. Las credenciales reales no deben incluirse en Git.

Desde la carpeta del proyecto, registra la configuración mediante User Secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:AirportConnection" "Host=localhost;Port=5432;Database=airport;Username=TU_USUARIO;Password=TU_CONTRASENA"
dotnet user-secrets set "PayPal:ClientId" "TU_CLIENT_ID_SANDBOX"
dotnet user-secrets set "PayPal:ClientSecret" "TU_CLIENT_SECRET_SANDBOX"
dotnet user-secrets set "PayPal:BaseUrl" "https://api-m.sandbox.paypal.com"
dotnet user-secrets set "PayPal:ReturnUrl" "https://localhost:7055/Payments/PayPalSuccess"
dotnet user-secrets set "PayPal:CancelUrl" "https://localhost:7055/Payments/PayPalCancel"
```

Puedes comprobar las claves registradas —sin compartir su salida— con:

```powershell
dotnet user-secrets list
```

Las URL de retorno deben coincidir con el perfil utilizado en `Properties/launchSettings.json`. Si ejecutas el perfil HTTP, ajusta el esquema, host y puerto de las URL de PayPal.

## Restauración y ejecución

```powershell
dotnet restore
dotnet build
dotnet run
```

Perfiles locales predeterminados:

- `https://localhost:7055`
- `http://localhost:5240`

## Seguridad

- Autenticación y administración de usuarios mediante ASP.NET Core Identity.
- Acciones privadas protegidas con `[Authorize]`.
- CRUD sensibles restringidos al rol `Administrador`.
- Validación de propiedad de órdenes, pagos y reservas.
- Formularios POST protegidos mediante antiforgery token.
- Validación de disponibilidad del asiento antes de continuar y antes de actualizar la reserva.
- Totales calculados o recuperados desde el servidor.
- Credenciales de PayPal utilizadas únicamente por el servicio del servidor.
- Secretos y archivos locales excluidos mediante `.gitignore`.

## Nota sobre la base de datos Airport

`AirportContext` conserva el esquema original autorizado para el examen. Las tablas originales de Airport no deben reemplazarse con migraciones generadas desde la aplicación. Las tablas requeridas para el flujo de reprogramación se mantienen separadas dentro de `ApplicationDbContext`.
