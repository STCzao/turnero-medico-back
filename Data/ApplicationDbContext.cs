using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Data
{
    /// <>
    /// Contexto de base de datos que incluye:
    /// Tablas de dominio: Pacientes, Doctores, Turnos
    /// Tablas de Identity: AspNetUsers, AspNetRoles, AspNetUserRoles, etc.
    /// </>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
    {
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Doctor> Doctores { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<ObraSocial> ObrasSociales { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // ← Configura las tablas de Identity

            // Configuración de relaciones de dominio
            
            // Relación: Paciente-ObraSocial (1-a-Muchos)
            modelBuilder.Entity<Paciente>()
                .HasOne(p => p.ObraSocial)
                .WithMany(o => o.Pacientes)
                .HasForeignKey(p => p.ObraSocialId)
                .OnDelete(DeleteBehavior.SetNull);  // Si se elimina OS, Paciente queda sin OS
            
            // Relación: Turno-ObraSocial (Muchos-a-Uno)
            modelBuilder.Entity<Turno>()
                .HasOne(t => t.ObraSocial)
                .WithMany()
                .HasForeignKey(t => t.ObraSocialId)
                .OnDelete(DeleteBehavior.SetNull);  // Si se elimina OS, Turno queda sin OS
            
            // Relación: Turno-Paciente (ya existe)
            modelBuilder.Entity<Turno>()
            .HasOne(t => t.Paciente)
            .WithMany(p => p.Turnos)
            .HasForeignKey(t => t.PacienteId)
            .OnDelete(DeleteBehavior.Restrict);

            // Relación: Turno-Doctor (ya existe)
            modelBuilder.Entity<Turno>()
            .HasOne(t => t.Doctor)
            .WithMany(d => d.Turnos)
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

            // ObraSocial: especialidades como columna JSONB
            modelBuilder.Entity<ObraSocial>()
                .Property(o => o.Especialidades)
                .HasColumnType("jsonb");

            // Índices únicos
            modelBuilder.Entity<Paciente>()
            .HasIndex(p => p.Dni)
            .IsUnique();

            modelBuilder.Entity<Doctor>()
            .HasIndex(d => d.Matricula)
            .IsUnique();
        }
    }
}