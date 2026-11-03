# 1. Contexto, arquitectura y diseño

Este capítulo reúne la descripción funcional y técnica del sistema: qué resuelve, cómo está organizado, qué decisiones sostienen su diseño y qué límites se conocen. Es la base para leer los capítulos de instalación, uso y operación.

## 1.1 Resumen ejecutivo

**Cafetería Leblanc POS** es una aplicación de escritorio para Windows que integra la operación diaria de una cafetería con un asistente conversacional local. El sistema permite iniciar sesión por empleado, registrar ventas con propina y cambio, descontar inventario, administrar productos, categorías y personal, consultar reportes del día y de la última semana, exportar información y preguntar a **Morgana** por precios, stock, ventas, propinas u horarios en lenguaje natural.

La aplicación se construyó con **C# 12**, **WinUI 3 / Windows App SDK 2.5.1**, **.NET 8** y **CommunityToolkit.Mvvm 8.4.2**. La persistencia es un archivo **JSON local** (`Documents\CafeteriaPOS\Data\db.json`) con escritura atómica y respaldo `.bak`. La IA se ejecuta **dentro del proceso** con **LLamaSharp 0.30.0** y un modelo **Qwen2.5-1.5B-Instruct** ajustado con **LoRA**, fusionado y cuantizado a **GGUF Q4_K_M** (~940 MB). No hay API, servidor, Ollama, LM Studio ni Python en tiempo de ejecución.

| Indicador | Valor |
|---|---|
| Versión documentada | 1.0.0 |
| Plataforma | Windows 10 1809+ / Windows 11, x64 |
| Lenguaje | C# 12 |
| Interfaz | WinUI 3, tema claro |
| Persistencia | JSON local + respaldo `.bak` |
| Modelo de IA | Qwen2.5-1.5B-Instruct + LoRA v2, GGUF Q4_K_M |
| Tamaño del modelo | 986,048,160 bytes (~940 MiB) |
| Entrenamiento | 150 conversaciones, 2 épocas, 76 pasos, 41 min 52 s |
| Pérdida final (paso 76) | 0.5717 |
| Precisión de token | 85.04 % |
| Compilación verificada | 0 errores; 52 advertencias no bloqueantes |

El proyecto cubre el ciclo completo de una caja pequeña y deja una base mantenible: separación MVVM, servicios de dominio, persistencia centralizada, validaciones de entrada, reportes exportables y un asistente con límites explícitos. Las secciones siguientes detallan la arquitectura, la instalación, el manual de usuario, el entrenamiento de Morgana y las decisiones técnicas que explican el resultado.

## 1.2 Descripción general

### 1.2.1 Qué es el proyecto

Cafetería Leblanc POS es un **sistema de punto de venta (POS)** de escritorio para una cafetería. Su nombre y estética están inspirados en el Café Leblanc de *Persona 5*; la aplicación es un proyecto académico y no comercial.

La aplicación concentra en una sola ventana:

- autenticación de empleados;
- catálogo de productos y categorías;
- carrito de venta con cantidades, propina y pago;
- descuento de stock y alertas de inventario;
- generación de tickets en texto plano;
- reportes de operación y exportaciones;
- asistente de IA local.

### 1.2.2 Propósito

El propósito es ofrecer una herramienta **sencilla, rápida y autónoma** para la operación de una cafetería pequeña, sin depender de internet ni de servicios en la nube. La IA se incorpora para reducir el tiempo de consulta de datos: en lugar de navegar entre pantallas, el usuario puede preguntar “¿cuánto vendimos hoy?” o “¿cuánto stock queda de capuchino?” y recibir una respuesta con las cifras reales del negocio.

El sistema parte de tres principios:

1. **Los datos mandan.** La IA nunca inventa precios, stock ni ventas; el código sustituye los valores reales.
2. **La operación no se bloquea.** Si el modelo falta o falla, el POS sigue funcionando y el asistente responde con un mensaje seguro.
3. **La instalación es simple.** Un ejecutable, una carpeta de datos y un archivo de modelo; no hay base de datos que administrar.

### 1.2.3 Casos de uso

| Caso de uso | Actor | Entradas | Resultado |
|---|---|---|---|
| Iniciar sesión | Administrador / Cajero | Usuario y contraseña | Sesión activa con rol |
| Registrar venta | Cajero | Productos, cantidades, propina, pago | Venta, detalle, stock y ticket |
| Calcular cambio | Cajero | Total y efectivo recibido | Cambio mostrado y guardado en ticket |
| Consultar catálogo | Cajero | Búsqueda o categoría | Productos filtrados con precio y stock |
| Administrar producto | Administrador | Nombre, categoría, precio, stock | Producto creado/actualizado/eliminado |
| Administrar categoría | Administrador | Nombre | Categoría creada/renombrada/eliminada |
| Administrar empleado | Administrador | Nombre, usuario, contraseña, puesto | Empleado creado/actualizado/eliminado |
| Consultar reportes | Administrador | Fecha actual | Métricas, top y propinas |
| Exportar reporte | Administrador | Ruta y formato | CSV o JSON |
| Consultar a Morgana | Administrador / Cajero | Pregunta en lenguaje natural | Respuesta con datos reales o fallback |
| Cerrar sesión | Cualquier rol | — | Vuelta al login y cierre del asistente |

### 1.2.4 Alcance y límites

**Incluido en esta versión**

- Una caja por instalación y una base local.
- Catálogo con categorías independientes.
- Ventas con propina, efectivo/tarjeta y ticket.
- Reportes del día y ventana móvil de siete días.
- Exportación CSV/JSON.
- Asistente local con 18 comodines de datos (más `MENU` como clave de control) y respaldo determinista.

**Fuera de alcance**

- Múltiples sucursales o sincronización entre equipos.
- Facturación fiscal (CFDI), notas de crédito o devoluciones.
- Integración con terminal bancaria o lectores de código.
- Compras, proveedores, nómina, reservaciones o fidelización.
- Combos, descuentos automáticos o promociones.
- Entrenamiento del modelo desde la app.

### 1.2.5 Usuarios y roles

| Rol | Descripción | Permisos |
|---|---|---|
| **Admin** | Encargado o administrador | Punto de venta, productos, categorías, empleados, reportes, asistente y cierre de sesión |
| **Cajero** | Personal de caja | Punto de venta, asistente y cierre de sesión |

El rol se guarda como texto (`"Admin"` o `"Cajero"`) en `Empleado.Puesto`. La navegación oculta las secciones administrativas al cajero y el código vuelve a comprobar el rol antes de navegar. La sesión vive solo en memoria (`SessionService`), por lo que al cerrar la aplicación se pierde.

### 1.2.6 Experiencia de uso

La interfaz sigue un flujo lineal:

```text
Login → Punto de venta → Carrito → Propina → Pago → Cobrar → Ticket
```

El menú lateral agrupa las funciones:

- **Punto de venta:** operación de caja.
- **Productos:** catálogo e inventario.
- **Empleados:** personal y credenciales.
- **Reportes:** métricas y exportaciones.
- **Asistente virtual:** ventana flotante de Morgana.
- **Cerrar sesión:** fin de turno.

La paleta usa marrón café, crema y blanco; el tema claro está forzado para mantener el contraste. Los mensajes de éxito son verdes y los de error o advertencia, rojos.

## 1.3 Arquitectura y tecnologías

### 1.3.1 Stack tecnológico

| Componente | Tecnología | Versión | Función |
|---|---|---|---|
| Lenguaje | C# | 12 | Lógica y modelos |
| Runtime | .NET | 8.0 | Ejecución |
| Framework de UI | WinUI 3 / Windows App SDK | 2.5.1 | Ventanas, páginas y controles |
| MVVM | CommunityToolkit.Mvvm | 8.4.2 | `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` |
| Serialización | System.Text.Json | .NET 8 | `db.json` y exportación JSON |
| IA | LLamaSharp + Backend.Cpu | 0.30.0 | Carga del GGUF e inferencia |
| Modelo | Qwen2.5-1.5B-Instruct + LoRA | v2 | Redacción de Morgana |
| Empaquetado | Windows App SDK, MSIX opcional | — | Distribución |
| Pruebas | Manuales + verificación de build | — | Validación funcional |

### 1.3.2 Vista general

La arquitectura se organiza en tres capas y un subsistema de IA:

![Figura 1.1. Arquitectura por capas de Cafetería Leblanc POS](recursos/arquitectura.png){width=16cm}

| Capa | Componentes | Responsabilidad |
|---|---|---|
| Presentación | `Views`, `ViewModels`, `Helpers` | Mostrar, capturar, validar y dar retroalimentación |
| Dominio | `Services` (ventas, productos, empleados, categorías, reportes) | Reglas de negocio y operaciones |
| Infraestructura | `JsonDatabase`, `CsvService`, `TicketService`, `AppPaths` | Archivos, formato y rutas |
| IA | `MorganaService`, `ComodinResolver` | Inferencia local y sustitución segura |

### 1.3.3 Capa de presentación

**Views (XAML + code-behind).** Cada pantalla declara la interfaz con `x:Bind` y eventos mínimos. El code-behind se ocupa de lo que depende del control: clics, foco, teclado, navegación y apertura de ventanas.

**ViewModels.** Concentran el estado y los comandos:

| ViewModel | Estado principal | Comandos |
|---|---|---|
| `LoginViewModel` | Usuario, mensaje de error | `IntentarLogin` |
| `SalesViewModel` | Búsqueda, categoría, carrito, subtotal, propina, total, pago, cambio | Agregar, aumentar, disminuir, quitar, vaciar, propina %, cobrar |
| `ProductsViewModel` | Filtros, formulario, categorías, selección | Nuevo, guardar, eliminar, cancelar, CRUD de categorías |
| `EmployeesViewModel` | Filtro, formulario, selección | Nuevo, guardar, eliminar, cancelar |
| `ReportsViewModel` | Métricas, top, propinas, últimos 7 días | Cargar, exportar CSV, exportar JSON |
| `MainViewModel` | Nombre, puesto y visibilidad | — (solo lectura) |

**Helpers.**

- `MoneyConverter`: formatea `decimal` como moneda `es-MX` con dos decimales.
- `MessageBrushConverter`: verde para mensajes que empiezan con `✅` o `🗑`, rojo para el resto.
- `NumericInput` / `NumericNumberBox`: restringen la entrada a dígitos y decimales, sin signos ni letras.
- `AppPaths`: resuelve las carpetas de datos, tickets y modelos.
- `WindowIcon`: aplica el icono a la ventana.

### 1.3.4 Capa de dominio

Los servicios encapsulan las operaciones que no deberían vivir en la UI:

| Servicio | Responsabilidad |
|---|---|
| `ProductoService` | Listar, guardar, generar el siguiente id y descontar stock |
| `VentaService` | Registrar venta + detalle + stock en una operación atómica; generar `IdVenta` |
| `EmpleadoService` | Validar credenciales, listar, guardar, generar id y comprobar usuario único |
| `CategoriaService` | Listar, guardar, renombrar (con cascada a productos) y eliminar |
| `TicketService` | Generar el ticket de texto de 42 columnas |
| `CsvService` | Leer/escribir CSV con escape de comas, comillas y saltos |
| `SessionService` | Mantener el empleado logueado en memoria |
| `ReportsViewModel` | Agregar métricas, top, propinas y últimos 7 días |

### 1.3.5 Infraestructura y persistencia

`JsonDatabase` es la fachada única al archivo:

- Define `BaseDatos` con listas de productos, categorías, empleados, ventas y detalles.
- Serializa en **camelCase**, con indentación y caracteres Unicode sin escapar.
- Usa un `lock` para serializar lecturas y escrituras.
- Ofrece `Leer()`, `Guardar()`, `Actualizar(Action<BaseDatos>)` y `AsegurarBase()`.
- Escribe un archivo temporal y reemplaza el original para evitar corrupción por corte.

Las rutas se resuelven con `Environment.SpecialFolder.MyDocuments`, por lo que funcionan aunque Documentos esté redirigido a OneDrive.

### 1.3.6 Subsistema de IA

El asistente se compone de dos piezas:

1. **`MorganaService`**: singleton perezoso que carga el GGUF, construye el prompt ChatML, ejecuta la inferencia con LLamaSharp y serializa las preguntas con un `SemaphoreSlim`.
2. **`ComodinResolver`**: valida la salida del modelo contra una lista blanca de 19 claves (los 18 comodines del dataset más `MENU` como clave de control), sustituye los valores reales y rechaza cualquier respuesta con comodines desconocidos o mal formados.

Antes de llamar al modelo, el servicio detecta intenciones de datos puros (menú, recaudado, propinas, conteo de tickets, top de productos) y las responde directamente con código. Esto evita que un modelo de 1.5B enumere precios o cuente ventas de forma inconsistente.

### 1.3.7 Patrones de diseño

- **MVVM:** separación entre vista, estado y comandos.
- **Service Layer:** los servicios concentran reglas de negocio.
- **Facade:** `JsonDatabase` oculta la complejidad de leer/escribir el archivo.
- **Singleton con inicialización perezosa:** una sola instancia del modelo para toda la app.
- **Strategy / fallback:** la respuesta puede venir del modelo o de una frase determinista.
- **Converter:** adaptación de datos a la presentación (moneda, color).
- **Template Method en la UI:** XAML define la estructura; el code-behind solo completa eventos.

### 1.3.8 Concurrencia, errores y ciclo de vida

- **Persistencia:** el candado de `JsonDatabase` impide que dos ventas simultáneas se pisen. `Actualizar` lee, modifica y escribe dentro del mismo bloqueo.
- **IA:** `MorganaService` usa un semáforo para que solo haya una inferencia a la vez; la ventana del chat deshabilita el botón Enviar mientras espera.
- **Carga del modelo:** se precalienta en segundo plano al abrir el chat; la primera pregunta no bloquea la UI.
- **Errores de IA:** se registran en `Data\morgana_error.log` y se devuelve un mensaje seguro.
- **Ciclo de vida:** `App.OnLaunched` crea carpetas, asegura la base y abre `MainWindow`; al cerrar la ventana principal se cierra la ventana del asistente.
- **Sesión:** `SessionService` se limpia al cerrar sesión; no hay persistencia de sesión entre ejecuciones.

### 1.3.9 Consideraciones de seguridad de la arquitectura

- La aplicación es **local y monousuario**; no expone puertos ni endpoints.
- El modelo y los datos no salen del equipo.
- Las contraseñas se guardan en texto plano en `db.json`: es una decisión aceptable para una demo, pero **no para producción**. La sección de seguridad propone hash + salt.
- El rol se valida en la UI y en el código de navegación; no hay un servidor que imponga permisos, por lo que un usuario con acceso al archivo podría modificarlo.

## 1.4 Estructura del proyecto

### 1.4.1 Árbol de directorios

```text
CompartirVCCafe/
├─ CafeteriaPOS.slnx
├─ README.md
├─ CafeteriaPOS/
│  ├─ App.xaml
│  ├─ App.xaml.cs
│  ├─ MainWindow.xaml
│  ├─ MainWindow.xaml.cs
│  ├─ Package.appxmanifest
│  ├─ app.manifest
│  ├─ CafeteriaPOS.csproj
│  ├─ Assets/
│  ├─ Helpers/
│  ├─ Models/
│  ├─ Services/
│  ├─ ViewModels/
│  ├─ Views/
│  └─ Properties/
│     ├─ launchSettings.json
│     └─ PublishProfiles/
├─ docs/
│  ├─ README.md
│  ├─ 01-descripcion-arquitectura.md
│  ├─ 02-instalacion.md
│  ├─ 03-manual-usuario.md
│  ├─ 04-morgana-ia.md
│  ├─ 05-operacion-mantenimiento.md
│  └─ recursos/
└─ Documentacion/
   ├─ CafeteriaPOS_Documentacion_Tecnica.docx
   ├─ CafeteriaPOS_Documentacion_Tecnica.pdf
   └─ recursos/
```

### 1.4.2 Responsabilidad de cada carpeta

| Ruta | Responsabilidad | Archivos clave |
|---|---|---|
| `CafeteriaPOS/` | Proyecto WinUI 3 | `.csproj`, `App.xaml`, `MainWindow` |
| `Assets/` | Iconos, splash y avatar | `AppIcon.ico`, `morgana.png` |
| `Helpers/` | Utilidades transversales | `AppPaths`, `MoneyConverter`, `NumericInput` |
| `Models/` | Entidades y modelos de vista | `Producto`, `Venta`, `Empleado`, `ChatMessage` |
| `Services/` | Reglas de negocio y persistencia | `JsonDatabase`, `VentaService`, `MorganaService` |
| `ViewModels/` | Estado y comandos | `SalesViewModel`, `ReportsViewModel` |
| `Views/` | Páginas y ventanas | `LoginPage`, `SalesPage`, `AiChatWindow` |
| `Properties/` | Perfiles de ejecución y publicación | `launchSettings.json`, `win-x64.pubxml` |
| `docs/` | Wiki técnica y fuentes del documento | Markdown + imágenes |
| `Documentacion/` | Entregables Word/PDF | `.docx`, `.pdf`, recursos |

### 1.4.3 Archivos principales

| Archivo | Qué hace |
|---|---|
| `App.xaml` | Recursos globales, tema claro y conversores |
| `App.xaml.cs` | Crea carpetas, asegura la base y abre la ventana |
| `MainWindow.xaml.cs` | Tamaño inicial, icono y barra de título |
| `MainPage.xaml(.cs)` | `NavigationView`, permisos por rol y ventana del asistente |
| `LoginPage.xaml(.cs)` | Formulario de acceso y validación de letras |
| `SalesPage.xaml(.cs)` | Catálogo, carrito, propina, pago y cobro |
| `ProductsPage.xaml(.cs)` | Lista, formulario y administración de categorías |
| `EmployeesPage.xaml(.cs)` | Lista y formulario de personal |
| `ReportsPage.xaml(.cs)` | Métricas y exportaciones |
| `AiChatWindow.xaml(.cs)` | Chat flotante de Morgana |
| `JsonDatabase.cs` | Lectura/escritura atómica y respaldo |
| `CafeteriaPOS.csproj` | Target, paquetes, plataformas y RID |

### 1.4.4 Flujo de navegación

```text
App.OnLaunched
  → EnsureFolders + AsegurarBase
  → MainWindow
  → LoginPage
      → (credenciales válidas) → MainPage
          → SalesPage (inicio)
          → ProductsPage      (solo Admin)
          → EmployeesPage     (solo Admin)
          → ReportsPage       (solo Admin)
          → AiChatWindow      (ventana flotante)
          → Logout → LoginPage
```

La navegación usa un `Frame` dentro de `NavigationView`. El ítem seleccionado se mantiene resaltado; el asistente se abre como ventana independiente para no perder el contexto de la caja.

## 1.5 Modelo de datos y persistencia

### 1.5.1 Entidades

| Entidad | Campos | Notas |
|---|---|---|
| `Producto` | `Id`, `Nombre`, `Categoria`, `Precio`, `Stock`, `StockMinimo` | `StockBajo` y `Disponible` son calculados y no se serializan |
| `Categoria` | Texto | Lista independiente para permitir categorías vacías |
| `Empleado` | `Id`, `Nombre`, `Usuario`, `Password`, `Puesto` | `Puesto`: `Admin` o `Cajero` |
| `Venta` | `IdVenta`, `FechaHora`, `IdEmpleado`, `NombreEmpleado`, `Subtotal`, `Propina`, `Total`, `MetodoPago` | `MetodoPago`: `Efectivo` o `Tarjeta` |
| `DetalleVenta` | `IdVenta`, `IdProducto`, `NombreProducto`, `Cantidad`, `PrecioUnitario`, `Subtotal` | Guarda nombre y precio históricos |
| `BaseDatos` | `Productos`, `Categorias`, `Empleados`, `Ventas`, `DetalleVentas` | Contenedor raíz de `db.json` |

### 1.5.2 Relaciones

| Relación | Cardinalidad | Clave | Comportamiento |
|---|---|---|---|
| Empleado → Venta | 1:N | `Venta.IdEmpleado` | Una venta conserva el id y nombre del empleado |
| Venta → DetalleVenta | 1:N | `DetalleVenta.IdVenta` | El detalle hereda el id de la venta |
| Producto → DetalleVenta | 1:N | `DetalleVenta.IdProducto` | El detalle conserva nombre y precio del momento |
| Categoría → Producto | 1:N | `Producto.Categoria` (texto) | Renombrar la categoría actualiza los productos |

La relación por nombre de categoría es deliberada: permite que una categoría exista sin productos y que el catálogo sea legible a simple vista. La desventaja es que el renombrado debe ser transaccional, y `CategoriaService.Renombrar` lo hace dentro de una sola escritura.

### 1.5.3 Esquema de `db.json`

Ejemplo reducido (los datos reales pueden variar):

```json
{
  "productos": [
    {
      "id": 1,
      "nombre": "Café Americano",
      "categoria": "Bebidas",
      "precio": 25,
      "stock": 20,
      "stockMinimo": 5
    }
  ],
  "categorias": ["Bebidas", "Panadería"],
  "empleados": [
    {
      "id": 1,
      "nombre": "Administrador",
      "usuario": "admin",
      "password": "1234",
      "puesto": "Admin"
    }
  ],
  "ventas": [
    {
      "idVenta": "V-20261101-101500-123",
      "fechaHora": "2026-11-01T10:15:00",
      "idEmpleado": 1,
      "nombreEmpleado": "Administrador",
      "subtotal": 65,
      "propina": 6.5,
      "total": 71.5,
      "metodoPago": "Efectivo"
    }
  ],
  "detalleVentas": [
    {
      "idVenta": "V-20261101-101500-123",
      "idProducto": 1,
      "nombreProducto": "Café Americano",
      "cantidad": 2,
      "precioUnitario": 25,
      "subtotal": 50
    }
  ]
}
```

**Notas de serialización**

- `PropertyNamingPolicy = CamelCase`: `IdVenta` se guarda como `idVenta`.
- `PropertyNameCaseInsensitive = true`: tolera diferencias de mayúsculas al leer.
- `WriteIndented = true`: el archivo es legible y diffeable.
- `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`: conserva tildes y signos como `$` sin escapes.
- `UTF-8` sin BOM al escribir; `UTF-8` al leer.

### 1.5.4 Operaciones y atomicidad

#### Lectura

`JsonDatabase.Leer()` toma el candado y llama a `LeerSinBloqueo()`:

1. Si el archivo no existe, devuelve una base vacía.
2. Si está vacío, devuelve una base vacía.
3. Deserializa con las opciones configuradas; si el resultado es `null`, devuelve una base vacía.

#### Escritura

`Guardar()` y `Actualizar()` toman el candado y llaman a `EscribirSinBloqueo()`:

1. Crea el directorio de datos si no existe.
2. Serializa la base completa.
3. Escribe `db.json.tmp`.
4. Si `db.json` existe, lo copia a `db.json.bak`.
5. Mueve el `.tmp` sobre `db.json`.

La operación `Actualizar(Action<BaseDatos>)` es la recomendada para ventas y stock porque lee, modifica y escribe dentro del mismo bloqueo.

#### Arranque

`AsegurarBase()` crea una base mínima solo si `db.json` no existe:

- `admin` / `1234` con puesto `Admin`;
- Café Americano, Capuchino y Croissant.

No sobrescribe una base existente.

### 1.5.5 Tickets

`TicketService` genera un archivo de texto de 42 columnas:

```text
        ☕ CAFETERÍA LEBLANC
           Ticket de venta
==========================================
Ticket:  V-20261101-101500-123
Fecha:   01/11/2026 10:15
Cajero:  Administrador
------------------------------------------
Cant  Producto                Importe
------------------------------------------
2     Café Americano              50.00
1     Capuchino                  40.00
------------------------------------------
Subtotal:                        90.00
Propina:                          9.00
TOTAL:                           99.00
Método:  Efectivo
Paga con:                        100.00
Cambio:                            1.00
==========================================
       Gracias por su compra!
```

- Ruta: `Documents\CafeteriaPOS\Tickets\<IdVenta>.txt`.
- Codificación: UTF-8 con BOM.
- Si el método es efectivo y se capturó “paga con”, incluye cambio.
- El ticket no se vuelve a leer como base; es un comprobante.

### 1.5.6 Configuración

`Documents\CafeteriaPOS\Data\configuracion.json` es opcional:

```json
{
  "horario": "8:00 a 20:00",
  "pagos": "efectivo y tarjeta"
}
```

`ComodinResolver` lo lee para las preguntas de horario y pagos; si falta o es inválido, usa los valores por defecto. No se escribe desde la UI en esta versión.

### 1.5.7 Exportaciones

| Formato | Contenido | Codificación | Uso |
|---|---|---|---|
| CSV | Últimos 7 días + propinas por empleado | UTF-8 con BOM | Excel / Power BI |
| JSON | Resumen de hoy, top, propinas y 7 días | UTF-8 sin BOM | Integraciones / respaldo analítico |

El CSV usa `CsvService.WriteAll`, que escapa comas, comillas y saltos de línea. El JSON se serializa con indentación y `UnsafeRelaxedJsonEscaping` para conservar acentos.

### 1.5.8 Rutas y ciclo de vida

| Ruta | Contenido | Se crea |
|---|---|---|
| `Documents\CafeteriaPOS\Data\db.json` | Base principal | Al arrancar, si no existe |
| `Documents\CafeteriaPOS\Data\db.json.bak` | Versión anterior | En cada escritura |
| `Documents\CafeteriaPOS\Data\db.json.tmp` | Archivo temporal | Durante la escritura |
| `Documents\CafeteriaPOS\Data\configuracion.json` | Horario y pagos | Manual/opcional |
| `Documents\CafeteriaPOS\Tickets\` | Tickets `.txt` | Al cobrar |
| `Documents\CafeteriaPOS\Modelos\morgana.gguf` | Modelo de IA | Manual |
| `Documents\CafeteriaPOS\Data\morgana_error.log` | Errores de IA | Si ocurre un error |

## 1.6 Decisiones de diseño y notas técnicas

### 1.6.1 JSON en lugar de SQL

**Decisión:** un solo archivo JSON como fuente de verdad.

**Motivo:** una cafetería pequeña no necesita un servidor de base de datos; el JSON simplifica la instalación, el respaldo y la inspección manual. El acceso se concentra en `JsonDatabase`, de modo que migrar a SQLite en el futuro no obligaría a reescribir los ViewModels.

**Costo:** no hay concurrencia multiusuario ni consultas complejas. Para varias cajas o sucursales, SQLite o PostgreSQL serían mejores opciones.

### 1.6.2 MVVM con code-behind mínimo

**Decisión:** usar `ObservableObject`, `[ObservableProperty]` y `[RelayCommand]`; dejar en el code-behind solo eventos de control.

**Motivo:** el estado y las validaciones se prueban con más facilidad; la vista queda declarativa. Los eventos de `PasswordBox`, `NumberBox` y navegación no se pueden bindear de forma directa, por eso permanecen en code-behind.

### 1.6.3 Venta atómica y stock

**Decisión:** registrar venta, detalle y descuento de stock en una sola operación.

**Motivo:** evitar ventas sin stock descontado o stock descontado sin venta. `VentaService.RegistrarVenta` genera el id, asigna el mismo id a los detalles y llama a `JsonDatabase.Actualizar` una vez.

**Regla de negocio:** el carrito no permite agregar más unidades que el stock disponible; el botón de producto se deshabilita si `Stock == 0`.

### 1.6.4 Dinero y redondeo

**Decisión:** usar `decimal` para precios, subtotales, propinas y totales; formatear con `es-MX` y dos decimales.

**Motivo:** `double` introduce errores de punto flotante en operaciones monetarias. La UI captura `double` por las limitaciones de `NumberBox`, pero el ViewModel convierte a `decimal` con `ADecimal`, que trata `NaN` e infinito como cero.

**Propina:** los botones 5/10/15 % redondean a dos decimales con `Math.Round`; el monto libre también se redondea al mostrarse.

### 1.6.5 Propinas y reportes

**Decisión:** el total de una venta incluye la propina; el “total vendido” del reporte usa el subtotal y “ventas” usa el total.

**Motivo:** separar la venta del negocio de la propina del empleado. Las propinas se desglosan por método de pago (efectivo/tarjeta) y por empleado. El promedio de ticket se calcula sobre el total con propinas, coherente con el dinero cobrado.

### 1.6.6 IA determinista para datos puros

**Decisión:** las preguntas de menú, recaudado, propinas, conteo de ventas y top de productos se responden con código, sin pasar por el modelo.

**Motivo:** un modelo de 1.5B puede enumerar mal un catálogo o confundir cifras. Para datos exactos, el código es más confiable y rápido. El modelo se reserva para redactar respuestas conversacionales y para preguntas de precio/stock/presupuesto, donde el resolvedor sustituye los valores.

### 1.6.7 Lista blanca de comodines

**Decisión:** solo se aceptan 19 claves (18 del dataset más `MENU`); cualquier otra invalida la respuesta.

**Motivo:** evitar que el modelo invente `«VENTAS»`, `«PRECIO_DESCUENTO»` o `«STOCK_RAMEN»`. Si la salida es inválida, se reintenta una vez con otra semilla; si vuelve a fallar, se usa una frase determinista.

### 1.6.8 Tema claro y accesibilidad

**Decisión:** forzar `RequestedTheme="Light"` y usar una paleta café/crema.

**Motivo:** evitar que el tema oscuro del sistema produzca textos ilegibles sobre fondos personalizados. Los colores de estado (verde/rojo) y el formato de moneda mejoran la lectura. Los controles tienen tamaños táctiles razonables y etiquetas descriptivas.

### 1.6.9 Entrada numérica restringida

**Decisión:** `NumericInput` filtra el `TextBox` interno del `NumberBox`.

**Motivo:** `NumberBox` acepta signos, letras y notación científica según la configuración regional. El filtro limita dígitos enteros y decimales, bloquea `+`/`-` y corrige texto pegado. Se engancha en `Loaded`, `GotFocus` y `OnApplyTemplate` para sobrevivir a los cambios de plantilla.

### 1.6.10 Deuda técnica y riesgos

| Tema | Riesgo | Mitigación actual | Trabajo futuro |
|---|---|---|---|
| Contraseñas en texto plano | Alto | Ninguna | Hash + salt (PBKDF2/Argon2) |
| `db.json` corrupto | Alto | `.bak` manual | Fallback automático y archivo dañado con timestamp |
| Sin transacciones ACID | Medio | Candado + escritura atómica | SQLite |
| Modelo pequeño | Medio | Lista blanca + reintento + fallback | Dataset de validación y modelo mayor |
| Warnings MVVMTK0045 | Bajo | No bloquean | Migrar a propiedades parciales |
| Warnings WMC1506 | Bajo | Bindings funcionan | Cambiar a `OneTime` donde no cambie |
| Sin pruebas automatizadas | Medio | Checklist manual | xUnit + UI tests |
| Un solo equipo | Medio | Copia de `db.json` | Sincronización o servidor |

Estas limitaciones se detallan en el documento de operación y mantenimiento.
