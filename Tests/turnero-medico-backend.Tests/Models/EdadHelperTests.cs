using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Tests.Models
{
    public class EdadHelperTests
    {
        [Fact]
        public void CalcularEdad_CumplioAnios_DevuelveEdadCorrecta()
        {
            // Alguien nacido hace exactamente 25 años (ya cumplió)
            var nacimiento = DateTime.UtcNow.AddYears(-25).AddDays(-10);
            Assert.Equal(25, EdadHelper.CalcularEdad(nacimiento));
        }

        [Fact]
        public void CalcularEdad_AunNoCumplioEsteAnio_DevuelveEdadMenosUno()
        {
            // Nació hace 25 años calendario pero aún no llegó su cumpleaños
            var nacimiento = DateTime.UtcNow.AddYears(-25).AddDays(10);
            Assert.Equal(24, EdadHelper.CalcularEdad(nacimiento));
        }

        [Fact]
        public void CalcularEdad_NacidoHoy_DevuelveCero()
        {
            Assert.Equal(0, EdadHelper.CalcularEdad(DateTime.UtcNow));
        }

        [Fact]
        public void EsMayorDeEdad_18Cumplidos_DevuelveTrue()
        {
            var nacimiento = DateTime.UtcNow.AddYears(-18).AddDays(-1);
            Assert.True(EdadHelper.EsMayorDeEdad(nacimiento));
        }

        [Fact]
        public void EsMayorDeEdad_17Anios_DevuelveFalse()
        {
            var nacimiento = DateTime.UtcNow.AddYears(-17);
            Assert.False(EdadHelper.EsMayorDeEdad(nacimiento));
        }

        [Fact]
        public void EsMayorDeEdad_CumpleHoy18_DevuelveTrue()
        {
            var nacimiento = DateTime.UtcNow.AddYears(-18);
            Assert.True(EdadHelper.EsMayorDeEdad(nacimiento));
        }

        [Fact]
        public void EsMayorDeEdad_FaltaUnDiaPara18_DevuelveFalse()
        {
            var nacimiento = DateTime.UtcNow.AddYears(-18).AddDays(1);
            Assert.False(EdadHelper.EsMayorDeEdad(nacimiento));
        }
    }
}
