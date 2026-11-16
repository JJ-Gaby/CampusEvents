# Lesson 05 — Requisitos y evidencia

Ejecución: 2026-11-16. Rama: `lesson-05`. Checkpoint: `Lesson 05: Implemented and Verified`.

| Requisito | Implementación | Procedimiento y resultado observado | Estado |
| --- | --- | --- | --- |
| UC-005, FR-EVT-001–003 | `EventsController.Create`, `EventInputModel`, `Create.cshtml` | POST válido redirige a Manage. Consulta SQL confirma propietario autenticado, Draft y fecha del servidor; los extras enviados no controlan esos campos. | PARTIAL: creación de Draft; gestión adicional pendiente de próximas lecciones. |
| FR-EVT-005, BR-AUT-002 | `Manage`, `Manage.cshtml` | Prueba HTTP con cuentas Identity reales: anónimo recibe Login, Participant denegado, Organizer ve únicamente sus eventos de todos los estados. | IMPLEMENTED—VERIFIED para este checkpoint. |
| FR-EVT-004, BR-EVT-001, BR-EVT-003–004 | Atributos e `IValidatableObject` de `EventInputModel`; `ValidateCategoryAsync` | Required/espacios, End anterior o igual a Start, capacidad inválida y Category inexistente muestran errores sin cambiar filas. Opciones y valores válidos se conservan. | PARTIAL: validación de Create/Edit Draft. |
| FR-EVT-006, BR-AUT-004 | Edit GET/POST | Draft propio se edita conservando propietario, estado y CreatedAt. GET/POST de otro propietario, Published o Cancelled devuelve 404 sin mutación; comparación de campos antes/después. | PARTIAL: edición de Draft propio; alcance del ciclo de vida pendiente. |
| NFR-SEC-003, NFR-REL-001–002 | Antiforgery, modelo restringido, consulta y asignaciones protegidas | POST sin token rechazado; escrituras válidas persistidas; rechazos sin cambios. Pruebas SQL aisladas y consulta del Draft manual. | IMPLEMENTED—VERIFIED para los escenarios de la lección; obligación continua. |
| NFR-USA-002, NFR-ACC-001–003, NFR-COM-002 | Manage/Create/Edit y `site.css` | Inspección en Edge en escritorio y a 390 × 844: sin desbordamiento horizontal, etiquetas visibles y Location legible. Tab llega al botón con contorno sólido; Shift+Tab vuelve al control. Error de cronología aparece en resumen y campo; valores y Category retenidos. Hoja compartida cargada. | IMPLEMENTED—VERIFIED en las vistas y tamaños inspeccionados. |
| FR-SRH-009, FR-ACC-001–007, AC-010 | Catálogo, Identity y suite acumulativa | Se conservan pruebas de búsqueda, filtros, paginación, Details, registro, Login, Logout y Participant. Navegación Organizer visible al iniciar sesión; catálogo permanece accesible. | PARTIAL: contratos heredados conservados; alcance SRS posterior pendiente. |

## Verificación

- `dotnet build CampusEvents.slnx --no-restore`: correcto, cero errores y advertencias.
- `dotnet test CampusEvents.slnx --no-build`: 54 aprobadas, cero fallidas u omitidas (3 min 32 s), con SQL Server LocalDB; incluye R1–R5 y los dos ejercicios adicionales.
- `dotnet ef migrations has-pending-model-changes --project src/CampusEvents --startup-project src/CampusEvents -- --environment Development`: sin cambios pendientes. No se añade migración.
- La revisión manual utilizó `CemsL05Practice_20261116`, separada de `CampusEventsDb`. Las pruebas automatizadas crean y eliminan sus propias bases con GUID.
- Create inválido con horas iguales mostró el error y conservó Title, Description, Location, Category y Capacity. Tras corregir End, Manage mostró el nuevo Draft.
- Edit inválido mostró el título intentado, pero SQL conservó `Lesson 05 Browser Draft`. Edit válido guardó `Lesson 05 Updated Draft`; SQL mantuvo `seed-organizer-001`, estado `0` (Draft) y CreatedAt. Tras refrescar Manage, apareció una sola coincidencia; el total quedó en siete eventos, frente a seis iniciales.

Los escenarios de propietario ajeno, estados no elegibles, Participant y manipulación se verifican mediante HTTP y consultas a SQL Server en `OrganizerEventHttpTests`.
