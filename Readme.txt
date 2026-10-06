# 🚚 MueveYa - Aplicación de Fletes

Aplicación móvil desarrollada en **.NET MAUI** que conecta clientes con conductores para solicitar y ejecutar servicios de transporte y fletes.

---

## 📋 Cambios Recientes (Última Actualización)

### 🗺️ Integración de Mapas
- **Implementación de mapas interactivos** usando `Microsoft.Maui.Controls.Maps`
- Visualización de la ubicación actual del cliente en **HomeCliente**
- Visualización de solicitudes disponibles con pins en **HomeConductor**
- Centrado automático del mapa en la ubicación del usuario
- Soporte para búsqueda y selección de destinos directamente en el mapa

### 📱 Nuevas Funcionalidades para Clientes

#### Solicitud de Fletes
- **Interfaz mejorada** para solicitar servicios de transporte
- Selección de **origen y destino** con ubicación geográfica exacta
- Cálculo automático de **distancia en km** usando algoritmo Haversine
- Visualización de **opciones de vehículos** con precios estimados
- Confirmación de solicitud con precio total antes de confirmar

#### Búsqueda de Destinos
- Input de búsqueda en tiempo real dentro del mapa
- Geolocalización automática del cliente al abrir la app
- Almacenamiento de ubicación en `Preferences` para referencias futuras

#### Características de Vehículos
- Visualización de vehículos disponibles en un **CollectionView horizontal**
- Información detallada: marca, modelo, año, capacidad
- Imágenes de los vehículos
- Precio dinámico según tipo de vehículo y distancia
- Selección de vehículo preferido

### 🚗 Nuevas Funcionalidades para Conductores

#### Recepción de Solicitudes en Tiempo Real
- Integración con **SignalR** para notificaciones en tiempo real
- Visualización de pins de solicitudes disponibles en el mapa
- Información de solicitud: precio, origen, destino
- Aceptación de solicitudes directamente desde el mapa

#### Gestión de Viajes
- Seguimiento de viajes en curso
- Estado actual del viaje (pendiente, en curso, completado)
- Visualización de información del cliente y detalles del vehículo
- Finalización de viajes

---

## 🔧 Arquitectura Técnica

### Estructura del Proyecto
```
AppFletesMueve/
├── Views/
│   ├── LoginPage.xaml(.cs)           # Autenticación
│   ├── HomeCliente.xaml(.cs)         # Panel cliente con mapa y solicitudes
│   ├── HomeConductor.xaml(.cs)       # Panel conductor con viajes activos
│   ├── RegistroPage.xaml(.cs)        # Registro de nuevos usuarios
│   └── CompletarPerfilConductorPage.xaml(.cs)
├── Services/
│   ├── TransporteService.cs          # API de transporte y fletes
│   └── UsuarioService.cs             # Autenticación y gestión de usuarios
├── Models/
│   ├── Usuario.cs
│   ├── SolicitudFlete.cs
│   ├── Conductor.cs
│   ├── Vehiculo.cs
│   └── SesionUsuario.cs              # Datos de sesión en memoria
├── ViewModels/
│   └── MainViewModel.cs              # Binding de datos para HomeCliente
└── Resources/
    └── Styles/, Fonts/, Images/
```

### Tecnologías Utilizadas
- **Framework**: .NET MAUI 10
- **Mapas**: Microsoft.Maui.Maps, Google Maps
- **Tiempo Real**: SignalR
- **Geolocalización**: Microsoft.Maui.Devices.Sensors
- **Almacenamiento Local**: Preferences
- **API Backend**: ASP.NET Core Web API
- **Lenguaje**: C#

---

## 📡 Flujo de Solicitud de Flete (Cliente)

```
1. Cliente abre HomeCliente
   ↓
2. App obtiene ubicación actual automáticamente
   ↓
3. Cliente busca destino en el mapa
   ↓
4. Cliente selecciona vehículo deseado
   ↓
5. Cliente confirma solicitud con precio
   ↓
6. TransporteService.CrearSolicitud() llamada
   ↓
7. Si éxito → Evento SolicitudCreada se dispara
   ↓
8. HomeConductor recibe notificación (SignalR)
   ↓
9. Pin aparece en mapa de conductor
```

---

## 📡 Flujo de Aceptación de Viaje (Conductor)

```
1. Conductor ve solicitud en mapa (pin)
   ↓
2. Conductor toca el pin o botón de aceptación
   ↓
3. Diálogo de confirmación con detalles
   ↓
4. TransporteService.AceptarSolicitud() llamada
   ↓
5. Solicitud actualiza estado a "EN_CURSO"
   ↓
6. HomeConductor muestra viaje activo
   ↓
7. Conductor finaliza viaje → Estado "COMPLETADO"
```

---

## 🌐 Integración con Mapas

### HomeCliente
```xml
<maps:Map x:Name="mapCliente" HeightRequest="220" />
```
- Muestra ubicación actual del cliente
- Permite marcar destino (tap en mapa)
- Input de búsqueda de dirección

### HomeConductor
```xml
<maps:Map x:Name="mapConductor" />
```
- Muestra pins de solicitudes pendientes
- Cada pin contiene: precio, origen, número de solicitud
- Tap en pin → Muestra opción para aceptar viaje

---

## 📊 Modelos de Datos

### SolicitudFleteDto
```csharp
{
    SolicitudFleteId,
    ClienteId,
    ClienteNombre,
    ConductorId,        // null hasta ser aceptada
    VehiculoId,         // null hasta ser aceptada
    DireccionOrigen,
    DireccionDestino,
    LatitudOrigen, LongitudOrigen,
    LatitudDestino, LongitudDestino,
    DistanciaKm,
    Precio,
    Estado,            // PENDIENTE, EN_CURSO, COMPLETADO, CANCELADA
    Cargas            // Lista de items
}
```

---

## 🔐 Seguridad

- **Autenticación**: Email + contraseña con almacenamiento en servidor
- **Sesión**: `SesionUsuario.cs` almacena datos en memoria durante la sesión
- **Preferences**: Almacenamiento seguro local de credenciales
- **Roles**: Distinción entre "CLIENTE" y "CONDUCTOR"

---

## 🚀 Configuración por Entorno

**DEBUG** (Local):
```csharp
const string ApiUrl = "http://10.0.2.2:5051/api/";
```

**RELEASE** (Producción):
```csharp
const string ApiUrl = "https://mueveya.onrender.com/api/";
```

---

## 📝 Próximos Pasos / Mejoras Planeadas

- [ ] Historial de viajes del cliente
- [ ] Sistema de reseñas y calificaciones
- [ ] Seguimiento en vivo del conductor
- [ ] Métodos de pago integrados
- [ ] Chat en tiempo real entre cliente y conductor
- [ ] Promociones y códigos de descuento
- [ ] Push notifications
- [ ] Interfaz de configuración de perfil

---

## 🛠️ Requisitos para Desarrollo

- Visual Studio 2026+ con MAUI workload
- .NET 10 SDK
- Android SDK (para emulador)
- API Backend ejecutándose

---

## 👨‍💻 Autor

**Damian Block** - Seminario Integrador - Carrera Analista en Sistemas

---

**Última actualización**: Enero 2025
