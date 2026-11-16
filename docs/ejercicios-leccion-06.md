# Lesson 06 — Ejercicios

## Matriz de transiciones

En todas las operaciones se exige rol Organizer y propietario autenticado. Un ID ajeno o inexistente devuelve 404; las mutaciones requieren POST y antiforgery.

| Estado inicial | Publish | Cancel | Delete | Edit |
| --- | --- | --- | --- | --- |
| Draft | Published si Start > now; de otro modo rechazo sin cambios. | Rechazo: requiere Published. | Confirmación GET sin escritura; POST elimina. La regla cero Registrations queda pendiente de Lesson 07. | Guarda campos validados; mantiene owner, Status y CreatedAt. |
| Published | Rechazo: requiere Draft. | Cancelled si Start > now; conserva fila. | Rechazo: requiere Draft. | Guarda campos validados. El mínimo Capacity ≥ inscripciones queda pendiente de Lesson 07. |
| Cancelled | Rechazo: terminal. | Rechazo: requiere Published. | Rechazo: requiere Draft. | Redirección con error, sin cambios. |

`Publish_OwnedFutureDraft_UpdatesStateAndPublicDiscovery` compara presencia pública antes/después de Publish y Cancel y consulta el estado persistido. `Publish_RejectedStatesOrTimes_NeverWrite` compara todos los campos tras rechazar un estado o fecha no elegible.

## Límite temporal

`Publish_OneSecondBoundary_UsesStrictFutureRule` añade tres casos independientes: −1 segundo, exactamente now y +1 segundo. El filtro `dotnet test --no-build --filter FullyQualifiedName~Publish_OneSecondBoundary` ejecutó tres casos: tres aprobados, cero fallidos (31,47 s). Los dos primeros conservan la instantánea; el último cambia a Published. End se fija dos horas después de Start para mantener válida la entidad. El reloj es el UTC−04 fijo heredado.

## Propiedad falsificada

`Lifecycle_ForgedRouteAndFields_PreserveForeignEvent` obtiene cookie y token legítimos y envía Publish, Cancel, DeleteConfirmed y Edit contra otro propietario; cada caso devuelve 404 y conserva la instantánea SQL. Cancel utiliza un Published; los demás un Draft. Edit incluye OrganizerId y Status falsificados. Antiforgery demuestra origen de solicitud, no autorización sobre el recurso.

## Feedback y accesibilidad

La advertencia de borrado inicialmente era un párrafo sin asociación con su panel. El cambio mínimo añade `role="region"`, `aria-labelledby="deletion-warning"` e ID al texto existente. El árbol de accesibilidad ahora identifica la región mediante la advertencia completa; no cambia los controles ni las reglas del servidor.

En Edge se recorrieron los controles con teclado: Tab desde el botón destructivo alcanza Keep Event con contorno visible; Enter vuelve a Manage y conserva el Draft. El rechazo de Edit Cancelled redirige con `role="alert"`. A 390 × 844, advertencia y acciones siguen legibles y Manage no presenta desbordamiento horizontal. No se ejecutó un lector de pantalla: se comprobó la semántica expuesta, sin afirmar un anuncio auditivo.

## Invariantes dependientes de datos

Una Registration persistida debe vincular UserId con ApplicationUser y EventId con Event y registrar CreatedAt. El conteo relevante es el de asociaciones actuales para el EventId, obtenido del servidor.

- Draft con N > 0: no eliminar. Draft con N = 0: eliminación posible tras confirmación y comprobación de propietario/estado.
- Published con N inscripciones: Capacity nueva debe ser ≥ N. Con N = 3, Capacity = 3 es válida y Capacity = 2 se rechaza sin guardar otros campos.

La lectura de estado, conteo y posterior modificación deben compartir una transacción y coordinación sobre el mismo Event con cualquier escritor que aumente el conteo o reduzca Capacity. Las FK protegen las relaciones, pero no sustituyen este límite agregado. Especificación de diseño en Lesson 06; implementación corresponde a Lesson 07.
