using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Data
{
    // Contexto de base de datos principal.
    // Hereda de IdentityDbContext para incluir automáticamente las tablas de ASP.NET Identity
    // (AspNetUsers, AspNetRoles, AspNetUserClaims, etc.) además de las tablas de dominio.
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
    {
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Doctor> Doctores { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<ObraSocial> ObrasSociales { get; set; }
        public DbSet<Horario> Horarios { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

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

            // Relación: Turno-Doctor (DoctorId nullable: el paciente puede no elegir doctor)
            modelBuilder.Entity<Turno>()
            .HasOne(t => t.Doctor)
            .WithMany(d => d.Turnos)
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

            // Relación: Turno-Especialidad (muchos-a-uno)
            modelBuilder.Entity<Turno>()
                .HasOne(t => t.Especialidad)
                .WithMany()
                .HasForeignKey(t => t.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict);

            // Doctor → Especialidad (muchos-a-uno)
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.Especialidad)
                .WithMany(e => e.Doctores)
                .HasForeignKey(d => d.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict);

            // ObraSocial ↔ Especialidad (muchos-a-muchos)
            modelBuilder.Entity<ObraSocial>()
                .HasMany(o => o.Especialidades)
                .WithMany(e => e.ObrasSociales)
                .UsingEntity(j => j.ToTable("ObraSocialEspecialidad"));

            // ObraSocial: planes como JSONB
            modelBuilder.Entity<ObraSocial>()
                .Property(o => o.Planes)
                .HasColumnType("jsonb");

            // Turno: concurrencia optimista
            modelBuilder.Entity<Turno>()
                .Property(t => t.RowVersion)
                .IsRowVersion();

            // Índices únicos
            modelBuilder.Entity<Paciente>()
            .HasIndex(p => p.Dni)
            .IsUnique();

            modelBuilder.Entity<Doctor>()
            .HasIndex(d => d.Matricula)
            .IsUnique();

            // Relación: Horario-Doctor (Muchos-a-Uno)
            modelBuilder.Entity<Horario>()
                .HasOne(h => h.Doctor)
                .WithMany(d => d.Horarios)
                .HasForeignKey(h => h.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Paciente.ResponsableId → AspNetUsers (dependiente vinculado a su responsable)
            modelBuilder.Entity<Paciente>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.ResponsableId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Turno.CreatedByUserId → AspNetUsers (quién solicitó el turno)
            modelBuilder.Entity<Turno>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Turno.ConfirmadaPorId → AspNetUsers (secretaria/admin que gestionó)
            modelBuilder.Entity<Turno>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.ConfirmadaPorId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            // Índices para queries frecuentes
            modelBuilder.Entity<Turno>()
                .HasIndex(t => t.Estado);

            modelBuilder.Entity<Turno>()
                .HasIndex(t => t.PacienteId);

            modelBuilder.Entity<Turno>()
                .HasIndex(t => t.DoctorId);

            modelBuilder.Entity<Doctor>()
                .HasIndex(d => d.UserId);

            modelBuilder.Entity<Paciente>()
                .HasIndex(p => p.UserId);

            modelBuilder.Entity<Paciente>()
                .HasIndex(p => p.ResponsableId);

            // AuditLog: índices para consultas por usuario y por fecha
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.UserId);
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.Entidad, a.EntidadId });
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.FechaHora);

            modelBuilder.Entity<Horario>()
                .HasIndex(h => new { h.DoctorId, h.DiaSemana });
        }
    }
}