# Resumen de correcciones de logica

Fecha: 2026-02-22

## Objetivo
- Eliminar desincronizaciones entre metadata en BD y archivos fisicos usando transacciones y compensaciones.
- Alinear el flujo de borrado con la regla: archivos expirados pasan a ser responsabilidad del sistema.
- Ajustar pruebas unitarias al nuevo flujo transaccional.

## Cambios aplicados
- `GhostSend.Domain\Interfaces\IUnitOfWork.cs`
  - Se agrego `ExecuteInTransactionAsync(...)` para ejecutar operaciones de BD de forma atomica.

- `GhostSend.Infrastructure\Persistence\ApplicationDbContext.cs`
  - Implementacion de `ExecuteInTransactionAsync(...)` con commit/rollback explicitos.

- `GhostSend.Application\Files\Commands\UploadFile\UploadFileCommandHandler.cs`
  - Persistencia de metadata dentro de transaccion.
  - Compensacion: si falla la BD despues de guardar en storage, se intenta borrar el archivo (best effort).

- `GhostSend.Domain\Entities\StoredFile.cs`
  - Nuevo metodo `MarkExpired()` para marcar expiracion manual.

- `GhostSend.Application\Files\Commands\DeleteFile\DeleteTokenCommandHandler.cs`
  - El borrado con token ahora marca el archivo como expirado y deja la eliminacion fisica al sistema (worker).
  - La actualizacion se hace dentro de transaccion.

- `GhostSend.UnitTests\Application\UploadFileCommandHandlerTests.cs`
  - Ajustes de mocks para `ExecuteInTransactionAsync(...)` y nuevas verificaciones asociadas.

## Comportamiento resultante
- Upload
  - Archivo se guarda en storage.
  - Metadata se guarda en BD dentro de transaccion.
  - Si falla la BD, se intenta borrar el archivo para evitar orfandad.

- Delete con token
  - Si el archivo no esta expirado y el token es valido, se marca como expirado.
  - La limpieza fisica queda a cargo del `FileCleanWorker`.

## Pendientes conocidos
- Tests no ejecutados en este entorno por fallos de restore. Ver `testlog.txt`.

