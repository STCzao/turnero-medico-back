namespace turnero_medico_backend.Models.Entities
{
    // Constantes para los estados posibles de un Turno.
    // Centraliza los valores para eliminar magic strings dispersos en el código.
    public static class EstadoTurno
    {
        // Turno creado, esperando confirmación o cobertura.
        public const string Pendiente = "Pendiente";

        // Turno confirmado por el consultorio.
        public const string Confirmado = "Confirmado";

        // Turno cancelado por el paciente o el doctor.
        public const string Cancelado = "Cancelado";

        // Consulta finalizada.
        public const string Completado = "Completado";

        // Obra social verificada; el turno está aprobado.
        public const string Aceptado = "Aceptado";

        // Cobertura rechazada por el doctor tras validación externa.
        public const string Rechazado = "Rechazado";

        // El doctor debe validar la cobertura en el sistema externo de la OS.
        public const string PendienteValidacionDoctor = "PendienteValidacionDoctor";

        // Todos los valores válidos para usar en validaciones.
        public static readonly IReadOnlyList<string> Todos =
        [
            Pendiente,
            Confirmado,
            Cancelado,
            Completado,
            Aceptado,
            Rechazado,
            PendienteValidacionDoctor
        ];
    }
}
