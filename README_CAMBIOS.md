# 🚚 MUEVE — Cotización de tarifa por tipo de vehículo

> Cambio: **cálculo de tarifa en el servidor, por tipo de vehículo**, con selección del vehículo en un *bottom sheet* que aparece después de elegir la carga.
> Fecha: 2026-10-09

---

## 1. Resumen

Antes, el cliente veía una lista de vehículos en la pantalla principal con un precio que **no coincidía** con el que cobraba el servidor:

- Las tarjetas pedían `Tarifas/opciones`, un endpoint que no existía en la API, y caían a un cálculo local (`50 + 12/km`).
- El servidor cobraba `1500 + 350/km + 15/kg` sin importar el vehículo.
- El vehículo elegido ni siquiera viajaba en la solicitud.

Ahora:

- El cliente arma su carga, toca **Calcular tarifa** y sube un *bottom sheet* con **todos los tipos de vehículo cotizados**.
- El precio depende de **distancia, peso de la carga, tipo de vehículo y tiempo de espera** (15 min de carga + 15 min de descarga).
- A mayor tamaño del vehículo, mayor tarifa.
- El sistema **sugiere** el vehículo más chico donde entra la carga y deshabilita los que no alcanzan.
- El precio que se muestra es **exactamente** el que se cobra: ambos salen de la misma calculadora del servidor.

---

## 2. Flujo del usuario

1. Elige **origen** y **destino**.
2. Toca **Elegir / editar carga** (opciones rápidas o detalle con `+` / `−`).
3. Toca **Calcular tarifa** → sube el sheet con los vehículos y sus precios.
4. Si el precio es alto o quiere quitar algo: **desliza la cabecera hacia abajo** (o toca ✕ o el fondo oscuro), edita la carga y el botón ahora dice **Recalcular tarifa**.
5. Elige un vehículo (viene uno preseleccionado) y toca **Confirmar flete** → se crea la solicitud con ese tipo de vehículo.

---

## 3. Arquitectura

```text
App (MAUI)                                   API (ASP.NET Core)
──────────                                   ──────────────────
HomeCliente ──(distancia + cargas)──► POST /api/Tarifas/cotizar
   │                                          │  TarifasController
   │                                          │  └─ CalculadoraTarifas (única fuente de precios)
   │ ◄──── CotizacionDto (5 opciones) ────────┘
   │
   └─ CotizacionViewModel (sheet)
        │
        └─(tipo elegido + cargas)──► POST /api/SolicitudesFlete
                                          └─ valida capacidad + recalcula precio con la misma calculadora
```

**Regla clave:** el precio nunca se calcula en la app. La app solo muestra lo que devuelve el servidor, y al crear la solicitud el servidor **vuelve a calcular** (no confía en el precio del cliente).

---

## 4. Tarifa

```text
precio = TarifaBase(tipo)
       + distanciaKm × PrecioPorKm(tipo)
       + pesoTotalKg × 15
       + 30 min de espera × PrecioMinutoEspera(tipo)

si el servicio es Programado → precio × 1.15
resultado redondeado a pesos enteros
```

Valores actuales (**de ejemplo, en ARS**; se ajustan en `AppFletesMueve.Api/Services/CalculadoraTarifas.cs`):

| Tipo | Capacidad | Base | $/km | $/min espera |
|---|---|---|---|---|
| Utilitario | 500 kg · 2 m³ | 1.500 | 350 | 40 |
| Camioneta / Pick-up | 1.000 kg · 5 m³ | 2.200 | 450 | 55 |
| Camión chico | 2.500 kg · 12 m³ | 3.500 | 600 | 80 |
| Camión mediano | 5.000 kg · 25 m³ | 5.000 | 800 | 110 |
| Camión grande / Mudanza | 10.000 kg · 45 m³ | 7.500 | 1.100 | 150 |

Un vehículo **"entra"** si `pesoTotal ≤ capacidadKg` **y** `volumenTotal ≤ capacidadM3`. El peso y el volumen salen de `TiposCarga` (estimados por unidad) multiplicados por la cantidad.

---

## 5. Contrato de la API

### `POST /api/Tarifas/cotizar` *(nuevo)*

Request:

```json
{
  "tipoServicio": "Inmediato",
  "distanciaKm": 5.5,
  "cargas": [
    { "tipoCargaId": 1, "cantidad": 3 },
    { "tipoCargaId": 3, "cantidad": 5 }
  ]
}
```

Response (recortada):

```json
{
  "pesoTotalKg": 195,
  "volumenTotalM3": 2.9,
  "minutosEspera": 30,
  "opciones": [
    { "tipoVehiculo": "Utilitario", "nombre": "Utilitario", "precio": 7550,
      "capacidadKg": 500, "capacidadM3": 2, "entra": false, "sugerido": false },
    { "tipoVehiculo": "Camioneta", "nombre": "Camioneta / Pick-up", "precio": 9250,
      "capacidadKg": 1000, "capacidadM3": 5, "entra": true, "sugerido": true }
  ]
}
```

### `POST /api/SolicitudesFlete` *(modificado)*

- Nuevo campo requerido en el body: `tipoVehiculo` (`Utilitario`, `Camioneta`, `CamionChico`, `CamionMediano`, `CamionGrande`).
- Valida que la carga entre en el vehículo (si no, `400` con `"La carga no entra en el vehículo elegido"`).
- Calcula el precio con el tipo de vehículo elegido.
- `SolicitudFleteDto` ahora devuelve `tipoVehiculo`.

---

## 6. Archivos modificados

### API — `AppFletesMueve.Api`

| Archivo | Cambio |
|---|---|
| `Models/SolicitudFlete.cs` | Nueva propiedad `TipoVehiculo` (tipo pedido por el cliente). |
| `Data/MueveDbContext.cs` | Mapeo de `TipoVehiculo` como texto (máx. 30) con default `Utilitario` para solicitudes existentes. |
| `Dtos/TransporteDtos.cs` | `TipoVehiculo` en `CrearSolicitudFleteDto` y `SolicitudFleteDto`; nuevos `CotizarTarifaDto`, `OpcionTarifaDto`, `CotizacionDto`. |
| `Services/CalculadoraTarifas.cs` | Reescrita: perfiles por tipo (capacidad y tarifas), espera de 15+15 min, redondeo a pesos enteros. |
| `Controllers/TarifasController.cs` | **Nuevo.** Endpoint `POST cotizar`. |
| `Controllers/SolicitudesFleteController.cs` | `Crear` valida capacidad y usa la calculadora con el tipo de vehículo; persiste y devuelve `TipoVehiculo`. |
| `Migrations/*_TipoVehiculoSolicitud.cs` | **Nueva migración.** Se aplica sola al arrancar la API (`db.Database.Migrate()`). |

### App — `AppFletesMueve`

| Archivo | Cambio |
|---|---|
| `Services/TransporteService.cs` | `TipoVehiculo` en `CrearSolicitudFleteRequest` y `SolicitudFleteDto`; nuevas clases `CotizarTarifaRequest`, `OpcionTarifaDto`, `CotizacionDto`; nuevo método `CotizarAsync`. |
| `Models/ObservableBase.cs` | **Nuevo.** Base con `INotifyPropertyChanged`. |
| `Models/CotizacionVehiculo.cs` | **Nuevo.** Opción de vehículo mostrada en el sheet (precio, capacidad, estado de selección, colores). |
| `ViewModels/CotizacionViewModel.cs` | **Nuevo.** Estado del sheet: opciones, selección, resumen del viaje, botón de confirmar. |
| `Views/HomeCliente.xaml` | Se quitó el bloque "Selecciona tu vehículo" y el botón de confirmar viejo; se agregó resumen de carga, botón **Calcular tarifa** y el *bottom sheet* con fondo oscuro. |
| `Views/HomeCliente.xaml.cs` | Flujo calcular → sheet → recalcular → confirmar; carga actual pasada a la pantalla de carga. |
| `Views/SeleccionarCargaPage.xaml.cs` | Recibe la carga actual para poder **editarla** sin empezar de cero (abre el detalle con las cantidades previas). |
| `Views/SeleccionarCargaPage.xaml` | `TextColor="Black"` en la cantidad (visible en modo oscuro). |

---

## 7. Bugs corregidos

- **Precio mostrado ≠ precio cobrado.** Ahora hay una sola calculadora, en el servidor.
- **El vehículo elegido no viajaba en la solicitud.** Ahora se envía `TipoVehiculo` y el servidor lo valida.
- **`MainViewModel.ConfirmarFleteCommand`** (botón de confirmar viejo) enviaba una solicitud con **dirección y coordenadas fijas** ("Av. Siempre Viva 123"). El botón se eliminó de `HomeCliente`.
- **`SugerenciaDestino_Tapped`** escribía el destino elegido en el campo **origen** (copy/paste del handler de origen). Corregido.
- **Cantidades de carga perdidas al reabrir** `SeleccionarCargaPage`. Ahora se restauran.
- Etiquetas de moneda "MXN" reemplazadas por formato `$ N0` en pesos.

---

## 8. Paleta de la UI

| Color | Hex | Uso |
|---|---|---|
| Naranja | `#E65100` | Marca, **Calcular tarifa**, vehículo seleccionado |
| Negro | `#212121` | Títulos y precios |
| Gris | `#757575` / `#E0E0E0` | Textos secundarios y bordes |
| Blanco | `#FFFFFF` | Fondos |
| Verde | `#2E7D32` | **Confirmar flete** y etiqueta "Recomendado" |

---

## 9. Cómo probar

1. Levantar la API (aplica la migración sola):

   ```powershell
   cd AppFletesMueve.Api
   dotnet run
   ```

2. Probar la cotización desde PowerShell:

   ```powershell
   $body = '{"tipoServicio":"Inmediato","distanciaKm":5.5,"cargas":[{"tipoCargaId":1,"cantidad":3},{"tipoCargaId":3,"cantidad":5}]}'
   Invoke-RestMethod -Method Post -Uri "http://localhost:5051/api/Tarifas/cotizar" -ContentType "application/json" -Body $body | ConvertTo-Json -Depth 5
   ```

   Esperado: 5 opciones; Utilitario con `entra: false`; Camioneta con `sugerido: true`.

3. Ejecutar la app en el emulador Android y recorrer el flujo de la sección 2. Verificar que el total de la alerta final coincide con el del sheet.

Si se cambian las tarifas o capacidades, basta con editar `CalculadoraTarifas.cs`: el sheet y las solicitudes se actualizan solos.

---

## 10. Pendientes y mejoras sugeridas

- **Distancia:** hoy la calcula la app en línea recta (Haversine) y el servidor confía en ella. Conviene calcularla en el servidor con una API de rutas (Google Directions) para que sea la distancia real y no manipulable.
- **Disponibilidad:** el sheet muestra los 5 tipos aunque no haya conductores cerca. Se puede cruzar con `Vehiculos/disponibles` para marcar "sin conductores ahora".
- **Conductor:** validar en `Aceptar` que `vehiculo.TipoVehiculo == solicitud.TipoVehiculo` y filtrar las pendientes del conductor por el tipo de su vehículo.
- **Limpieza:** `MainViewModel` ya no se usa en `HomeCliente` (conserva un request con datos fijos). También se pueden borrar `VehiculoOpcionDto`, `ObtenerOpcionesVehiculoAsync` y `CalcularPrecioEstimado` de `TransporteService` si todavía están.
- **Servicio programado:** la app siempre cotiza `Inmediato`; la API ya soporta `Programado` (recargo 15 %).
- **Roadmap existente:** seguimiento GPS en tiempo real, historial de viajes, notificaciones/OTP y pasarela de pagos (Mercado Pago) sobre esta tarifa.
