namespace turnero_medico_backend.Services
{
    internal static class DiaSemanaHelper
    {
        private static readonly string[] Nombres =
            ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

        internal static string ToString(int dia) =>
            dia >= 0 && dia <= 6 ? Nombres[dia] : "Desconocido";
    }
}
