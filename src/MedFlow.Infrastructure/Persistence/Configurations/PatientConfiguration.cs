using MedFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedFlow.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.Property(p => p.PrimerNombre).HasMaxLength(100).IsRequired();
        builder.Property(p => p.SegundoNombre).HasMaxLength(100);
        builder.Property(p => p.PrimerApellido).HasMaxLength(100).IsRequired();
        builder.Property(p => p.SegundoApellido).HasMaxLength(100);
        builder.Property(p => p.Sexo).HasMaxLength(20);
        builder.Property(p => p.TipoDocumento).HasMaxLength(30);
        builder.Property(p => p.NumeroDocumento).HasMaxLength(50);
        builder.Property(p => p.Telefono).HasMaxLength(30);
        builder.Property(p => p.Correo).HasMaxLength(256);
        builder.Property(p => p.Direccion).HasMaxLength(500);
        builder.Property(p => p.ContactoEmergenciaNombre).HasMaxLength(200);
        builder.Property(p => p.ContactoEmergenciaTelefono).HasMaxLength(30);
        builder.Property(p => p.Alergias).HasMaxLength(1000);
        builder.Property(p => p.Observaciones).HasMaxLength(4000);
        builder.Property(p => p.UserId).HasMaxLength(450);

        builder.Property(p => p.TenantId).IsRequired();
        builder.HasIndex(p => p.UserId).HasFilter("\"UserId\" IS NOT NULL");
        builder.HasIndex(p => p.TenantId);
        builder.HasIndex(p => new { p.TenantId, p.CreatedAt });
        builder.HasIndex(p => p.NumeroDocumento);
        builder.HasIndex(p => new { p.PrimerApellido, p.PrimerNombre });

        builder.HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
