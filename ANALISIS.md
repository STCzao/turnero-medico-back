# Análisis Profesional — turnero-medico-backend

**Proyecto:** Sistema de Gestión de Turnos Médicos (Backend REST API)  
**Fecha:** 10 de marzo de 2026 — Revisión 5 (post Fase 1 + Fase 2)  
**Stack:** .NET 8 · ASP.NET Core · EF Core 8 · PostgreSQL · JWT · ASP.NET Identity  
**Estado de compilación:** ✅ 0 errores · 0 advertencias  
**Migraciones:** 9 (schema estable)

---

## Tabla de Contenidos

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [Inventario del Sistema](#2-inventario-del-sistema)
3. [Arquitectura](#3-arquitectura)
4. [Flujos de Negocio](#4-flujos-de-negocio)
5. [Catálogo de Endpoints](#5-catálogo-de-endpoints)
6. [Estado de Calidad — Issues Pendientes](#6-estado-de-calidad--issues-pendientes)
7. [Análisis de Seguridad](#7-análisis-de-seguridad)
8. [Evaluación MVP — Readiness para Frontend](#8-evaluación-mvp--readiness-para-frontend)
9. [Roadmap — Fases Siguientes](#9-roadmap--fases-siguientes)
10. [Métricas](#10-métricas)
11. [Historial de Cambios](#11-historial-de-cambios)

---

## 1. Resumen Ejecutivo

El backend implementa una API REST completa para la gestión de turnos de un consultorio médico. El sistema modela un flujo realista de 6 estados (solicitud → confirmación/rechazo → completado/ausente/cancelado) con 4 roles diferenciados (Paciente, Doctor, Secretaria, Admin).

Tras la Fase 1 (estabilización de bugs críticos) y la Fase 2 (features MVP), el sistema ofrece:

- **Registro seguro** con vinculación por DNI (pacientes) y Matrícula (doctores) dentro de transacciones.
- **Gestión completa del ciclo de vida del turno** con máquina de estados protegida.
- **Endpoints operativos** para cada rol: el paciente ve sus turnos, el doctor su agenda, la secretaria gestiona pendientes.
- **Sistema de horarios** con cálculo de disponibilidad en tiempo real.
- **Dependientes** (menores de edad sin cuenta propia).
- **Historial clínico básico** (turnos completados con observación clínica).

El proyecto compila sin errores y está en posición de conectarse con un frontend para conformar un MVP funcional.

---

## 2. Inventario del Sistema

### 2.1 Entidades (8)

| Entidad | Campos clave | Relaciones |
|---------|-------------|------------|
| **ApplicationUser** | Nombre, Apellido, Rol, DoctorId?, PacienteId?, FechaRegistro | Extiende IdentityUser |
| **ApplicationRole** | Descripcion | Extiende IdentityRole |
| **Doctor** | Matricula (unique), Especialidad, UserId? | 1:N Turnos, 1:N Horarios |
| **Paciente** | Dni (unique), FechaNacimiento, TipoPago, ObraSocialId?, ResponsableId?, UserId? | 1:N Turnos, N:1 ObraSocial |
| **Turno** | Estado, FechaHora?, Especialidad, Motivo, RowVersion, ObservacionClinica? | N:1 Paciente, N:1 Doctor?, N:1 ObraSocial? |
| **ObraSocial** | Especialidades (JSONB), Planes (JSONB), Observaciones | 1:N Pacientes |
| **Horario** | DoctorId, DiaSemana (0-6), HoraInicio, HoraFin, DuracionMinutos | N:1 Doctor (Cascade) |
| **EstadoTurno** | Clase estática con 6 constantes + lista `Todos` para validación | — |

### 2.2 DTOs (22 clases)

| Módulo | DTOs |
|--------|------|
| Auth | LoginRequestDto, RegisterPacienteDto, RegisterDoctorDto, RegisterSecretariaDto, RegisterRequestDto (legacy) |
| Turno | TurnoCreateDto, TurnoReadDto, TurnoUpdateDto, ConfirmarTurnoDto, RechazarTurnoDto, CancelarTurnoDto |
| Doctor | DoctorCreateDto, DoctorReadDto, DoctorUpdateDto |
| Paciente | PacienteCreateDto, PacienteReadDto, PacienteUpdateDto, DependienteCreateDto |
| ObraSocial | ObraSocialCreateDto, ObraSocialReadDto, ObraSocialUpdateDto |
| Horario | HorarioCreateDto, HorarioReadDto, SlotDisponibleDto |
| Common | PagedResultDto\<T\> |

### 2.3 Servicios (8)

| Servicio | Responsabilidad | Interfaz |
|----------|-----------------|----------|
| AuthService | Registro (3 flujos) + Login + JWT | IAuthService ✓ |
| TurnoService | CRUD + máquina de estados + agenda + historial | ITurnoService ✓ |
| DoctorService | CRUD + búsqueda por especialidad + perfil propio | IDoctorService ✓ |
| PacienteService | CRUD + perfil propio + dependientes | IPacienteService ✓ |
| ObraSocialService | CRUD obras sociales | IObraSocialService ✓ |
| HorarioService | CRUD horarios + cálculo de disponibilidad | IHorarioService ✓ |
| CurrentUserService | Extrae claims del usuario autenticado | ✗ (sin interfaz) |
| SeedDataService | Seed de roles + admin por defecto | ✗ (sin interfaz) |

### 2.4 Repositorios (2)

| Repositorio | Tipo | Especialización |
|-------------|------|-----------------|
| Repository\<T\> | Genérico | CRUD + Find + Paginación |
| TurnoRepository | Especializado | Include de Paciente, Doctor, ObraSocial + filtro por estado paginado en BD |

### 2.5 Índices de Base de Datos

| Índice | Columna(s) | Tipo |
|--------|-----------|------|
| IX_Pacientes_Dni | Dni | Unique |
| IX_Doctores_Matricula | Matricula | Unique |
| IX_Turnos_Estado | Estado | Simple |
| IX_Turnos_PacienteId | PacienteId | FK |
| IX_Turnos_DoctorId | DoctorId | FK |
| IX_Doctores_UserId | UserId | Simple |
| IX_Pacientes_UserId | UserId | Simple |
| IX_Pacientes_ResponsableId | ResponsableId | Simple |
| IX_Horarios_DoctorId_DiaSemana | DoctorId + DiaSemana | Compuesto |

---

## 3. Arquitectura

### 3.1 Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ASP.NET Core Pipeline                             │
│                                                                             │
│  HTTP → GlobalExceptionMiddleware → JWT Auth → RateLimiter → CORS → MVC    │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │                         Controllers (6)                               │  │
│  │  Auth · Turnos · Doctores · Pacientes · ObrasSociales · Horarios     │  │
│  │  [Authorize] + [Authorize(Roles)] por endpoint                       │  │
│  └────────────────────────────┬──────────────────────────────────────────┘  │
│                               │  DTOs (validados con DataAnnotations)       │
│  ┌────────────────────────────▼──────────────────────────────────────────┐  │
│  │                      Services (8) + AutoMapper                        │  │
│  │  Lógica de negocio · Autorización por rol · Máquina de estados       │  │
│  │  CurrentUserService: extrae identity del ClaimsPrincipal             │  │
│  └────────────────────────────┬──────────────────────────────────────────┘  │
│                               │                                             │
│  ┌────────────────────────────▼──────────────────────────────────────────┐  │
│  │                     Repositories (2)                                   │  │
│  │  Repository<T> genérico · TurnoRepository con Includes               │  │
│  └────────────────────────────┬──────────────────────────────────────────┘  │
│                               │                                             │
│  ┌────────────────────────────▼──────────────────────────────────────────┐  │
│  │                  ApplicationDbContext (EF Core 8)                      │  │
│  │  PostgreSQL · ASP.NET Identity · JSONB · RowVersion (concurrencia)    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Principios Aplicados

| Principio | Implementación |
|-----------|---------------|
| **Separation of Concerns** | Controllers solo manejan HTTP; Services contienen lógica de negocio; Repositories encapsulan data access. |
| **Dependency Inversion** | Todas las dependencias inyectadas por interfaz (excepto CurrentUserService y SeedDataService). |
| **Single Responsibility** | Cada servicio maneja una entidad de dominio. AuthService maneja exclusivamente autenticación/registro. |
| **Repository Pattern** | Genérico para CRUD estándar + especializado para queries complejas (TurnoRepository con Includes). |
| **Fail-Fast** | Middleware global captura excepciones y las mapea a HTTP status codes semánticos. |

### 3.3 Fortalezas de la Arquitectura

- **Doble barrera de autorización:** `[Authorize(Roles)]` en controller + verificación en service. Si el controller se bypasea, el service la atrapa.
- **Estado inyectado server-side:** Al crear un turno, `Estado`, `CreatedAt` y `CreatedByUserId` se asignan en el service, nunca desde el DTO del cliente.
- **Filtrado en base de datos:** `TurnoRepository.GetAllWithDetailsPagedAsync` filtra por estado y pagina directamente en la query SQL.
- **Concurrencia optimista:** `RowVersion` en Turno previene sobreescrituras simultáneas.
- **Trazabilidad:** Cada turno registra `CreatedByUserId`, `ConfirmadaPorId`, `FechaGestion`, `CreatedAt`.

---

## 4. Flujos de Negocio

### 4.1 Máquina de Estados del Turno

```
                    ┌──────────────┐
              ┌────►│  Rechazado   │  Secretaria/Admin + MotivoRechazo obligatorio
              │     └──────────────┘
              │
┌─────────────┴──────┐   Confirmar    ┌────────────┐   Completar    ┌────────────┐
│ SolicitudPendiente ├───────────────►│ Confirmado  ├──────────────►│ Completado │
└────────┬───────────┘  Sec/Admin +   └──┬────┬────┘   Doctor +    └────────────┘
         │              FechaHora +      │    │        Observación
         │              DoctorId         │    │
         │                               │    │  Ausente    ┌──────────┐
         │                               │    └────────────►│ Ausente  │
         │                               │      Doctor     └──────────┘
         │       Cancelar                │  Cancelar
         └──────────┬────────────────────┘
                    ▼
              ┌───────────┐
              │ Cancelado │  Todos los roles (con reglas específicas)
              └───────────┘
```

**Transiciones válidas:**

| Desde | Hacia | Quién | Requisitos |
|-------|-------|-------|------------|
| SolicitudPendiente | Confirmado | Secretaria, Admin | FechaHora + DoctorId; especialidad del doctor debe coincidir con la del turno |
| SolicitudPendiente | Rechazado | Secretaria, Admin | MotivoRechazo obligatorio (5-500 chars) |
| SolicitudPendiente | Cancelado | Paciente (propietario o responsable), Secretaria, Admin | Motivo opcional |
| Confirmado | Completado | Doctor (asignado), Admin | ObservacionClinica recomendada |
| Confirmado | Ausente | Doctor (asignado), Admin | — |
| Confirmado | Cancelado | Paciente, Doctor (asignado), Secretaria, Admin | Motivo opcional |

**Cualquier otra transición lanza `InvalidOperationException` → HTTP 400.**

---

### 4.2 Flujo de Registro de Paciente

```
  Cliente (Frontend)                         Backend
  ─────────────────                         ───────
        │                                      │
        │  POST /api/auth/register-paciente    │
        │  { email, password, nombre,          │
        │    apellido, dni, telefono,           │
        │    fechaNacimiento }                  │
        │─────────────────────────────────────►│
        │                                      │  BEGIN TRANSACTION
        │                                      │  1. Buscar Paciente por DNI
        │                                      │     ├─ Existe + ya tiene UserId → Error "DNI ya vinculado"
        │                                      │     ├─ Existe + sin UserId → Vincular (adopción)
        │                                      │     └─ No existe → Crear entidad Paciente
        │                                      │  2. Crear ApplicationUser (Identity)
        │                                      │  3. Asignar rol "Paciente"
        │                                      │  4. Vincular User ↔ Paciente (bidireccional)
        │                                      │  5. COMMIT (todo o nada)
        │                                      │
        │  200 { success, message }            │
        │◄─────────────────────────────────────│
```

**Vinculación por DNI:** Si un dependiente (creado por su padre) luego se registra usando el mismo DNI, el sistema detecta el Paciente existente y simplemente vincula la cuenta nueva sin crear duplicado.

**Registro de Doctor:** Mismo patrón pero vincula por Matrícula. Solo ejecutable por Admin.

---

### 4.3 Flujo Completo de un Turno (Happy Path)

```
  Paciente                  Secretaria                   Doctor
  ────────                  ──────────                   ──────
     │                          │                           │
     │  POST /api/turnos        │                           │
     │  { pacienteId,           │                           │
     │    especialidad,         │                           │
     │    motivo }              │                           │
     │─────────►                │                           │
     │                          │                           │
     │  Estado: SolicitudPendiente (sin fecha, sin doctor)  │
     │                          │                           │
     │                   GET /api/turnos/pendientes         │
     │                   (bandeja de solicitudes)           │
     │                          │                           │
     │                   GET /api/horarios/doctor/{id}/     │
     │                       disponibilidad?fecha=...       │
     │                   (consulta slots libres)            │
     │                          │                           │
     │                   POST /api/turnos/{id}/confirmar    │
     │                   { fechaHora, doctorId }            │
     │                          │                           │
     │  Estado: Confirmado (doctor + fecha asignados)       │
     │                          │                           │
     │                          │     GET /api/turnos/      │
     │                          │     doctor/me/agenda      │
     │                          │     ?fecha=2026-03-15     │
     │                          │           │               │
     │                          │     PATCH /api/turnos/{id}│
     │                          │     { estado:"Completado",│
     │                          │       observacionClinica } │
     │                          │           │               │
     │  Estado: Completado (con observación clínica)        │
     │                                                      │
     │  GET /api/turnos/paciente/{id}/historial             │
     │  (turnos completados + observaciones)                │
```

---

### 4.4 Flujo de Dependientes (Menores de Edad)

```
  Paciente (padre/madre)                    Backend
  ──────────────────────                    ───────
        │                                      │
        │  POST /api/pacientes/dependientes    │
        │  { dni, nombre, apellido,            │
        │    fechaNacimiento }                  │
        │─────────────────────────────────────►│
        │                                      │  1. Validar DNI único
        │                                      │  2. Crear Paciente:
        │                                      │     ResponsableId = userId del padre
        │                                      │     EsMayorDeEdad = false
        │                                      │     UserId = null
        │                                      │
        │  201 { dependiente }                 │
        │◄─────────────────────────────────────│
        │                                      │
        │  Operaciones disponibles:            │
        │  · POST /turnos { pacienteId: hijo } │  → Autorizado por ResponsableId
        │  · GET /turnos/paciente/{hijoId}     │  → Autorizado por ResponsableId
        │  · GET /pacientes/mis-dependientes   │  → Lista todos los dependientes
```

**Emancipación automática:** Si el menor crece y se registra con `POST /register-paciente` usando su DNI, el sistema vincula la cuenta existente. A partir de ahí, su propio `UserId` toma precedencia sobre `ResponsableId` en las verificaciones de autorización.

---

### 4.5 Flujo de Disponibilidad de Horarios

```
  Secretaria                                Backend
  ──────────                                ───────
       │                                       │
       │  GET /api/horarios/doctor/5/          │
       │      disponibilidad?fecha=2026-03-17  │
       │──────────────────────────────────────►│
       │                                       │  1. DiaSemana de 2026-03-17 → Martes (2)
       │                                       │  2. Buscar Horarios del doctor para día 2:
       │                                       │     09:00-13:00 (30 min), 15:00-18:00 (30 min)
       │                                       │  3. Buscar Turnos Confirmados para esa fecha:
       │                                       │     09:30, 10:00, 15:00 ocupados
       │                                       │  4. Generar slots, filtrar ocupados y pasados:
       │                                       │     09:00 ✓, 10:30 ✓, 11:00 ✓, 15:30 ✓, ...
       │                                       │
       │  200 [                                │
       │    { fechaHora: "2026-03-17T09:00",   │
       │      doctorId: 5,                     │
       │      doctorNombre: "Dr. López",       │
       │      duracionMinutos: 30 },           │
       │    ...                                │
       │  ]                                    │
       │◄──────────────────────────────────────│
```

---

### 4.6 Reglas de Cancelación por Rol

| Rol | Estado permitido | Validación adicional |
|-----|-----------------|---------------------|
| **Admin** | SolicitudPendiente, Confirmado | Ninguna (acceso total) |
| **Secretaria** | SolicitudPendiente, Confirmado | Ninguna |
| **Doctor** | Solo Confirmado | Debe estar asignado a ese turno |
| **Paciente** | SolicitudPendiente, Confirmado | Debe ser propietario del turno o responsable del dependiente |

---

## 5. Catálogo de Endpoints

### 5.1 Autenticación (`/api/auth`) — 5 endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/register-paciente` | Anónimo | Auto-registro con vinculación por DNI |
| POST | `/register-doctor` | Admin | Registro + vinculación por Matrícula |
| POST | `/register-secretaria` | Admin | Registro de secretaria |
| POST | `/login` | Anónimo (Rate: 5/min) | Login → JWT token |
| GET | `/profile` | Autenticado | Claims del usuario actual |

### 5.2 Turnos (`/api/turnos`) — 14 endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/` | Admin, Secretaria | Paginado + filtro ?estado |
| GET | `/{id}` | Autenticado | Detalle con control de acceso |
| GET | `/paciente/{id}` | Todos | Turnos por paciente + ?estado |
| GET | `/doctor/{id}` | Todos | Turnos por doctor + ?estado |
| GET | `/me` | Paciente, Doctor | Mis turnos (ID resuelto automáticamente) |
| GET | `/doctor/me/agenda?fecha=` | Doctor | Agenda confirmada del día |
| GET | `/pendientes` | Secretaria, Admin | Solicitudes pendientes paginadas |
| GET | `/paciente/{id}/historial` | Todos | Completados con observación clínica |
| POST | `/` | Paciente, Secretaria, Admin | Crear solicitud |
| POST | `/{id}/confirmar` | Secretaria, Admin | Asignar fecha + doctor → Confirmado |
| POST | `/{id}/rechazar` | Secretaria, Admin | Rechazar con motivo → Rechazado |
| POST | `/{id}/cancelar` | Todos | Cancelar con reglas por rol |
| PATCH | `/{id}` | Doctor, Admin | Completado/Ausente + observación |
| DELETE | `/{id}` | Admin | Eliminar |

### 5.3 Doctores (`/api/doctores`) — 7 endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/` | Admin | Paginado |
| GET | `/me` | Doctor | Perfil propio |
| GET | `/{id}` | Autenticado | Detalle |
| GET | `/especialidad/{esp}` | Autenticado | Búsqueda por especialidad |
| POST | `/` | Admin | Crear (sin cuenta usuario) |
| PUT | `/{id}` | Admin | Actualizar |
| DELETE | `/{id}` | Admin | Eliminar |

### 5.4 Pacientes (`/api/pacientes`) — 8 endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/` | Admin | Paginado |
| GET | `/me` | Paciente | Perfil propio |
| GET | `/{id}` | Autenticado | Detalle |
| GET | `/mis-dependientes` | Paciente | Dependientes del responsable |
| POST | `/` | Admin, Secretaria | Crear (sin cuenta usuario) |
| POST | `/dependientes` | Paciente | Registrar menor dependiente |
| PUT | `/{id}` | Admin | Actualizar |
| DELETE | `/{id}` | Admin | Eliminar |

### 5.5 Obras Sociales (`/api/obras-sociales`) — 5 endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/` | Admin | Listar todas |
| GET | `/{id}` | Autenticado | Detalle |
| POST | `/` | Admin | Crear (nombre único) |
| PUT | `/{id}` | Admin | Actualizar |
| DELETE | `/{id}` | Admin | Eliminar |

### 5.6 Horarios (`/api/horarios`) — 4 endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/doctor/{id}` | Autenticado | Horarios semanales |
| GET | `/doctor/{id}/disponibilidad?fecha=` | Admin, Secretaria, Paciente | Slots libres |
| POST | `/` | Admin | Crear bloque (con detección de overlap) |
| DELETE | `/{id}` | Admin | Eliminar bloque |

**Total: 43 endpoints funcionales.**

---

## 6. Estado de Calidad — Issues Pendientes

### 6.1 Resueltos en Fase 1 + Fase 2 ✓

| # | Issue original | Resolución |
|---|----------------|------------|
| C1 | Sin transacciones en registro | `IDbContextTransaction` con rollback automático |
| C2 | Admin puede inyectar cualquier estado vía PATCH | Whitelist validada antes del branch Admin |
| C3 | register-paciente / register-doctor no expuestos | Endpoints creados en AuthController |
| A1 | Especialidad no validada al confirmar | Doctor.Especialidad comparada con Turno.Especialidad |
| A2 | 404 en lista vacía de turnos | Retorna 200 con array vacío |
| A3 | ?estado no validado en query string | `ValidarEstado()` contra `EstadoTurno.Todos` |
| A4 | Sin índices en columnas filtradas | 7 índices nuevos en migración Fase2 |

### 6.2 Issues Abiertos

| Sev | Issue | Ubicación | Impacto |
|-----|-------|-----------|---------|
| **ALTO** | ObrasSociales solo accesible para Admin | ObrasSocialesController | Secretaria no puede verificar cobertura |
| **ALTO** | `PacienteReadDto.ObraSocial` siempre null | Repository genérico sin Include | Frontend no puede mostrar OS del paciente |
| **MEDIO** | `MotivoRechazo` reutilizado para cancelaciones | Turno entity | Un campo para dos conceptos |
| **MEDIO** | CurrentUserService y SeedDataService sin interfaz | DI | Bloquea testing unitario |
| **MEDIO** | JWT usa `user.Rol` en vez de `GetRolesAsync` | AuthService | JWT desincronizado si se cambia rol vía Identity |
| **MEDIO** | `GetAllAsync` sin paginación en Doctor y OS | Services | No escalable |
| **MEDIO** | `DateTime` vs `DateTimeOffset` en FechaHora | ConfirmarTurnoDto | Errores de timezone posibles |
| **BAJO** | Password admin hardcodeado | SeedDataService | Visible en repo |
| **BAJO** | RegisterRequestDto legacy sin uso activo | AuthDTOs | Código muerto |
| **BAJO** | `TurnoService.GetAllAsync` nunca invocado | TurnoService | Método muerto en interfaz |

---

## 7. Análisis de Seguridad

### 7.1 Controles Implementados ✓

| Control | Implementación |
|---------|---------------|
| Autenticación | JWT con HMAC-SHA256, ClockSkew = Zero |
| Autorización | `[Authorize(Roles)]` + verificación en service layer |
| Rate Limiting | Login: 5 intentos/minuto por IP |
| CORS | Orígenes específicos: localhost:5173, localhost:3000 |
| Registro restringido | Solo Paciente puede auto-registrarse |
| Estado inmutable | Estado, CreatedAt, CreatedByUserId inyectados server-side |
| Concurrencia | RowVersion en Turno (optimistic locking) |
| Transacciones | Registro usa IDbContextTransaction |
| Validación de entrada | DataAnnotations en DTOs + validación de negocio |
| User Secrets | Configurado para desarrollo |

### 7.2 Pendientes de Implementar

| Prioridad | Control | Recomendación |
|-----------|---------|---------------|
| ALTA | Rate limiting solo en login | Extender a endpoints de escritura |
| ALTA | Sin validación de JWT SecretKey al startup | Verificar >= 32 bytes, fallar si no cumple |
| MEDIA | JWT no se invalida al cambiar rol | Tokens cortos (15 min) + Refresh token |
| MEDIA | CORS hardcodeado para dev | Configurar por ambiente en appsettings |
| BAJA | Sin audit log de acciones admin | Tabla de auditoría con actor, acción, timestamp |

---

## 8. Evaluación MVP — Readiness para Frontend

### 8.1 Operatividad por Rol

| Rol | Estado | Detalle |
|-----|--------|---------|
| **Paciente** | 🟢 95% | Registrarse, solicitar turnos, ver sus turnos, registrar dependientes, ver historial. Falta: ver su ObraSocial en el perfil (siempre null). |
| **Doctor** | 🟢 100% | Ver perfil, consultar agenda del día, completar/marcar ausente, ver historial del paciente. |
| **Secretaria** | 🟡 85% | Ver pendientes, confirmar/rechazar, cancelar, ver disponibilidad. Falta: consultar obras sociales (solo Admin). |
| **Admin** | 🟢 100% | CRUD completo de todas las entidades, gestión de horarios, registro de doctores y secretarias. |

### 8.2 Contrato API para el Frontend

**Autenticación:** JWT en header `Authorization: Bearer <token>`. Claims: `sub` (userId), `email`, `name`, `role`.

**Formato de errores consistente:**
```json
{
  "statusCode": 400,
  "message": "El paciente con ID 5 no existe.",
  "detail": null,
  "timestamp": "2026-03-10T12:00:00Z"
}
```

**Paginación estándar:** `?page=1&pageSize=20` →
```json
{
  "items": [...],
  "total": 150,
  "page": 1,
  "pageSize": 20,
  "totalPages": 8,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Filtrado por estado:** `?estado=Confirmado` — validado contra 6 valores: `SolicitudPendiente`, `Confirmado`, `Rechazado`, `Completado`, `Ausente`, `Cancelado`.

**CORS:** `localhost:5173` (Vite) y `localhost:3000` (CRA).

### 8.3 Pantallas Sugeridas para el Frontend MVP

| # | Pantalla | Rol | Endpoints principales |
|---|----------|-----|-----------------------|
| 1 | **Login** | Todos | `POST /auth/login` |
| 2 | **Registro** | Anónimo | `POST /auth/register-paciente` |
| 3 | **Mis Turnos** | Paciente | `GET /turnos/me`, `POST /turnos/{id}/cancelar` |
| 4 | **Solicitar Turno** | Paciente | `GET /doctores/especialidad/{e}`, `GET /horarios/.../disponibilidad`, `POST /turnos` |
| 5 | **Mis Dependientes** | Paciente | `GET /pacientes/mis-dependientes`, `POST /pacientes/dependientes` |
| 6 | **Agenda del Día** | Doctor | `GET /turnos/doctor/me/agenda?fecha=`, `PATCH /turnos/{id}` |
| 7 | **Historial Paciente** | Doctor | `GET /turnos/paciente/{id}/historial` |
| 8 | **Dashboard Pendientes** | Secretaria | `GET /turnos/pendientes`, `POST .../confirmar`, `POST .../rechazar` |
| 9 | **Panel Admin** | Admin | CRUD doctores, pacientes, obras sociales, horarios |

**Con estas 9 pantallas y los 43 endpoints existentes se cubre el 100% del flujo operativo de un consultorio.**

### 8.4 Conclusión de Readiness

El backend está **listo para conectar con un frontend MVP**. Los flujos críticos (registro, login, solicitud de turno, gestión por secretaria, agenda del doctor, completar turno) están completos e integrados.

Los 2 issues marcados como ALTO (ObraSocial null + acceso restringido) son mejoras de UX, no bloquean el flujo core. Un frontend puede arrancar con las 9 pantallas propuestas sin necesidad de cambios adicionales en el backend.

Para una demo profesional, se recomienda resolver los 2 issues ALTO (son cambios menores) y agregar documentación Swagger con ejemplos.

---

## 9. Roadmap — Fases Siguientes

### FASE 3: Pre-Producción

| Tarea | Prioridad |
|-------|-----------|
| Abrir lectura de ObrasSociales a Secretaria/Paciente | Alta |
| Resolver PacienteReadDto.ObraSocial null (PacienteRepository con Include) | Alta |
| Swagger/OpenAPI documentado con XML comments y ejemplos | Alta |
| Health checks (`/health` con validación de BD) | Media |
| Docker multi-stage + docker-compose con PostgreSQL | Media |
| Logging estructurado (Serilog) con correlation IDs | Media |
| Interfaces para CurrentUserService y SeedDataService | Media |
| Separar MotivoCancelacion de MotivoRechazo | Media |
| Tests unitarios de la máquina de estados | Media |
| CORS configurado por ambiente (appsettings) | Baja |

### FASE 4: Features Avanzadas

| Feature | Valor |
|---------|-------|
| Notificaciones email (confirmar/rechazar/cancelar) | Reduce no-shows |
| Reprogramación de turnos (`POST .../reprogramar`) | UX mejorado |
| Recordatorios automáticos (24h y 2h) | BackgroundService |
| Reportes (% ausentismo, turnos/especialidad) | Métricas de negocio |
| Refresh Token (JWT 15 min + refresh 7 días) | Seguridad |
| Audit Trail (tabla de auditoría) | Compliance médico |
| Multi-sucursal (modelo Sucursal) | Escalabilidad |

---

## 10. Métricas

| Métrica | Valor |
|---------|-------|
| Archivos .cs | ~45 |
| Entidades | 8 |
| DTOs | 22 |
| Endpoints | 43 |
| Servicios | 8 (6 con interfaz) |
| Repositorios | 2 |
| Migraciones | 9 |
| Índices de BD | 9 |
| Tests | 0 |
| Docker | No |
| CI/CD | No |

---

## 11. Historial de Cambios

| Revisión | Fecha | Cambios |
|----------|-------|---------|
| Rev 1-3 | Feb 2026 | Análisis inicial, modelo de datos, primeros bugs |
| Rev 4 | 10/03/2026 | Análisis integral: inventario completo, roadmap 4 fases, bugs C1-C3/A1-A5/M1-M6, análisis de seguridad |
| Rev 5 | 10/03/2026 | **Post Fase 1 + Fase 2.** 7 bugs resueltos. +10 endpoints nuevos (me, agenda, pendientes, historial, horarios, dependientes). Definición de 6 flujos de negocio. Evaluación MVP con readiness por rol. Catálogo completo de 43 endpoints. Reescritura completa del documento. |