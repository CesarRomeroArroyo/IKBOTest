# Product Requests API

API ASP.NET Core para gestionar solicitudes de productos, ofertas de proveedores, aceptación, rechazo, contraofertas, decisión final del proveedor e historial de negociación.

## 1. Descripción

Sistema backend para proceso de negociación entre clientes y proveedores:

- clientes crean solicitudes de productos
- proveedores publican ofertas sobre solicitudes abiertas
- cliente acepta, rechaza o envía una contraoferta
- proveedor acepta o rechaza contraoferta
- sistema conserva historial cronológico de cada oferta

## 2. Alcance implementado

Implementado en repositorio actual:

- autenticación JWT
- roles `Client` y `Provider`
- creación de solicitudes de producto
- consulta paginada de solicitudes propias
- consulta paginada de solicitudes abiertas para proveedores
- creación de ofertas por proveedor
- consulta segura de ofertas por propietario
- aceptación de oferta inicial
- rechazo de oferta inicial
- contraoferta única por oferta
- aceptación de contraoferta por proveedor
- rechazo de contraoferta por proveedor
- historial de negociación por oferta
- autorización por propiedad del recurso
- transacciones locales para adjudicación y negociación
- control de concurrencia optimista
- respuestas de error con `ProblemDetails`
- pruebas unitarias de dominio
- pruebas de integración con MySQL real usando Testcontainers
- ejecución local con Docker y Docker Compose

Fuera del alcance de esta versión:

- frontend
- registro de usuarios
- refresh tokens
- cancelación de solicitudes vía API
- múltiples rondas de contraoferta
- notificaciones
- pagos

## 3. Arquitectura

Solución organizada como monolito modular por capas:

- `ProductRequests.Api`: controllers, autenticación, autorización, configuración HTTP, Swagger, health checks
- `ProductRequests.Application`: casos de uso, contratos, DTOs, reglas de autorización de aplicación
- `ProductRequests.Domain`: entidades, estados, invariantes y reglas de negocio
- `ProductRequests.Infrastructure`: Entity Framework Core, MySQL, repositorios, JWT, seeding, transacciones

Detalle técnico: [DocumentosArquitectura/Diseno-Tecnico.md](DocumentosArquitectura/Diseno-Tecnico.md)

## 4. Tecnologías

| Tecnología | Versión | Propósito |
|---|---:|---|
| .NET SDK | 8.0.403 | SDK de compilación y ejecución local |
| .NET Target Framework | net8.0 | Runtime objetivo |
| ASP.NET Core | 8.0 | API HTTP |
| C# | 12 | Lenguaje |
| Entity Framework Core | 8.0.10 | ORM, migraciones, persistencia |
| MySql.EntityFrameworkCore | 8.0.8 | Proveedor EF Core para MySQL |
| MySQL | 8.4.0 | Base de datos relacional |
| JWT Bearer | 8.0.10 | Autenticación |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger / OpenAPI |
| xUnit | 2.9.2 | Framework de pruebas |
| Microsoft.NET.Test.Sdk | 17.11.1 | Ejecución de pruebas |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.10 | `WebApplicationFactory` para integración |
| Testcontainers.MySql | 3.10.0 | MySQL efímero para pruebas de integración |
| Docker | imagen SDK 8.0.403 / ASP.NET 8.0.10 | Contenerización |
| Docker Compose | archivo `docker-compose.yml` | Orquestación local |
| GitHub Actions | `actions/checkout@v4`, `actions/setup-dotnet@v4` | CI |

## 5. Requisitos previos

### Ejecución con Docker

- Docker
- Docker Compose v2

### Ejecución local sin Docker

- .NET SDK `8.0.403`
- MySQL `8.4.x`
- Git

Verificación realizada:

```bash
dotnet --version
```

Resultado esperado en este repositorio:

```text
8.0.403
```

## 6. Configuración

Variables realmente usadas por aplicación y contenedores:

| Variable | Descripción | Obligatoria | Ejemplo local seguro |
|---|---|---|---|
| `ConnectionStrings__ProductRequests` | Connection string principal de MySQL | Sí, sin Docker local | `Server=localhost;Port=3306;Database=product_requests;User=product_requests;Password=local_only_password;UseAffectedRows=true` |
| `Jwt__Issuer` | Emisor del token JWT | Sí | `product-requests-local` |
| `Jwt__Audience` | Audiencia del token JWT | Sí | `product-requests-local-client` |
| `Jwt__SigningKey` | Clave HMAC para firmar JWT. Debe tener al menos 32 caracteres para operación válida | Sí | `replace_with_at_least_32_random_characters` |
| `Jwt__ExpirationMinutes` | Minutos de expiración del token | Sí | `60` |
| `MYSQL_DATABASE` | Nombre de base local Docker | Solo Docker | `product_requests` |
| `MYSQL_USER` | Usuario de aplicación MySQL | Solo Docker | `product_requests` |
| `MYSQL_PASSWORD` | Contraseña del usuario de aplicación | Solo Docker | `replace_with_local_password` |
| `MYSQL_ROOT_PASSWORD` | Contraseña root del contenedor MySQL | Solo Docker | `replace_with_local_root_password` |
| `API_PORT` | Puerto publicado de API | No | `8080` |

Archivo `.env.example` existe. Copia recomendada:

```bash
cp .env.example .env
```

Editar `.env` antes de compartir ambiente.

## 7. Ejecución con Docker

Comando validado:

```bash
docker compose up --build
```

Puertos verificados:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Health check: `http://localhost:8080/health`
- MySQL no publica puerto al host en `docker-compose.yml`

Detener contenedores:

```bash
docker compose down
```

Eliminar volumen para reiniciar base desde cero:

```bash
docker compose down --volumes
```

Comandos verificados:

```bash
docker compose config
docker compose build
docker compose up -d
docker compose ps
```

## 8. Ejecución local sin Docker

Restauración:

```bash
dotnet restore ProductRequests.sln
```

Configurar variables:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__ProductRequests='Server=localhost;Port=3306;Database=product_requests;User=product_requests;Password=local_only_password;UseAffectedRows=true'
export Jwt__Issuer='product-requests-local'
export Jwt__Audience='product-requests-local-client'
export Jwt__SigningKey='replace_with_at_least_32_random_characters'
export Jwt__ExpirationMinutes=60
```

Aplicar migraciones:

```bash
dotnet ef database update --project src/ProductRequests.Infrastructure --startup-project src/ProductRequests.Api
```

Ejecutar API:

```bash
dotnet run --project src/ProductRequests.Api
```

En `Development`, aplicación ejecuta migraciones y seeding al iniciar.

## 9. Migraciones

- DbContext: `src/ProductRequests.Infrastructure/Persistence/ProductRequestsDbContext.cs`
- Migraciones: `src/ProductRequests.Infrastructure/Persistence/Migrations/`
- Proyecto de migraciones: `src/ProductRequests.Infrastructure`
- Startup project: `src/ProductRequests.Api`
- Factory de diseño: `src/ProductRequests.Infrastructure/Persistence/ProductRequestsDbContextFactory.cs`

Aplicar migraciones:

```bash
dotnet ef database update --project src/ProductRequests.Infrastructure --startup-project src/ProductRequests.Api
```

Crear migración nueva:

```bash
dotnet ef migrations add NombreMigracion --project src/ProductRequests.Infrastructure --startup-project src/ProductRequests.Api
```

Para operaciones de diseño fuera de Docker, `ProductRequestsDbContextFactory` exige variable `ConnectionStrings__ProductRequests`.

## 10. Usuarios de demostración

Seeder real: `DemoUserSeeder`.

Disponibles solo en ambiente `Development`:

| Correo | Rol | Contraseña local de demostración |
|---|---|---|
| `client@example.com` | Client | `Passw0rd!` |
| `client2@example.com` | Client | `Passw0rd!` |
| `provider1@example.com` | Provider | `Passw0rd!` |
| `provider2@example.com` | Provider | `Passw0rd!` |

Estas credenciales son exclusivas para entorno local de desarrollo.

## 11. Flujo de prueba reproducible

### 11.1 Autenticarse como cliente

```bash
curl -s http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"client@example.com","password":"Passw0rd!"}'
```

Guardar `accessToken` como `CLIENT_TOKEN`.

### 11.2 Crear solicitud

```bash
curl -s http://localhost:8080/api/product-requests \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"productName":"Laptop empresarial","description":"16 GB RAM y SSD","quantity":2,"currency":"USD"}'
```

Guardar `id` como `REQUEST_ID`.

### 11.3 Autenticarse como proveedor 1 y crear oferta

```bash
curl -s http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"provider1@example.com","password":"Passw0rd!"}'
```

Guardar token como `PROVIDER1_TOKEN`.

```bash
curl -s http://localhost:8080/api/product-requests/$REQUEST_ID/offers \
  -H "Authorization: Bearer $PROVIDER1_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"amount":1200,"currency":"USD","deliveryDays":5,"notes":"Oferta 1"}'
```

Guardar `id` como `OFFER1_ID`.

### 11.4 Autenticarse como proveedor 2 y crear segunda oferta

```bash
curl -s http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"provider2@example.com","password":"Passw0rd!"}'
```

Guardar token como `PROVIDER2_TOKEN`.

```bash
curl -s http://localhost:8080/api/product-requests/$REQUEST_ID/offers \
  -H "Authorization: Bearer $PROVIDER2_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"amount":1100,"currency":"USD","deliveryDays":7,"notes":"Oferta 2"}'
```

Guardar `id` como `OFFER2_ID`.

### 11.5 Volver como cliente y consultar ofertas

```bash
curl -s http://localhost:8080/api/product-requests/$REQUEST_ID/offers \
  -H "Authorization: Bearer $CLIENT_TOKEN"
```

### 11.6 Aceptar, rechazar o contraofertar

Aceptar oferta inicial:

```bash
curl -s -X POST http://localhost:8080/api/offers/$OFFER1_ID/accept \
  -H "Authorization: Bearer $CLIENT_TOKEN"
```

Rechazar oferta inicial:

```bash
curl -s -X POST http://localhost:8080/api/offers/$OFFER1_ID/reject \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"reason":"Precio fuera de presupuesto"}'
```

Enviar contraoferta:

```bash
curl -s -X POST http://localhost:8080/api/offers/$OFFER1_ID/counter-offer \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"amount":1150,"currency":"USD","comment":"Ajuste presupuesto"}'
```

Proveedor acepta contraoferta:

```bash
curl -s -X POST http://localhost:8080/api/offers/$OFFER1_ID/counter-offer/accept \
  -H "Authorization: Bearer $PROVIDER1_TOKEN"
```

Proveedor rechaza contraoferta:

```bash
curl -s -X POST http://localhost:8080/api/offers/$OFFER1_ID/counter-offer/reject \
  -H "Authorization: Bearer $PROVIDER1_TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"reason":"No es viable"}'
```

### 11.7 Consultar historial

```bash
curl -s http://localhost:8080/api/offers/$OFFER1_ID/history \
  -H "Authorization: Bearer $PROVIDER1_TOKEN"
```

Flujo validado en ejecución real con Docker: login, creación de solicitud, creación de dos ofertas, contraoferta, aceptación de contraoferta e historial.

## 12. Endpoints

| Método | Ruta | Rol | Descripción | Respuesta principal |
|---|---|---|---|---|
| POST | `/api/auth/login` | Anónimo | Autenticación y emisión de JWT | `200 OK` |
| GET | `/api/auth/me` | Autenticado | Devuelve claims principales del usuario actual | `200 OK` |
| POST | `/api/product-requests` | Client | Crea solicitud | `201 Created` |
| GET | `/api/product-requests/mine` | Client | Lista solicitudes propias paginadas | `200 OK` |
| GET | `/api/product-requests/open` | Provider | Lista solicitudes abiertas paginadas | `200 OK` |
| GET | `/api/product-requests/{requestId}` | Client/Provider* | Consulta detalle según reglas de acceso | `200 OK` |
| POST | `/api/product-requests/{requestId}/offers` | Provider | Crea oferta | `201 Created` |
| GET | `/api/product-requests/{requestId}/offers` | Client | Lista ofertas de solicitud propia | `200 OK` |
| GET | `/api/offers/mine` | Provider | Lista ofertas propias paginadas | `200 OK` |
| GET | `/api/offers/{offerId}` | Client/Provider* | Consulta oferta según propiedad | `200 OK` |
| POST | `/api/offers/{offerId}/accept` | Client | Acepta oferta inicial | `200 OK` |
| POST | `/api/offers/{offerId}/reject` | Client | Rechaza oferta inicial | `200 OK` |
| POST | `/api/offers/{offerId}/counter-offer` | Client | Envía contraoferta | `200 OK` |
| POST | `/api/offers/{offerId}/counter-offer/accept` | Provider | Acepta contraoferta | `200 OK` |
| POST | `/api/offers/{offerId}/counter-offer/reject` | Provider | Rechaza contraoferta | `200 OK` |
| GET | `/api/offers/{offerId}/history` | Client/Provider* | Consulta historial de oferta | `200 OK` |
| GET | `/health` | Anónimo | Health check | `200 OK` |

\* Acceso condicionado por propiedad del recurso.

## 13. Pruebas

Comandos verificados:

```bash
dotnet test ProductRequests.sln -c Release --no-build
dotnet test -c Release
```

Tipos de pruebas existentes:

- pruebas de dominio: validan invariantes, estados, adjudicación, contraofertas y versiones
- pruebas de integración: validan autenticación, autorización, endpoints, persistencia, transacciones, historial y concurrencia

Detalles relevantes:

- integración usa `WebApplicationFactory`
- integración usa contenedor MySQL real con `Testcontainers.MySql`
- Docker activo es requisito para pruebas de integración
- ejecución actual verificada: 18 pruebas de dominio y 51 pruebas de integración, total 69

## 14. CI/CD

Workflow real: `.github/workflows/ci.yml`

Pasos implementados:

- checkout
- setup de .NET SDK `8.0.403`
- verificación de SDK y `net8.0` en proyectos
- restore
- build Release
- test Release
- validación de `docker compose config --quiet`
- `docker compose build`
- publicación de resultados `.trx`

Trigger actual: `push` y `pull_request` sobre rama `master`.

## 15. Decisiones y limitaciones

Decisiones principales:

- monolito modular para simplificar operación y mantener transacciones locales
- MySQL relacional para consistencia fuerte entre solicitudes, ofertas e historial
- JWT Bearer simple para prueba técnica
- concurrencia optimista con token `Version` en `ProductRequest` y `Offer`
- historial persistente por oferta para auditabilidad

Limitaciones actuales:

- una sola ronda de contraoferta por oferta
- sin refresh token
- sin frontend
- sin notificaciones ni eventos asíncronos
- `Cancelled` existe en enum de `ProductRequestStatus`, pero no hay caso de uso ni endpoint para cancelación

Evolución futura:

- notificaciones asíncronas
- múltiples rondas de negociación
- endpoints de cancelación
- caché de consultas y optimización de lectura
- separación futura de módulos si volumen operativo lo exige

## 16. Documentación adicional

- [Diseño técnico](DocumentosArquitectura/Diseno-Tecnico.md)
- [Diagrama de contexto](DocumentosArquitectura/Diagrama-Contexto.md)
- [Diagrama de contenedores](DocumentosArquitectura/Diagrama-Contenedores.md)
- [Diagrama de dominio](DocumentosArquitectura/Diagrama-Dominio.md)
- [Diagrama de estados](DocumentosArquitectura/Diagrama-Estados.md)
- [Secuencia de aceptación](DocumentosArquitectura/Diagrama-Secuencia-Aceptacion.md)
- [Secuencia de contraoferta](DocumentosArquitectura/Diagrama-Secuencia-Contraoferta.md)
- [Justificación técnica](DocumentosArquitectura/Justificacion.md)
- [Gestión de equipo](DocumentosArquitectura/gestion-de-equipo.md)
