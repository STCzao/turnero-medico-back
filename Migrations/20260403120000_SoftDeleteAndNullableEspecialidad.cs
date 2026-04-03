using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using turnero_medico_backend.Data;

#nullable disable

namespace turnero_medico_backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260403120000_SoftDeleteAndNullableEspecialidad")]
    public partial class SoftDeleteAndNullableEspecialidad : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Soft Delete: Doctor ──────────────────────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""Doctores""
                    ADD COLUMN IF NOT EXISTS ""IsDeleted"" boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ""DeletedAt"" timestamp with time zone NULL;
            ");

            // ── Soft Delete: Secretaria ──────────────────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""Secretarias""
                    ADD COLUMN IF NOT EXISTS ""IsDeleted"" boolean NOT NULL DEFAULT false,
                    ADD COLUMN IF NOT EXISTS ""DeletedAt"" timestamp with time zone NULL;
            ");

            // ── EspecialidadId nullable: Doctor ──────────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""Doctores""
                    ALTER COLUMN ""EspecialidadId"" DROP NOT NULL;
            ");

            // ── EspecialidadId nullable: Turno ───────────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""Turnos""
                    ALTER COLUMN ""EspecialidadId"" DROP NOT NULL;
            ");

            // ── FK Doctor → Especialidad: Restrict → SetNull ─────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""Doctores""
                    DROP CONSTRAINT IF EXISTS ""FK_Doctores_Especialidades_EspecialidadId"";
                ALTER TABLE ""Doctores""
                    ADD CONSTRAINT ""FK_Doctores_Especialidades_EspecialidadId""
                    FOREIGN KEY (""EspecialidadId"")
                    REFERENCES ""Especialidades"" (""Id"")
                    ON DELETE SET NULL;
            ");

            // ── FK Turno → Especialidad: Restrict → SetNull ──────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""Turnos""
                    DROP CONSTRAINT IF EXISTS ""FK_Turnos_Especialidades_EspecialidadId"";
                ALTER TABLE ""Turnos""
                    ADD CONSTRAINT ""FK_Turnos_Especialidades_EspecialidadId""
                    FOREIGN KEY (""EspecialidadId"")
                    REFERENCES ""Especialidades"" (""Id"")
                    ON DELETE SET NULL;
            ");

            // ── FK Turno → Doctor: Restrict → SetNull ────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""Turnos""
                    DROP CONSTRAINT IF EXISTS ""FK_Turnos_Doctores_DoctorId"";
                ALTER TABLE ""Turnos""
                    ADD CONSTRAINT ""FK_Turnos_Doctores_DoctorId""
                    FOREIGN KEY (""DoctorId"")
                    REFERENCES ""Doctores"" (""Id"")
                    ON DELETE SET NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Doctores"" DROP COLUMN IF EXISTS ""IsDeleted"", DROP COLUMN IF EXISTS ""DeletedAt"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Secretarias"" DROP COLUMN IF EXISTS ""IsDeleted"", DROP COLUMN IF EXISTS ""DeletedAt"";");

            migrationBuilder.Sql(@"ALTER TABLE ""Doctores"" ALTER COLUMN ""EspecialidadId"" SET NOT NULL;");
            migrationBuilder.Sql(@"ALTER TABLE ""Turnos"" ALTER COLUMN ""EspecialidadId"" SET NOT NULL;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Doctores""
                    DROP CONSTRAINT IF EXISTS ""FK_Doctores_Especialidades_EspecialidadId"";
                ALTER TABLE ""Doctores""
                    ADD CONSTRAINT ""FK_Doctores_Especialidades_EspecialidadId""
                    FOREIGN KEY (""EspecialidadId"") REFERENCES ""Especialidades"" (""Id"") ON DELETE RESTRICT;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Turnos""
                    DROP CONSTRAINT IF EXISTS ""FK_Turnos_Especialidades_EspecialidadId"";
                ALTER TABLE ""Turnos""
                    ADD CONSTRAINT ""FK_Turnos_Especialidades_EspecialidadId""
                    FOREIGN KEY (""EspecialidadId"") REFERENCES ""Especialidades"" (""Id"") ON DELETE RESTRICT;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Turnos""
                    DROP CONSTRAINT IF EXISTS ""FK_Turnos_Doctores_DoctorId"";
                ALTER TABLE ""Turnos""
                    ADD CONSTRAINT ""FK_Turnos_Doctores_DoctorId""
                    FOREIGN KEY (""DoctorId"") REFERENCES ""Doctores"" (""Id"") ON DELETE RESTRICT;
            ");
        }
    }
}
