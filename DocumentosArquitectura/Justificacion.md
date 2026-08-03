# Justificación técnica

## 13.1 Resumen de decisiones

| Decisión | Alternativas consideradas | Elección | Motivo |
|---|---|---|---|
| Estilo arquitectónico | Monolito modular, microservicios | Monolito modular | Alcance acotado, menor costo operativo, transacciones locales simples |
| Persistencia | MySQL, PostgreSQL, NoSQL | MySQL | Modelo relacional fuerte, restricciones, índices y operación simple con Docker |
| ORM | EF Core, SQL manual, Dapper | EF Core | Mapeo, migraciones, integración natural con .NET y soporte de concurrencia |
| Autenticación | JWT Bearer, cookies, OAuth completo | JWT Bearer | API stateless, simple de probar en Swagger y curl |
| Concurrencia | Locking pesimista exclusivo, optimista, sin control | Optimista + `FOR UPDATE` sobre request | Protege adjudicación sin complejidad distribuida |
| Historial | Auditoría externa, tabla de eventos simple, sin historial | `OfferHistory` persistido | Trazabilidad explícita de negociación |
| Pruebas de integración | InMemory, SQLite, MySQL real | MySQL real con Testcontainers | Valida índices, restricciones y comportamiento real del proveedor |

## 13.2 ¿Por qué se eligió esta arquitectura?

Se eligió **monolito modular con separación por capas** porque problema actual exige coherencia transaccional entre solicitud, oferta aceptada, ofertas competidoras e historial.

Ventajas para este alcance:

- simplicidad operacional
- una sola unidad desplegable
- transacciones locales reales
- menor superficie de fallos distribuidos
- mantenibilidad por responsabilidades claras
- posibilidad de evolución posterior sin sobreingeniería temprana

Comparación breve con microservicios:

- microservicios introducirían coordinación distribuida, observabilidad más compleja, contratos entre servicios y mayor costo de despliegue
- para un backend pequeño con reglas de negocio centrales y consistencia fuerte, ese costo no se justifica

## 13.3 ¿Por qué .NET 8?

Razones:

- versión LTS
- ecosistema maduro para APIs empresariales
- ASP.NET Core estable y performante
- lenguaje tipado fuerte con C# 12
- inyección de dependencias integrada
- soporte sólido de pruebas, logging y configuración

SDK fijado en repositorio: **8.0.403** mediante `global.json`.

## 13.4 ¿Por qué MySQL?

Razones:

- base relacional adecuada para relaciones `User -> ProductRequest -> Offer -> OfferHistory`
- soporte de transacciones
- índices y restricciones únicas
- integridad referencial
- operación sencilla con Docker Compose
- compatibilidad con EF Core en stack actual

Modelo de negociación requiere consistencia fuerte y consultas relacionales claras; MySQL encaja bien con ese patrón.

## 13.5 ¿Por qué Entity Framework Core?

Beneficios obtenidos:

- mapeo entre entidades y tablas
- migraciones versionadas
- consultas LINQ para listados y cargas agregadas
- integración con transacciones y excepciones de concurrencia
- testabilidad con mismo proveedor sobre contenedor real

Trade-offs asumidos:

- menos control explícito que SQL manual en cada consulta
- riesgo de N+1 si cargas se diseñan mal
- necesidad de revisar índices y SQL en consultas críticas

Repositorio ya mitiga parte de esto usando `Include`, consultas paginadas y una consulta SQL puntual con `FOR UPDATE` para caso crítico de adjudicación.

## 13.6 Seguridad de transacciones

Controles implementados:

- autenticación JWT
- roles `Client` y `Provider`
- autorización por propiedad del recurso
- hash de contraseñas con `PasswordHasher<User>`
- transacciones locales `ReadCommitted`
- restricción única `UX_Offers_ProductRequestId_ProviderId`
- concurrencia optimista con `Version`
- bloqueo `FOR UPDATE` sobre solicitud antes de adjudicar
- ocultamiento de ofertas competidoras a proveedores ajenos
- `ProblemDetails` sin detalles internos sensibles en errores inesperados

## 13.7 Escalabilidad

### Etapa 1

- escalamiento vertical de API y MySQL
- revisión de índices
- optimización de consultas de lectura

### Etapa 2

- varias instancias de API detrás de load balancer
- base MySQL administrada
- caché para listados y consultas de solo lectura

### Etapa 3

- eventos para notificaciones
- separación gradual de módulos con límites claros
- réplicas de lectura
- observabilidad centralizada

Microservicios no deben ser primera respuesta automática. Solo tendrían sentido cuando presión operativa y de dominio lo justifique.

## 13.8 Trade-offs

- una sola contraoferta por oferta
- sin frontend
- sin mensajería
- JWT sin refresh token
- seeding solo en `Development`
- consistencia fuerte priorizada sobre disponibilidad eventual

## 13.9 Riesgos y mitigaciones

| Riesgo | Mitigación real o futura |
|---|---|
| Condiciones de carrera al aceptar dos ofertas | `FOR UPDATE`, transacción local, `Version`, `DbUpdateConcurrencyException` |
| Fuga de ofertas competidoras | validación de propiedad en `ResourceAuthorizationService` |
| Duplicidad de ofertas por proveedor | restricción única `UX_Offers_ProductRequestId_ProviderId` + validación de dominio |
| Secretos débiles o expuestos | variables de entorno y `.env.example` sin secretos reales |
| Migraciones inconsistentes | migraciones versionadas y CI con build/test/docker build |
| Consultas lentas futuras | índices actuales y evolución futura con revisión de planes y caché |
| Historial creciente | índice por `OfferId, OccurredAt`; evolución futura con archivado o partición si volumen crece |
| Dependencia de base relacional única | respaldo y evolución futura a servicio administrado o réplicas |
