# Product Requests API

API ASP.NET Core 8 para solicitudes de productos, ofertas y negociación entre clientes y proveedores. Persistencia MySQL 8.4, JWT, autorización por rol/propiedad, historial y concurrencia transaccional.

## Requisitos

- Docker Desktop con Docker Compose v2, o
- .NET SDK **8.0.403** y MySQL **8.4**.

Verificar SDK:

```bash
dotnet --version
# 8.0.403
```

## Ejecución con Docker Compose

Configuración predeterminada sirve solo para desarrollo local:

```bash
docker compose up --build
```

API: `http://localhost:8080`  
Swagger: `http://localhost:8080/swagger`  
Health: `http://localhost:8080/health`

Para personalizar credenciales:

```bash
cp .env.example .env
# Editar .env y reemplazar valores
docker compose up --build
```

MySQL no publica puerto al host. API usa usuario `MYSQL_USER`, nunca `root`. Migraciones y seeding idempotente se ejecutan al iniciar en `Development` después del health check de MySQL.

Detener conservando datos:

```bash
docker compose down
docker compose up --build
```

Reconstruir base desde migraciones:

```bash
docker compose down --volumes
docker compose up --build
```

## Ejecución sin Docker

Crear base y usuario de aplicación en MySQL 8.4. Después exportar configuración:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__ProductRequests='Server=localhost;Port=3306;Database=product_requests;User=product_requests;Password=LOCAL_PASSWORD;UseAffectedRows=true'
export Jwt__Issuer='product-requests-local'
export Jwt__Audience='product-requests-local-client'
export Jwt__SigningKey='replace-with-a-random-key-of-at-least-32-characters'
export Jwt__ExpirationMinutes=60

dotnet restore
dotnet tool restore
dotnet run --project src/ProductRequests.Api
```

Aplicar migraciones manualmente:

```bash
dotnet tool run dotnet-ef database update \
  --project src/ProductRequests.Infrastructure \
  --startup-project src/ProductRequests.Api
```

No usar `EnsureCreated`; esquema se administra con migraciones.

## Usuarios demo

Solo en `Development`:

| Correo | Rol |
|---|---|
| `client@example.com` | Client |
| `client2@example.com` | Client |
| `provider1@example.com` | Provider |
| `provider2@example.com` | Provider |

Contraseña local común: `Passw0rd!`. Se almacena exclusivamente como hash mediante `PasswordHasher<User>`.

## JWT y flujo sugerido

Login:

```bash
curl -s http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"client@example.com","password":"Passw0rd!"}'
```

Guardar `accessToken` como `CLIENT_TOKEN`. Repetir para proveedores.

Crear solicitud:

```bash
curl -s http://localhost:8080/api/product-requests \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"productName":"Laptop empresarial","description":"16 GB RAM y SSD","quantity":10,"currency":"USD"}'
```

Consultar abiertas y crear oferta:

```bash
curl -s http://localhost:8080/api/product-requests/open \
  -H "Authorization: Bearer $PROVIDER_TOKEN"

curl -s http://localhost:8080/api/product-requests/$REQUEST_ID/offers \
  -H "Authorization: Bearer $PROVIDER_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"amount":12500,"currency":"USD","deliveryDays":7,"notes":"Incluye transporte"}'
```

Aceptar, rechazar o contraofertar:

```bash
curl -X POST http://localhost:8080/api/offers/$OFFER_ID/accept \
  -H "Authorization: Bearer $CLIENT_TOKEN"

curl -X POST http://localhost:8080/api/offers/$OFFER_ID/reject \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"reason":"Precio fuera de presupuesto"}'

curl -X POST http://localhost:8080/api/offers/$OFFER_ID/counter-offer \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"amount":11800,"currency":"USD","comment":"Podemos aceptar este ajuste"}'
```

Respuesta del proveedor e historial:

```bash
curl -X POST http://localhost:8080/api/offers/$OFFER_ID/counter-offer/accept \
  -H "Authorization: Bearer $PROVIDER_TOKEN"

curl -s http://localhost:8080/api/offers/$OFFER_ID/history \
  -H "Authorization: Bearer $PROVIDER_TOKEN"
```

## Tests

Docker debe estar activo. Testcontainers crea MySQL 8.4 real, aplica migraciones y elimina contenedor al terminar:

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release --no-build
```

No se usa EF Core InMemory ni SQLite para persistencia.

## Variables

| Variable | Uso |
|---|---|
| `ConnectionStrings__ProductRequests` | Conexión MySQL del usuario de aplicación |
| `Jwt__Issuer` | Emisor JWT |
| `Jwt__Audience` | Audiencia JWT |
| `Jwt__SigningKey` | Clave de al menos 32 caracteres |
| `Jwt__ExpirationMinutes` | Vigencia del token |
| `MYSQL_*` | Inicialización del contenedor MySQL |
| `API_PORT` | Puerto HTTP local |

## Alcance

Incluye autenticación demo, solicitudes, ofertas, una contraoferta, aceptación/rechazo, historial y protección contra doble adjudicación. No incluye frontend, registro, refresh tokens, pagos, notificaciones, edición, cancelación ni múltiples rondas.

## Problemas comunes

- **API espera MySQL:** revisar `docker compose ps` y `docker compose logs mysql`.
- **Access denied:** confirmar coincidencia entre `MYSQL_USER`, `MYSQL_PASSWORD` y connection string.
- **Datos/configuración anteriores:** ejecutar `docker compose down --volumes` y levantar de nuevo.
- **Puerto 8080 ocupado:** cambiar `API_PORT` en `.env`.
- **SDK incorrecto:** instalar 8.0.403; `global.json` bloquea otras versiones.
- **Tests no conectan a Docker:** iniciar Docker Desktop y comprobar `docker info`.
