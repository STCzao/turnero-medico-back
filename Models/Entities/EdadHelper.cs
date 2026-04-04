namespace turnero_medico_backend.Models.Entities
{
    /// <summary>
    /// Centraliza el cálculo de edad y mayoría de edad para evitar duplicación.
    /// </summary>
    public static class EdadHelper
    {
        public static int CalcularEdad(DateTime fechaNacimiento)
        {
            var hoy = DateTime.UtcNow;
            var edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento > hoy.AddYears(-edad)) edad--;
            return edad;
        }

        public static bool EsMayorDeEdad(DateTime fechaNacimiento)
            => CalcularEdad(fechaNacimiento) >= 18;
    }
}
