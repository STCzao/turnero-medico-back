using Microsoft.EntityFrameworkCore;
using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Paciente> Pacientes { get; set; }
        public DbSet<Doctor> Doctores { get; set; }
        public DbSet<Turno> Turnos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configuracion de relaciones
            modelBuilder.Entity<Turno>()
            .HasOne(t => t.Paciente)
            .WithMany(p => p.Turnos)
            .HasForeignKey(t => t.PacienteId)
            .OnDelete(DeleteBehavior.Restrict); //No permite eliminar paciente si tiene turnos

            modelBuilder.Entity<Turno>()
            .HasOne(t => t.Doctor)
            .WithMany(d => d.Turnos)
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.Restrict); //No permite eliminar doctor si tiene turnos

            //Configuracion de indices unicos
            modelBuilder.Entity<Paciente>()
            .HasIndex(p => p.Dni)
            .IsUnique();

            modelBuilder.Entity<Doctor>()
            .HasIndex(d => d.Matricula)
            .IsUnique();
        }
    }
}