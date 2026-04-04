using turnero_medico_backend.Models.Entities;

namespace turnero_medico_backend.Tests.Models
{
    public class EstadoTurnoTests
    {
        // ── Transiciones válidas ────────────────────────────────

        [Theory]
        [InlineData(EstadoTurno.SolicitudPendiente, EstadoTurno.Confirmado)]
        [InlineData(EstadoTurno.SolicitudPendiente, EstadoTurno.Rechazado)]
        [InlineData(EstadoTurno.SolicitudPendiente, EstadoTurno.Cancelado)]
        [InlineData(EstadoTurno.Confirmado, EstadoTurno.Completado)]
        [InlineData(EstadoTurno.Confirmado, EstadoTurno.Ausente)]
        [InlineData(EstadoTurno.Confirmado, EstadoTurno.Cancelado)]
        public void ValidarTransicion_TransicionPermitida_NoLanzaExcepcion(string desde, string hacia)
        {
            var ex = Record.Exception(() => EstadoTurno.ValidarTransicion(desde, hacia));
            Assert.Null(ex);
        }

        // ── Transiciones inválidas ──────────────────────────────

        [Theory]
        [InlineData(EstadoTurno.Rechazado, EstadoTurno.Confirmado)]
        [InlineData(EstadoTurno.Completado, EstadoTurno.Cancelado)]
        [InlineData(EstadoTurno.Ausente, EstadoTurno.Confirmado)]
        [InlineData(EstadoTurno.Cancelado, EstadoTurno.SolicitudPendiente)]
        [InlineData(EstadoTurno.Confirmado, EstadoTurno.SolicitudPendiente)]
        [InlineData(EstadoTurno.SolicitudPendiente, EstadoTurno.Completado)]
        [InlineData(EstadoTurno.SolicitudPendiente, EstadoTurno.Ausente)]
        public void ValidarTransicion_TransicionProhibida_LanzaInvalidOperation(string desde, string hacia)
        {
            Assert.Throws<InvalidOperationException>(() => EstadoTurno.ValidarTransicion(desde, hacia));
        }

        [Fact]
        public void ValidarTransicion_EstadoDesconocido_LanzaInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => EstadoTurno.ValidarTransicion("Inventado", EstadoTurno.Confirmado));
        }

        // ── Estados finales ─────────────────────────────────────

        [Theory]
        [InlineData(EstadoTurno.Rechazado)]
        [InlineData(EstadoTurno.Completado)]
        [InlineData(EstadoTurno.Ausente)]
        [InlineData(EstadoTurno.Cancelado)]
        public void EsEstadoFinal_EstadoTerminal_DevuelveTrue(string estado)
        {
            Assert.True(EstadoTurno.EsEstadoFinal(estado));
        }

        [Theory]
        [InlineData(EstadoTurno.SolicitudPendiente)]
        [InlineData(EstadoTurno.Confirmado)]
        public void EsEstadoFinal_EstadoNoTerminal_DevuelveFalse(string estado)
        {
            Assert.False(EstadoTurno.EsEstadoFinal(estado));
        }

        // ── Desde estados finales no hay salida ─────────────────

        [Theory]
        [InlineData(EstadoTurno.Rechazado)]
        [InlineData(EstadoTurno.Completado)]
        [InlineData(EstadoTurno.Ausente)]
        [InlineData(EstadoTurno.Cancelado)]
        public void ValidarTransicion_DesdeEstadoFinal_SiempreFalla(string estadoFinal)
        {
            foreach (var destino in EstadoTurno.Todos)
            {
                Assert.Throws<InvalidOperationException>(
                    () => EstadoTurno.ValidarTransicion(estadoFinal, destino));
            }
        }
    }
}
