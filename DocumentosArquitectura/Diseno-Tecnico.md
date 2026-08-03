# Diseño técnico

## 1. Contexto

Sistema resuelve negociación básica entre dos actores:

- **Cliente**: crea solicitud de compra y decide entre ofertas recibidas.
- **Proveedor**: consulta solicitudes abiertas, publica oferta y responde contraofertas.

Problema de negocio: centralizar solicitud, comparación de ofertas, adjudicación y trazabilidad de decisiones sin exponer ofertas competidoras a terceros no autorizados.

## 2. Objetivos técnicos

- **Seguridad**: autenticación JWT, roles y autorización por propiedad.
- **Consistencia**: adjudicación atómica y protección frente a doble aceptación.
- **Mantenibilidad**: separación por capas y dependencias acotadas.
- **Escalabilidad**: base relacional e índices para consultas principales.
- **Auditabilidad**: historial cronológico persistente por oferta.
- **Facilidad de prueba**: dominio aislado y pruebas de integración con MySQL real.

## 3. Arquitectura seleccionada

Arquitectura actual: **monolito modular por capas**.

### 3.1 Capas y responsabilidades

#### API

Proyecto: `ProductRequests.Api`

Responsabilidades reales:

- exponer controllers HTTP
- validar autenticación y políticas de rol
- obtener usuario actual desde claims
- configurar Swagger y health checks
- mapear excepciones a `ProblemDetails`

#### Application

Proyecto: `ProductRequests.Application`

Responsabilidades reales:

- orquestar casos de uso
- aplicar reglas de autorización de aplicación
- definir comandos, DTOs y abstracciones
- invocar transacciones a través de `IUnitOfWork`

#### Domain

Proyecto: `ProductRequests.Domain`

Responsabilidades reales:

- entidades de negocio
- estados y transiciones
- invariantes
- objeto de valor `Money`
- historial de cambios sobre ofertas

#### Infrastructure

Proyecto: `ProductRequests.Infrastructure`

Responsabilidades reales:

- persistencia EF Core con MySQL
- repositorios
- configuración de entidades y migraciones
- implementación JWT
- hashing y verificación de contraseñas
- transacciones
- seeding de usuarios demo

### 3.2 Dependencias permitidas

- `Api` → `Application`, `Infrastructure`
- `Application` → `Domain`
- `Infrastructure` → `Application`, `Domain`
- `Domain` sin dependencias hacia capas externas

## 4. Modelo de dominio

### 4.1 User

Representa identidad del sistema.

Propiedades relevantes:

- `Id`
- `Name`
- `Email`
- `NormalizedEmail`
- `PasswordHash`
- `Role`
- `IsActive`
- `CreatedAt`

Responsabilidades:

- normalizar correo
- representar rol y estado activo

Invariantes:

- correo, nombre y hash obligatorios al crear

### 4.2 ProductRequest

Representa solicitud creada por cliente.

Propiedades relevantes:

- `Id`
- `ClientId`
- `ProductName`
- `Description`
- `Quantity`
- `Currency`
- `Status`
- `AcceptedOfferId`
- `CreatedAt`
- `UpdatedAt`
- `Version`
- `Offers`

Responsabilidades:

- aceptar nuevas ofertas
- aceptar o rechazar oferta inicial
- registrar contraoferta
- aceptar o rechazar respuesta a contraoferta
- adjudicar solicitud y marcar ofertas competidoras como no seleccionadas

Invariantes:

- cantidad mayor que cero
- moneda ISO de 3 letras
- solo solicitudes `Open` aceptan nuevas operaciones
- un proveedor no puede ofertar dos veces sobre misma solicitud
- solo una oferta puede quedar aceptada

### 4.3 Offer

Representa oferta de proveedor para una solicitud.

Propiedades relevantes:

- `Id`
- `ProductRequestId`
- `ProviderId`
- `ProposedAmount`
- `CounterAmount`
- `AgreedAmount`
- `DeliveryDays`
- `Notes`
- `Status`
- `CreatedAt`
- `UpdatedAt`
- `Version`
- `Histories`

Responsabilidades:

- recibir aceptación o rechazo inicial del cliente
- recibir contraoferta del cliente
- aceptar o rechazar contraoferta por proveedor
- registrar historial de transición

Invariantes:

- monto mayor que cero
- plazo de entrega mayor que cero
- terminales `Accepted`, `Rejected` y `NotSelected` no admiten nuevas transiciones
- solo una contraoferta por oferta

### 4.4 OfferHistory

Representa evento auditable sobre oferta.

Propiedades relevantes:

- `OfferId`
- `ProductRequestId`
- `ActorId`
- `ActorRole`
- `Action`
- `PreviousStatus`
- `NewStatus`
- `Amount`
- `Comment`
- `OccurredAt`

Responsabilidad:

- conservar trazabilidad cronológica de negociación y adjudicación

### 4.5 Money

Objeto de valor inmutable.

Propiedades:

- `Amount`
- `Currency`

Invariantes:

- monto positivo
- moneda normalizada a mayúsculas con 3 letras

## 5. Estados

### 5.1 ProductRequest

Estados reales:

- `Open`
- `Awarded`
- `Cancelled`

Transiciones implementadas:

- `Open -> Awarded`

`Cancelled` existe en modelo, pero no tiene transición implementada ni endpoint expuesto. Debe tratarse como **fuera del alcance de esta versión**.

### 5.2 Offer

Estados reales:

- `PendingClientDecision`
- `PendingProviderDecision`
- `Accepted`
- `Rejected`
- `NotSelected`

Transiciones implementadas:

- `PendingClientDecision -> Accepted`
- `PendingClientDecision -> Rejected`
- `PendingClientDecision -> PendingProviderDecision`
- `PendingProviderDecision -> Accepted`
- `PendingProviderDecision -> Rejected`
- `PendingClientDecision -> NotSelected`
- `PendingProviderDecision -> NotSelected`

Estados terminales:

- `Accepted`
- `Rejected`
- `NotSelected`

## 6. Casos de uso

### 6.1 Autenticación

- entrada: correo y contraseña
- repositorio localiza usuario por `NormalizedEmail`
- verificador valida hash
- generador emite JWT con claims `sub`, `email`, `role`

### 6.2 Crear solicitud

- usuario `Client`
- valida campos básicos
- crea `ProductRequest`
- persiste con estado `Open`

### 6.3 Consultar solicitudes

- cliente obtiene solo solicitudes propias
- proveedor obtiene solo solicitudes `Open`
- detalle de solicitud depende de rol y propiedad

### 6.4 Crear oferta

- usuario `Provider`
- carga solicitud
- valida moneda y unicidad de proveedor
- crea `Offer` con historial `OfferSubmitted`

### 6.5 Consultar ofertas

- cliente propietario puede ver todas las ofertas de su solicitud
- proveedor solo puede ver sus propias ofertas

### 6.6 Aceptar

- cliente acepta oferta inicial, o proveedor acepta contraoferta
- adjudicación actualiza oferta elegida, solicitud y competidores dentro de transacción

### 6.7 Rechazar

- cliente rechaza oferta inicial
- proveedor rechaza contraoferta
- solicitud permanece `Open`

### 6.8 Contraofertar

- cliente envía contraoferta única
- oferta pasa a `PendingProviderDecision`

### 6.9 Responder contraoferta

- proveedor acepta o rechaza si es dueño de oferta

### 6.10 Consultar historial

- cliente dueño de solicitud y proveedor dueño de oferta pueden ver historial cronológico

## 7. Persistencia

### 7.1 Base de datos

- motor: MySQL 8.4.0
- engine de tablas: InnoDB
- charset: `utf8mb4`
- collation Docker local: `utf8mb4_0900_ai_ci`

### 7.2 Entity Framework Core

- mapeo por configuraciones separadas
- migración inicial versiona esquema
- `UseAffectedRows=true` en connection strings usadas por proyecto

### 7.3 Tablas y relaciones

- `Users`
- `ProductRequests`
- `Offers`
- `OfferHistories`

Relaciones reales:

- `ProductRequests.ClientId -> Users.Id`
- `Offers.ProductRequestId -> ProductRequests.Id`
- `Offers.ProviderId -> Users.Id`
- `OfferHistories.OfferId -> Offers.Id`
- `OfferHistories.ProductRequestId -> ProductRequests.Id`
- `OfferHistories.ActorId -> Users.Id`

Todos los `DeleteBehavior` configurados como `Restrict`.

### 7.4 Índices y restricciones

Índices y restricciones reales:

- `UX_Users_NormalizedEmail` único
- `IX_ProductRequests_Status`
- `IX_ProductRequests_ClientId`
- `IX_Offers_ProductRequestId`
- `IX_Offers_ProviderId`
- `UX_Offers_ProductRequestId_ProviderId` único
- `IX_OfferHistories_OfferId_OccurredAt`

### 7.5 Tratamiento de dinero

- `Money` valida monto y moneda en dominio
- persistencia guarda montos como `decimal(18,2)`
- moneda se guarda en `ProductRequest.Currency` como `char(3)`

## 8. Transacciones

Operaciones de negociación se ejecutan con `IUnitOfWork.ExecuteInTransactionAsync` sobre aislamiento `ReadCommitted`.

Cambios que se confirman de forma atómica durante adjudicación:

- oferta seleccionada cambia a `Accepted`
- solicitud cambia a `Awarded`
- `AcceptedOfferId` se establece
- ofertas competidoras abiertas cambian a `NotSelected`
- historial de aceptación y adjudicación se persiste

Esto aplica tanto para aceptación inicial como para aceptación de contraoferta.

## 9. Concurrencia

Implementación real: concurrencia optimista basada en propiedad `Version` tipo `Guid`.

### 9.1 Entidades protegidas

- `ProductRequest.Version`
- `Offer.Version`

Ambas configuradas como `IsConcurrencyToken()`.

### 9.2 Actualización del token

Dominio renueva `Version` cuando cambia estado relevante mediante método `Touch`.

### 9.3 Verificación

`ProductRequestsDbContext.SaveChangesAsync` consulta versión persistida en base y compara contra `OriginalValue` del change tracker.

Si versión persistida difiere:

- lanza `DbUpdateConcurrencyException`
- capa API responde `409 Conflict`
- código estable: `CONCURRENCY_CONFLICT`

### 9.4 Cómo se evita doble aceptación

- `GetByOfferIdForUpdateAsync` bloquea fila de `ProductRequests` con `FOR UPDATE`
- dominio verifica `AcceptedOfferId`
- concurrencia optimista detecta cambios paralelos

Combinación evita que dos operaciones de aceptación dejen dos ganadores.

## 10. Seguridad

### 10.1 JWT

- esquema: Bearer
- claims emitidos: `sub`, `email`, `role`
- validación de issuer, audience, signing key y expiración
- `ClockSkew = TimeSpan.Zero`

### 10.2 Roles

Roles reales:

- `Client`
- `Provider`

Políticas:

- `Client`
- `Provider`

### 10.3 Autorización por propiedad

Además del rol, aplicación valida propiedad:

- cliente solo accede a solicitudes donde `ClientId == currentUser.Id`
- proveedor solo accede a ofertas donde `ProviderId == currentUser.Id`
- historial y detalle de oferta requieren cliente dueño o proveedor dueño

### 10.4 Contraseñas

- hash con `PasswordHasher<User>` de ASP.NET Core Identity
- verificación mediante `PasswordVerifier`

### 10.5 Secretos

- JWT y connection string llegan por configuración externa
- no hay secretos hardcodeados para producción
- usuarios demo solo para `Development`

### 10.6 Protección de ofertas competidoras

- proveedor no puede consultar oferta ajena
- cliente no propietario no puede consultar ofertas de otra solicitud

### 10.7 Identidad desde token

`CurrentUser` extrae `sub`, `email` y `role` desde claims. Si faltan o son inválidos, lanza error de autenticación.

## 11. Manejo de errores

Sistema usa `ProblemDetails` para errores de validación, autorización, concurrencia, conflictos y fallos inesperados.

### 11.1 Códigos HTTP usados

- `400 Bad Request`: validaciones
- `401 Unauthorized`: credenciales inválidas o ausentes
- `403 Forbidden`: rol o propiedad no autorizada
- `404 Not Found`: recursos inexistentes
- `409 Conflict`: conflictos de negocio o concurrencia
- `500 Internal Server Error`: fallo inesperado

### 11.2 Códigos internos

Ejemplos reales:

- `VALIDATION_ERROR`
- `INVALID_CREDENTIALS`
- `RESOURCE_ACCESS_DENIED`
- `PRODUCT_REQUEST_NOT_FOUND`
- `OFFER_NOT_FOUND`
- `DUPLICATE_PROVIDER_OFFER`
- `REQUEST_ALREADY_AWARDED`
- `CONCURRENCY_CONFLICT`

### 11.3 Trazabilidad

Cada `ProblemDetails` agrega:

- `code`
- `traceId`

Errores inesperados no exponen mensaje interno ni stack trace en respuesta.

## 12. Pruebas

### 12.1 Dominio

Cubren:

- creación válida e inválida
- unicidad de oferta por proveedor
- aceptación inicial
- rechazo inicial
- contraoferta única
- aceptación y rechazo de contraoferta
- marcación de competidores no seleccionados
- actualización de versión y timestamps

### 12.2 Integración

Cubren:

- login y claims JWT
- rechazo de credenciales inválidas y token expirado
- endpoints de solicitudes, ofertas e historial
- autorización por rol y propiedad
- persistencia real en MySQL
- índices y restricciones críticas
- conflictos por duplicidad
- concurrencia en aceptación y rechazo
- adjudicación atómica

### 12.3 Infraestructura de pruebas

- `WebApplicationFactory<Program>`
- `Testcontainers.MySql`
- migraciones aplicadas en contenedor real
- paralelización deshabilitada a nivel assembly en integración

## 13. Observabilidad

Implementado actualmente:

- logging estándar ASP.NET Core
- `ApiExceptionHandler` registra errores conocidos como warning y errores inesperados como error
- endpoint `/health`
- `traceId` en respuestas de error

No hay observabilidad centralizada ni métricas especializadas en esta versión.

## 14. Escalabilidad

### 14.1 Arquitectura actual

- una API ASP.NET Core
- una base MySQL
- transacciones locales síncronas

### 14.2 Evolución futura

Posibles pasos, no implementados actualmente:

- varias instancias de API detrás de balanceador
- base administrada MySQL
- réplicas de lectura para consultas de listados
- caché para endpoints de consulta frecuente
- notificaciones asíncronas mediante eventos
- separación futura de módulos con límites ya visibles en capas
- observabilidad centralizada con trazas y métricas

## 15. Decisiones de alcance

No se implementaron en esta versión:

- **Microservicios**: complejidad operacional innecesaria para alcance actual.
- **Frontend**: prueba concentrada en backend y reglas de negocio.
- **Pagos**: no pertenecen al flujo negociado implementado.
- **Mensajería**: no hay requerimiento de integración asíncrona en alcance base.
- **Múltiples rondas**: se implementó una sola contraoferta por simplicidad y consistencia.
- **Notificaciones**: identificadas como evolución futura.
