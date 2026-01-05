using System;

namespace GhostSend.Domain.Entities;

public class StoredFile
{
    public Guid Id { get; private set; }
    public string NombreArchivo { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;

    public long Tamaño { get; private set; }
    public string RutaAlmacenamiento { get; private set; } = string.Empty;
    public string DeleteToken { get; private set; } = string.Empty;

    public DateTime FechaSubida { get; private set; }
    public DateTime? FechaExpiracion { get; private set; }
    public int? MaxDescargas { get; private set; }

    public int DescargasActuales { get; private set; }

    // constructor privado para Entity Framework
    private StoredFile() { }

    public StoredFile(string nombreArchivo, string contentType, long tamaño, DateTime fechaSubida, int? maxDescargas, TimeSpan? lifeTime)
    {
        Id = Guid.NewGuid();
        DeleteToken = Guid.NewGuid().ToString("N");

        NombreArchivo = nombreArchivo;
        ContentType = contentType;
        Tamaño = tamaño;
        FechaSubida = fechaSubida;

        DescargasActuales = 0;

        MaxDescargas = maxDescargas;

        if (lifeTime.HasValue)
        {
            FechaExpiracion = DateTime.UtcNow.Add(lifeTime.Value);
        }
    }

    public void SetRutaAlmacenamiento(string rutaAlmacenamiento)
    {
        if (string.IsNullOrWhiteSpace(rutaAlmacenamiento))
        {
            throw new ArgumentException("La ruta de almacenamiento no puede ser nula o vacía.");
        }

        RutaAlmacenamiento = rutaAlmacenamiento;
    }

    public void IncrementarDescargas()
    {
        DescargasActuales++;
    }

    public bool Expirado()
    {
        var tiempoDeExpiracion = FechaExpiracion.HasValue && DateTime.UtcNow > FechaExpiracion.Value;
        var descargasAgotadas = MaxDescargas.HasValue && DescargasActuales >= MaxDescargas.Value;

        return tiempoDeExpiracion || descargasAgotadas;
    }
}
