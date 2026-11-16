# Lesson 06 — Requisitos y evidencia

Fecha: 2026-11-16. Rama: `lesson-06`. Commit: `Lesson 06: Implemented and Verified`.

| Requisito | Implementación y verificación | Estado |
| --- | --- | --- |
| UC-005, FR-EVT-007, BR-EVT-002 | Publish: propietario, Draft y Start > reloj. Casos válidos, estados rechazados, igualdad y ±1 segundo; estado SQL comparado. | VERIFIED FOR LESSON SLICE |
| FR-EVT-008, BR-EVT-005, FR-SRH-001–002/009 | Index heredado sin cambios; prueba antes/después Publish/Cancel con search, Category y pageSize 25; suite 10/25/50, orden y filtros retenida. | VERIFIED FOR LESSON SLICE |
| FR-EVT-009, BR-EVT-006 | Cancel: Published propio antes de Start; casos Draft, Cancelled, igualdad y pasado sin cambios. | VERIFIED FOR LESSON SLICE |
| FR-EVT-010, FR-SRH-008, BR-EVT-007 | Cancelled conserva fila y Details 200 con aviso; excluido de catálogo; Publish rechazado; Draft/missing Details 404. | VERIFIED FOR LESSON SLICE |
| FR-EVT-011, BR-EVT-008 | Delete GET read-only, POST protegido, rechazos por estado/propiedad. Keep Event conserva la fila. | PARTIAL: conteo de inscripciones pendiente de Lesson 07. |
| FR-EVT-004/006, BR-EVT-009–010 | Edit Draft/Published valida campos y Category, protege metadatos y rechaza Cancelled. | PARTIAL: mínimo de capacidad pendiente de Lesson 07. |
| FR-EVT-005, FR-ACC-006, BR-AUT-001–002/004 | Manage propio y comprobaciones por acción, cookie Identity real y pruebas de IDs ajenos. | PARTIAL: autoridades adicionales pertenecen a siguientes lecciones. |
| NFR-SEC-001/003 | Anónimo/Participant rechazados; POST sin token rechazado; cuatro ataques de ID ajeno no modifican filas. | VERIFIED FOR LESSON SLICE |
| NFR-USA-003–004, NFR-ACC-001–003 | Edge: confirmación explícita, Keep Event por teclado, foco visible, región de advertencia identificada; feedback status/alert; 390 px sin desbordamiento en Manage. | VERIFIED FOR LESSON SLICE; sin prueba auditiva con lector de pantalla. |
| NFR-REL-001–002, AC-003/005/009–010 | Pruebas HTTP y contextos SQL frescos; compilación y suite acumulativa. | PARTIAL: confiabilidad de inscripciones pendiente de Lesson 07. |

La revisión manual utiliza `CemsL05Practice_20261116`, separada de la base normal. El Draft ID 7 se conservó tras abrir confirmación y seleccionar Keep Event, pasó de Draft a Published y luego Cancelled; Details mostró el aviso y Edit redirigió con error. La prueba automatizada comprueba borrado efectivo con datos aislados.

Las pruebas usan las migraciones reales y bases SQL Server con GUID del fixture heredado. No se añade migración en esta lección. Los ejercicios y límites de la evidencia están en `ejercicios-leccion-06.md`.

Resultado: build correcto, cero errores y advertencias; suite acumulativa de 81 casos aprobados, cero fallidos u omitidos (5 min 47 s). Filtro del ejercicio temporal: 3 casos aprobados. git diff --check sin errores de espacios.
