# Análisis Completo — turnero-medico-backend
Fecha: 28 de febrero de 2026

---

## Estado General

El proyecto compila con 0 errores y 0 advertencias. La migración RefactorTurnoFlowAndSecretaria está aplicada en la base de datos. La arquitectura base es correcta y el flujo de negocio rediseñado (solicitud pendiente → secretaria confirma/rechaza → doctor marca completado/ausente) está implementado de manera consistente en todas las capas.

---

## 1. Arquitectura

La separación de responsabilidades es clara: Controller → Service (con interfaz) → Repository genérico → DbContext. El repository pattern usa Expression como predicados, lo cual es correcto porque evita que IQueryable se filtre fuera de la capa de datos. AutoMapper está centralizado en AutoMapperProfile. CurrentUserService actúa como servicio dedicado a leer claims del usuario autenticado. GlobalExceptionMiddleware captura excepciones no manejadas. PagedResultDto es genérico y reutilizable.

Problemas encontrados:

CRÍTICO — SaveChangesAsync silencia excepciones. En Repository.SaveChangesAsync el catch devuelve false pero todo el código que llama a UpdateAsync, AddAsync y DeleteAsync nunca verifica ese resultado. Si la base de datos rechaza una operación (por ejemplo una violación de concurrencia con RowVersion), el servicio retorna el resultado como si hubiera tenido éxito. Esto es un bug silencioso que puede causar inconsistencias difíciles de depurar.

CRÍTICO — Sin transacciones en registro de usuarios. Los métodos RegisterPacienteAsync y RegisterDoctorAsync crean primero el usuario en ASP.NET Identity y luego crean la entidad de dominio (Paciente o Doctor) en dos pasos separados. Si el segundo paso falla, queda un usuario de Identity sin entidad asociada, lo cual rompe todas las operaciones posteriores. El propio código tiene un comentario TODO reconociendo este problema pero no está resuelto.

MEDIA — SeedDataService y CurrentUserService no tienen interfaz. Están registrados en el contenedor de DI como clase concreta. Esto rompe el principio de inyección por abstracción y hace imposible reemplazarlos en tests sin modificar el código de producción.

MEDIA — El Repository genérico no ofrece mecanismo de Include (eager loading). Los métodos GetAllAsync, GetAllPagedAsync y FindAsync hacen queries simples sin cargar propiedades de navegación. Esto produce un problema grave descrito en la sección de rendimiento.

---

## 2. Seguridad

Fortalezas: JWT configurado con ClockSkew en cero, rate limiting de 5 intentos por minuto en login, roles verificados tanto en el controller como en el servicio (doble barrera), el campo Estado es ignorado por AutoMapper al crear un turno (el cliente no puede forzar el estado inicial).

Problemas encontrados:

CRÍTICO — El endpoint de registro público permite que cualquier usuario se auto-asigne el rol Admin. En RegisterAsync, la lista de roles válidos incluye "Admin". Cualquier persona puede hacer un POST al endpoint de registro con rol "Admin" y obtener privilegios totales. Este endpoint debería solo permitir el rol "Paciente" para auto-registro. Los roles "Doctor", "Admin" y "Secretaria" deberían ser asignables únicamente por un administrador.

ALTA — La clave secreta JWT no tiene validación de longitud mínima al iniciar la aplicación. Si appsettings.json tiene una clave corta (menos de 256 bits para HMAC-SHA256), la aplicación arranca sin error pero el token generado es criptográficamente débil. Debería validarse en startup y lanzar una excepción si la clave no cumple el mínimo.

MEDIA — El JWT usa el campo user.Rol (campo custom en ApplicationUser) en lugar de los roles de Identity. Si un administrador cambia el rol de un usuario en la base de datos usando UserManager, el token JWT existente sigue siendo válido con el rol anterior hasta que expire. No hay blacklisting ni invalidación de tokens. Para un sistema médico esto puede ser relevante si se necesita revocar permisos de inmediato.

MEDIA — Solo el endpoint de login tiene rate limiting. Los endpoints de creación de turnos, registro de pacientes y consultas no tienen protección contra flood.

BAJA — appsettings.json probablemente contiene la SecretKey en texto plano en el repositorio. Debería usarse User Secrets en desarrollo y variables de entorno en producción.

---

## 3. Máquina de Estados (Turno)

El flujo de 6 estados está correctamente definido y las transiciones son coherentes. Las transiciones válidas implementadas son:

SolicitudPendiente puede pasar a Confirmado (por Secretaria/Admin en ConfirmarAsync), a Rechazado (por Secretaria/Admin en RechazarAsync), o a Cancelado (por cualquier rol en CancelarAsync).
Confirmado puede pasar a Completado (por Doctor/Admin en UpdateAsync), a Ausente (por Doctor/Admin en UpdateAsync), o a Cancelado (por cualquier rol en CancelarAsync).

Problemas encontrados:

ALTA — UpdateAsync no valida en la capa de servicio que el estado nuevo sea Completado o Ausente. El DTO tiene una anotación RegularExpression que restringe los valores, pero las validaciones de DTO se pueden eludir si el endpoint es llamado directamente. Un Admin que llama a PATCH podría poner cualquier estado arbitrario incluyendo SolicitudPendiente, revirtiendo un turno ya procesado. La validación debe existir en la capa de servicio, no solo en las Data Annotations del DTO.

MEDIA — La especialidad del turno no se valida contra la especialidad del doctor al confirmar. Al crear la solicitud, si el paciente elige un doctor específico, se verifica que su especialidad coincida. Pero cuando la secretaria confirma y asigna (o reasigna) un doctor, no se hace este chequeo. La secretaria podría confirmar un turno de Cardiología asignando un Traumatólogo.

MEDIA — El campo MotivoRechazo se reutiliza tanto para rechazos como para cancelaciones. Cuando se cancela, el código hace turno.MotivoRechazo = dto.Motivo. Semánticamente, una cancelación no es un rechazo. Convendría un campo MotivoCancelacion separado, o renombrar el campo existente a algo más neutral como NotasGestion o MotivoCierre.

---

## 4. Rendimiento y Queries (CRÍTICO)

Este es el área con el problema más impactante en términos de funcionalidad real.

CRÍTICO — Los nombres PacienteNombre y DoctorNombre en TurnoReadDto siempre muestran "No disponible" o "Sin asignar". El AutoMapperProfile mapea estos campos desde las propiedades de navegación Turno.Paciente y Turno.Doctor. Sin embargo, el Repository genérico nunca hace Include de estas propiedades. Como EF Core tiene lazy loading deshabilitado por defecto, Turno.Paciente y Turno.Doctor son siempre null al momento del mapeo. El fallback del mapeador devuelve "No disponible" y "Sin asignar" respectivamente. Esto afecta absolutamente todos los endpoints que devuelven TurnoReadDto.

Para corregirlo hay dos opciones: agregar métodos especializados al repository que hagan Include, o agregar métodos de query directamente a TurnoService usando el DbContext con Include. La segunda opción es más pragmática dado el tamaño del proyecto.

ALTA — Sin índices en las columnas más consultadas. El campo Turno.Estado es filtrado en todos los endpoints que aceptan el parámetro ?estado. Los campos Turno.PacienteId y Turno.DoctorId son filtrados en GetByPacienteAsync y GetByDoctorAsync respectivamente. Los campos Doctor.UserId y Paciente.UserId son consultados en cada verificación de autorización dentro de los servicios. Ninguno de estos tiene índice explícito configurado en ApplicationDbContext. En tablas con miles de registros, estas queries hacen full table scan.

MEDIA — GetAllAsync sin paginación existe en DoctorService y ObraSocialService. Carga todos los registros en memoria de una vez. Aceptable para catálogos pequeños pero es una deuda técnica que debería eliminarse.

---

## 5. Diseño de API

Fortalezas: convenciones REST respetadas, CreatedAtAction en creación, NoContent en eliminación, paginación implementada.

Problemas encontrados:

ALTA — El Doctor no puede descubrir su propio ID numérico. Para usar el endpoint GET /api/turnos/doctor/{id}, el Doctor necesita conocer su Doctor.Id (número entero en la tabla Doctores). No existe un endpoint GET /api/doctores/me que devuelva el perfil completo del doctor autenticado. El método GetMyProfileAsync existe en DoctorService pero nunca fue expuesto como endpoint en DoctoresController.

MEDIA — Los endpoints GetByPaciente y GetByDoctor devuelven 404 cuando la lista está vacía. Una lista vacía debería ser 200 con un array vacío. El 404 significa "recurso no encontrado", pero el recurso (el paciente, el doctor) sí existe — simplemente no tiene turnos. Esto confunde a los clientes HTTP y rompe convenciones REST estándar.

MEDIA — El parámetro ?estado no valida que el valor sea uno de los 6 estados permitidos. Si un cliente manda ?estado=pendiente (minúscula) o ?estado=invalido, la query simplemente retorna 0 resultados sin ningún error o advertencia. Debería retornar 400 con la lista de valores válidos.

MEDIA — El PATCH /{id} recibe un DTO que incluye el campo Id en el body. Ya se valida que coincida con el ID de la URL, pero es mejor práctica no incluir el ID en el body de PATCH dado que ya viene en la ruta.

BAJA — No existe GET /api/turnos/me para que el paciente autenticado vea sus propios turnos directamente, sin necesidad de conocer su PacienteId numérico. Actualmente el paciente necesita llamar a GetByPaciente con su ID propio, que tiene que haber obtenido de alguna otra llamada previa.

---

## 6. Calidad de Código

DoctorService inyecta CurrentUserService como clase concreta en lugar de interfaz. Lo mismo ocurre en PacienteService. Si se cambia la implementación de CurrentUserService, hay que modificar cada constructor que la use.

TurnoService es el servicio más largo del proyecto con más de 400 líneas. Mezcla autorización, validación de negocio, y persistencia. Para este scope es manejable, pero en un proyecto mayor convendría separar la lógica de autorización en handlers dedicados de ASP.NET Authorization.

ApplicationDbContext tiene comentarios XML mal formados (usan etiquetas en texto plano en lugar de la sintaxis XML correcta de documentación).

Repository.SaveChangesAsync es público pero no está declarado en la interfaz IRepository. Se llama internamente, lo cual es correcto, pero exponerlo como método público en la clase concreta genera confusión sobre cuándo debe llamarse externamente.

No existe ningún proyecto de tests. Los servicios son testeables porque usan inyección por interfaz, pero actualmente no hay ninguna cobertura de tests unitarios ni de integración.

---

## Resumen Priorizado

Los siguientes problemas deben resolverse en orden de impacto:

Prioridad 1 — Funcionalidad rota en producción:
El problema de los Includes faltantes hace que PacienteNombre y DoctorNombre sean siempre "No disponible". Esto es visible para cualquier usuario del sistema desde el primer día. SaveChangesAsync silenciando errores puede causar inconsistencias de datos invisibles. El registro público con rol Admin es un agujero de seguridad crítico.

Prioridad 2 — Correctitud del sistema:
Sin transacciones en registro puede dejar usuarios huérfanos. UpdateAsync permite estados inválidos vía Admin. El 404 incorrecto en listas vacías rompe clientes que siguen convenciones REST.

Prioridad 3 — Completar funcionalidad conocida faltante:
Endpoint GET /api/doctores/me (el método ya existe, solo falta el endpoint). Validación de especialidad en ConfirmarAsync. Índices de base de datos para performance.

Prioridad 4 — Deuda técnica a resolver antes de escalar:
Interfaces para CurrentUserService y SeedDataService. Eliminar GetAllAsync sin paginación. Separar MotivoCancelacion de MotivoRechazo. Validación del parámetro ?estado.
