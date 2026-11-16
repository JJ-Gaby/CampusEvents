# Lesson 07 — Requisitos y evidencia

Fecha: 2026-11-16. Rama: `lesson-07`. Checkpoint: `Lesson 07: Implemented and Verified`.

| Referencia | Implementación | Verificación y resultado | Estado |
| --- | --- | --- | --- |
| UC-003, FR-REG-001–005, BR-REG-001–004 | Registration, RegistrationService y Register protegido | Registro válido y timestamp; duplicado, Draft, Cancelled, igualdad al inicio, token ausente, cupo lleno y último cupo con contextos independientes. Consultas SQL de filas y count ≤ Capacity. | VERIFIED FOR LESSON SLICE. |
| UC-004, FR-REG-006–008, BR-REG-005–006 | Mine propio y Cancel por usuario y tiempo | Cancel ajeno 404; propio elimina; igualdad/pasado conserva; Cancelled futuro permite cancelar. Browser: Mine muestra la inscripción y luego confirma su cancelación. | VERIFIED FOR LESSON SLICE. |
| FR-SRH-004/008 | EventDetailsViewModel y conteos persistidos | Modelo directo con count 1 y remaining 1; HTTP y navegador muestran disponibilidad y estado propio. Predicado Published/Cancelled conservado; Draft/missing 404. | VERIFIED FOR LESSON SLICE. |
| FR-EVT-011, BR-EVT-008 | DeleteConfirmed transaccional | Draft con inscripción conserva Event y Registration, con alert de rechazo. Borrado válido y propietario ajeno mantienen regresión. | VERIFIED FOR LESSON SLICE. |
| FR-EVT-004/006, BR-EVT-009 | Edit con lock y conteo actual | Reducción por debajo de count rechazada sin cambios; órdenes Register/Edit y competencia mantienen count ≤ Capacity. Owner/status/CreatedAt protegidos. | VERIFIED FOR LESSON SLICE. |
| NFR-REL-001–003, AC-005/009 | SQL Server, índices, FK y coordinación | Índice único rechaza duplicado; FK rechaza Event inexistente; experimento de dos sesiones registra bloqueo real; pruebas concurrentes comparan filas finales. | PARTIAL: evidencia de este flujo, no garantía de todos los escenarios operativos del SRS. |
| AC-004 | Register → Mine → Cancel | Edge y SQL: disponibilidad 0/2 → 1/1 → 0/2; mensajes y ausencia final de inscripción propia. | VERIFIED FOR LESSON SLICE. |
| AC-010 | Suite acumulativa y filtros | Pruebas heredadas de cuentas, catálogo, formularios y ciclo de vida, más casos de inscripción; resultados registrados abajo. | PARTIAL: aceptación crítica del alcance acumulado. |
| FR-ACC-006 | Autorización Participant/Organizer | HTTP exige autenticación, antiforgery y propiedad; roles elevados conservan capacidad Participant. | PARTIAL: alcance Administrator adicional pendiente de otra lección. |

## Entorno y observación manual

SQL Server LocalDB `(localdb)\MSSQLLocalDB`; pruebas sobre bases GUID migradas con el fixture heredado, nunca DefaultConnection. Reloj de pruebas explícito UTC−04, fijo en 2026-04-15 12:00 local. Las horas manuales usan el TimeProvider normal de Development.

Se aplicó la migración revisada a `CampusEventsDb`: conserva las seis filas Event originales y crea Registrations inicialmente vacía. Las mutaciones de inspección se realizaron en `CemsL05Practice_20261116`, base privada con Event 1 Published, Start 2026-12-16 18:00 y Capacity 2; no se cambió la capacidad de la base normal.

En Edge se utilizó la cuenta de prueba owner para ejercer capacidades Participant: Details comenzó con count 0/remaining 2; Register por teclado mostró confirmación y count 1/remaining 1. Mine mostró el evento, Cancel confirmó la eliminación y Details volvió a 0/2. Logout mantuvo Details público con Login to Register y returnUrl local. A 390 × 844, Details y Mine no excedieron el ancho disponible; controles con nombres visibles, foco y formularios POST. No se ejecutó lector de pantalla.

## Bloqueo observado con dos sesiones

Base privada, EventId 1, Published futuro, Capacity 2, count inicial 0. La sesión SQL 75 abrió transacción y adquirió UPDLOCK/HOLDLOCK del Event. Un cliente HTTP distinto, autenticado con cuenta de prueba y token válido, envió Register. La sesión SQL 59 quedó esperando a 75 con `LCK_M_U`; el POST permanecía pendiente. Tras COMMIT en la conexión 75, el POST respondió 302 a `/Events/Details/1`. Consulta posterior: Capacity 2, count 1. La conexión y transacción se cerraron; no quedó bloqueo retenido. Esto evidencia espera real, además del test de solicitudes liberadas juntas.

Los escenarios de más usuarios, duplicado/full, propiedad, límites temporales y escrituras competidoras se comprueban mediante HTTP y contextos SQL frescos en la suite; no se presentan como observaciones manuales de navegador.

## Ejecuciones

`dotnet test --no-build --list-tests` descubrió 60 métodos y 102 casos, contando cada InlineData por separado. El filtro `FullyQualifiedName~RegistrationServiceTests` aprobó sus siete casos, sin fallos ni omisiones (34 s), incluidos Cancelled, igualdad de fechas, índices/FK y último cupo. La compilación de la solución terminó con cero advertencias y errores. La suite acumulativa aprobó 102 casos, con cero fallos y cero omisiones (8 min 3 s). No hay diferencias pendientes entre el modelo EF y la última migración. La revisión staged y git diff --check quedaron sin errores.
