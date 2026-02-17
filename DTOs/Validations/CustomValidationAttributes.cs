using System.ComponentModel.DataAnnotations;

namespace turnero_medico_backend.DTOs.Validations
{
    // Atributo personalizado para validar que la fecha de nacimiento cumpla con una edad mínima requerida

    [AttributeUsage(AttributeTargets.Property)]
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
            ErrorMessage = $"Debe ser mayor de {minimumAge} años";
        }

        public override bool IsValid(object? value)
        {
            if (value is not DateTime birthDate)
                return false;

            var age = DateTime.UtcNow.Year - birthDate.Year;
            if (birthDate.Date > DateTime.UtcNow.AddYears(-age))
                age--;

            return age >= _minimumAge;
        }
    }

    // Atributo personalizado para validar que una fecha no esté en el pasado

    [AttributeUsage(AttributeTargets.Property)]
    public class FutureOrTodayAttribute : ValidationAttribute
    {
        public FutureOrTodayAttribute()
        {
            ErrorMessage = "La fecha debe ser hoy o en el futuro";
        }

        public override bool IsValid(object? value)
        {
            if (value is not DateTime dateTime)
                return false;

            return dateTime.Date >= DateTime.UtcNow.Date;
        }
    }

    // Atributo personalizado para validar que una fecha no esta más allá de una cantidad máxima de días en el futuro

    [AttributeUsage(AttributeTargets.Property)]
    public class MaximumFutureDateAttribute : ValidationAttribute
    {
        private readonly int _maximumDaysInFuture;

        public MaximumFutureDateAttribute(int maximumDaysInFuture)
        {
            _maximumDaysInFuture = maximumDaysInFuture;
            ErrorMessage = $"La fecha no puede estar más allá de {maximumDaysInFuture} días en el futuro";
        }

        public override bool IsValid(object? value)
        {
            if (value is not DateTime dateTime)
                return false;

            var maxDate = DateTime.UtcNow.AddDays(_maximumDaysInFuture);
            return dateTime <= maxDate;
        }
    }
}
