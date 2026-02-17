using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Data
{
    /// <summary>
    /// Contexto de base de datos que incluye:
    /// - Tablas de dominio: Pacientes, Doctores, Turnos
    /// - Tablas de Identity: AspNetUsers, AspNetRoles, AspNetUserRoles, etc.
    /// </summary>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
    {
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Doctor> Doctores { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<ObraSocial> ObrasSociales { get; set; }
        public DbSet<ObraSocialEspecialidad> ObrasSocialesEspecialidades { get; set; }  // ← NUEVA

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

            // ===== NUEVA: Relación ObraSocial-Especialidades (1-a-Muchos) =====
            modelBuilder.Entity<ObraSocialEspecialidad>()
                .HasOne(ose => ose.ObraSocial)
                .WithMany(os => os.Especialidades)
                .HasForeignKey(ose => ose.ObraSocialId)
                .OnDelete(DeleteBehavior.Cascade);  // Si se elimina OS, también especialidades

            // Índice único: una OS no puede tener dos veces la misma especialidad
            modelBuilder.Entity<ObraSocialEspecialidad>()
                .HasIndex(ose => new { ose.ObraSocialId, ose.Especialidad })
                .IsUnique();

            // ===== Índices Únicos =====
            modelBuilder.Entity<Paciente>()
            .HasIndex(p => p.Dni)
            .IsUnique();

            modelBuilder.Entity<Doctor>()
            .HasIndex(d => d.Matricula)
            .IsUnique();
        }
    }
}