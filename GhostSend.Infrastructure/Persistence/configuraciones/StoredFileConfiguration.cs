namespace GhostSend.Infrastructure.Persistence.Configuraciones;

using GhostSend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("stored_files");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NombreArchivo)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.RutaAlmacenamiento)
            .IsRequired();

        builder.Property(x => x.DeleteToken)
            .IsRequired();

        builder.HasIndex(x => x.FechaExpiracion);
    }
}
