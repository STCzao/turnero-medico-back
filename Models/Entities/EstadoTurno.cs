namespace turnero_medico_backend.Models.Entities
{
    // Constantes para los estados posibles de un Turno.
    // Centraliza los valores para eliminar magic strings dispersos en el código.
    public static class EstadoTurno
    {
        // Solicitud creada por el paciente, esperando que la secretaria asigne fecha y confirme.
        public const string SolicitudPendiente = "SolicitudPendiente";

        // Secretaria asignó fecha/hora, verificó cobertura y aprobó el turno.
        public const string Confirmado = "Confirmado";

        // Secretaria rechazó la solicitud (motivo obligatorio).
        public const string Rechazado = "Rechazado";

        // Doctor marcó la consulta como realizada.
        public const string Completado = "Completado";

        // Paciente no se presentó al turno.
        public const string Ausente = "Ausente";

        // Cancelado por paciente, secretaria, doctor o admin.
        public const string Cancelado = "Cancelado";

        // Todos los valores válidos para usar en validaciones.
        public static readonly IReadOnlyList<string> Todos =
        [
            SolicitudPendiente,
            Confirmado,
            Rechazado,
            Completado,
            Ausente,
            Cancelado
        ];

        // ─────────────────────────────────────────────────────────────
        // MÁQUINA DE ESTADOS — transiciones válidas
        // ─────────────────────────────────────────────────────────────

        private static readonly Dictionary<string, HashSet<string>> TransicionesValidas = new()
        {
            [SolicitudPendiente] = [Confirmado, Rechazado, Cancelado],
            [Confirmado]         = [Completado, Ausente, Cancelado],
            [Rechazado]          = [],
            [Completado]         = [],
            [Ausente]            = [],
            [Cancelado]          = [],
        };

        /// <summary>
        /// Valida que la transición de estado sea permitida.
        /// Lanza InvalidOperationException si no lo es.
        /// </summary>
        public static void ValidarTransicion(string estadoActual, string nuevoEstado)
        {
            if (!TransicionesValidas.TryGetValue(estadoActual, out var permitidos))
                throw new InvalidOperationException($"Estado actual '{estadoActual}' no es reconocido.");

            if (!permitidos.Contains(nuevoEstado))
                throw new InvalidOperationException(
                    $"No se puede pasar de '{estadoActual}' a '{nuevoEstado}'. " +
                    $"Transiciones permitidas: {(permitidos.Count > 0 ? string.Join(", ", permitidos) : "ninguna (estado final)")}.");
        }

        public static bool EsEstadoFinal(string estado)
            => TransicionesValidas.TryGetValue(estado, out var permitidos) && permitidos.Count == 0;
    }
}
