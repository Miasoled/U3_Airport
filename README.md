# U3 Examen Airport — Tipo 4

Aplicación ASP.NET Core MVC .NET 10 para consultar reservas y gestionar el cambio o reprogramación de vuelos. Usa PostgreSQL, Entity Framework Core, Identity y PayPal Sandbox mediante Orders API v2.

## Funcionalidad implementada

- Registro e inicio de sesión con roles `Cliente` y `Administrador`.
- Consulta segura de reservas asociadas al correo del usuario.
- Búsqueda manual por reserva y pasaporte.
- Búsqueda de vuelos alternativos con el mismo origen y destino.
- Comparación entre el vuelo actual y el nuevo.
- Cálculo en servidor de diferencia de tarifa, penalización y total.
- Creación transaccional de solicitud, orden y detalle de orden.
- Pago con PayPal Sandbox, captura y comprobante.
- Estados `Pendiente`, `Aprobado`, `Cancelado`, `Rechazado` y `Fallido`.
- Historial de cambios y transacciones.
- Historial paginado del cliente.
- Panel administrativo con filtros, métricas y operaciones recientes.
- CRUD administrativos protegidos por rol.

## Arquitectura de datos

La aplicación utiliza dos contextos sobre la misma base PostgreSQL:

- `AirportContext`: tablas originales de Airport, generado con Database First.
- `ApplicationDbContext`: Identity y tablas propias de reprogramación, órdenes, pagos e historiales.

La confirmación de PayPal se guarda primero en `ApplicationDbContext` y después se actualiza `bookings.flight_id` con `AirportContext`. No se simula una transacción distribuida entre ambos contextos.

## Requisitos

- .NET SDK 10
- PostgreSQL con la base Airport proporcionada para el examen
- Cuenta de desarrollador y credenciales de PayPal Sandbox

## Configuración segura

El archivo `appsettings.Example.json` documenta todas las claves necesarias, pero contiene únicamente valores de ejemplo. Las credenciales reales no deben incluirse en Git.

Desde la carpeta del proyecto, configura User Secrets:

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

En el panel de PayPal Sandbox, las URL de retorno deben coincidir con las configuradas. Si usas el perfil HTTP, cambia el host y puerto según `Properties/launchSettings.json`.

## Ejecución

```powershell
dotnet restore
dotnet build
dotnet run
```

Perfiles locales predeterminados:

- `https://localhost:7055`
- `http://localhost:5240`

## Flujo principal

1. El cliente inicia sesión y abre **Reprogramar vuelo**.
2. Selecciona una reserva y un boleto.
3. Busca un vuelo alternativo del mismo origen y destino.
4. Compara importes y confirma la solicitud.
5. Selecciona PayPal y completa el pago en Sandbox.
6. La aplicación captura el pago, registra los historiales y actualiza el vuelo de la reserva.
7. El cliente consulta el comprobante o su historial.

## Roles

- `Cliente`: reservas propias, reprogramación, pago, comprobante e historial personal.
- `Administrador`: panel general, acceso a operaciones y CRUD de catálogos.

Los roles se crean al iniciar la aplicación mediante `IdentitySeeder`.

## Seguridad

- Acciones privadas protegidas con `[Authorize]`.
- CRUD sensibles restringidos a `Administrador`.
- Validación de propiedad de órdenes, pagos y reservas.
- Formularios POST protegidos con antiforgery token.
- Totales recalculados o leídos desde la orden del servidor.
- El secreto de PayPal se usa únicamente en el servicio del servidor.
- `appsettings.json`, archivos locales y secretos están excluidos mediante `.gitignore`.

## Nota sobre la base Airport

`AirportContext` conserva el esquema original autorizado para el examen. No debe reemplazarse por migraciones de las tablas Airport. El proyecto mantiene por separado las tablas propias requeridas para el flujo Tipo 4.
