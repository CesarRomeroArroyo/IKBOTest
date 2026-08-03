# Gestión de equipo

## 14.1 Enfoque de liderazgo

Para un equipo de 2 o 3 desarrolladores:

- alcance claro desde inicio
- decisiones técnicas visibles y documentadas
- comunicación corta y frecuente
- propiedad compartida del código
- calidad integrada desde primer sprint
- eliminación temprana de bloqueos técnicos

Rol de liderazgo técnico: reducir ambigüedad, proteger coherencia de diseño y mantener balance entre velocidad y mantenibilidad.

## 14.2 Organización del trabajo

Plan recomendado: sprints de 1 o 2 semanas. Para este tipo de backend, 1 semana funciona bien si backlog está acotado.

### Ejemplo de plan

#### Sprint 0

- refinamiento de alcance
- decisiones de arquitectura
- estructura del repositorio
- CI inicial
- modelo de dominio
- identificación de riesgos

#### Sprint 1

- autenticación
- solicitudes
- ofertas
- persistencia inicial

#### Sprint 2

- negociación
- concurrencia
- historial
- pruebas de integración

#### Sprint 3

- hardening
- documentación
- seguridad
- observabilidad básica
- preparación de entrega

Plan debe adaptarse según riesgo principal encontrado en refinamiento.

## 14.3 Asignación para dos desarrolladores

Ejemplo:

- **Desarrollador A**: dominio, negociación, pruebas unitarias
- **Desarrollador B**: API, seguridad, persistencia, pruebas de integración

Regla clave: revisión cruzada obligatoria para evitar conocimiento aislado.

## 14.4 Asignación para tres desarrolladores

Ejemplo:

- **Desarrollador A**: dominio y casos de uso
- **Desarrollador B**: API, autenticación y autorización
- **Desarrollador C**: persistencia, pruebas de integración y DevOps

No deben formarse silos permanentes. Rotación parcial de revisión y tareas reduce riesgo de dependencia individual.

## 14.5 Gestión del backlog

Backlog recomendable:

- historias pequeñas
- criterios de aceptación concretos
- dependencias visibles
- priorización por riesgo y valor
- refinamiento semanal
- `Definition of Ready` antes de entrar al sprint

Buenas prácticas:

- dividir endpoints y reglas complejas en piezas verificables
- mantener trabajo en progreso bajo
- adelantar temas de concurrencia y seguridad, no dejarlos para cierre

## 14.6 Estrategia de ramas

Estrategia simple recomendada: **trunk-based development** con ramas cortas.

Ramas:

- `main`
- `feature/*`
- `fix/*`

Reglas:

- ramas de corta duración
- integración frecuente
- prohibidos commits directos a `main`
- pull request obligatorio

## 14.7 Revisión de código

Reglas mínimas:

- al menos un aprobador
- diffs pequeños siempre que sea posible
- revisión funcional y no solo de estilo

Checklist concreto:

- ¿cumple criterios de aceptación?
- ¿respeta reglas de dominio?
- ¿hay pruebas suficientes?
- ¿se cubren casos negativos?
- ¿consultas e índices siguen siendo correctos?
- ¿contratos HTTP cambiaron? ¿documentación actualizada?
- ¿migraciones nuevas son seguras y necesarias?
- ¿no hay secretos ni datos sensibles?
- ¿logs y errores son adecuados?

## 14.8 Definition of Done

Historia terminada cuando:

- cumple criterios de aceptación
- compila
- pruebas pasan
- casos negativos relevantes cubiertos
- documentación actualizada
- sin secretos en código
- logs razonables
- PR aprobado
- CI en verde
- cambio es desplegable

## 14.9 Estrategia de pruebas

Pirámide recomendada para este backend:

- base: pruebas de dominio
- medio: pruebas de integración con base real
- borde: validaciones de contrato HTTP y autorización

Coberturas prioritarias:

- reglas de dominio
- concurrencia
- autorización por rol y propiedad
- persistencia real
- regresión de flujos críticos

## 14.10 CI/CD

### Implementado actualmente

- restore
- build
- tests
- validación de Docker Compose
- build de imagen

### Recomendado para producción

- análisis estático adicional
- escaneo de dependencias e imagen
- despliegue a ambiente de prueba
- smoke tests post despliegue
- promoción controlada

## 14.11 Gestión de calidad

- convenciones de código acordadas
- uso de analizadores del ecosistema .NET cuando aplique
- revisión periódica de deuda técnica
- métricas útiles: tiempo de ciclo, defectos escapados, fallos de build, regresiones

No medir productividad por líneas de código.

## 14.12 Gestión de riesgos

Riesgos a vigilar:

- concurrencia en adjudicación
- migraciones en entornos compartidos
- cambios de contrato HTTP
- seguridad de secretos
- disponibilidad de MySQL
- crecimiento de historial
- desalineación entre capas

Tratamiento: identificar temprano, asignar responsable, revisar mitigación por sprint.

## 14.13 Comunicación

Cadencia recomendada:

- daily breve
- refinamiento
- planning
- review
- retrospectiva
- ADR para decisiones importantes
- canal claro para bloqueos

Objetivo: mantener decisiones visibles y reducir retrabajo.

## 14.14 Manejo de incidentes y defectos

Proceso recomendado:

- clasificar severidad
- reproducir caso
- corregir con alcance mínimo seguro
- agregar prueba de regresión
- realizar postmortem sin culpables cuando amerite

## 14.15 Incorporación de nuevos desarrolladores

Ruta de entrada sugerida:

- leer `README.md`
- levantar aplicación localmente
- revisar documentos de arquitectura
- sesión de pairing inicial
- primera tarea pequeña y segura
- acceso a ambientes y credenciales locales
- repaso de convenciones y pipeline
