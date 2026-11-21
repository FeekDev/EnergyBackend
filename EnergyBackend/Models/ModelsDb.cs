using EnergyBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Casa> Casas { get; set; }
    public DbSet<Direccion> Direcciones { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Factura> Facturas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>()
            .HasIndex(usuario => usuario.CorreoElectronico)
            .IsUnique();

        modelBuilder.Entity<Casa>()
            .HasIndex(casa => new { casa.UsuarioId, casa.NumeroContrato })
            .IsUnique()
            .HasFilter("[NumeroContrato] IS NOT NULL");

        modelBuilder.Entity<Factura>()
            .HasOne(factura => factura.Usuario)
            .WithMany(usuario => usuario.Facturas)
            .HasForeignKey(factura => factura.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Usuario>()
            .HasMany(usuario => usuario.Casas)
            .WithOne(casa => casa.Usuario)
            .HasForeignKey(casa => casa.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Casa>()
            .HasOne(casa => casa.Direccion)
            .WithOne(direccion => direccion.Casa)
            .HasForeignKey<Direccion>(direccion => direccion.CasaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Casa>()
            .HasMany(casa => casa.Facturas)
            .WithOne(factura => factura.Casa)
            .HasForeignKey(factura => factura.CasaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Factura>()
            .Property(factura => factura.CasaId)
            .HasColumnName("idCasa");

        modelBuilder.Entity<Direccion>()
            .HasIndex(direccion => direccion.CasaId)
            .IsUnique()
            .HasDatabaseName("IX_Direcciones_idCasa");

        modelBuilder.Entity<Direccion>()
            .Property(direccion => direccion.CasaId)
            .HasColumnName("idCasa");

        modelBuilder.Entity<Factura>()
            .Property(factura => factura.ConsumoAcueductoM3)
            .HasPrecision(18, 3);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.ConsumoAlcantarilladoM3)
            .HasPrecision(18, 3);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.ConsumoEnergiaKwh)
            .HasPrecision(18, 3);
        modelBuilder.Entity<Factura>()
            .Property(factura => factura.ConsumoGasM3)
            .HasPrecision(18, 3);
    }
}