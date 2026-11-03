# 2. Instalación y configuración

Esta sección explica cómo preparar un equipo para ejecutar Cafetería Leblanc POS, desde la compilación del código hasta la instalación del modelo de IA. Está pensada para que una persona con conocimientos básicos de Windows y línea de comandos pueda dejar el sistema funcionando sin depender de servicios externos.

> **Antes de empezar:** si solo necesitas usar la aplicación ya instalada, salta al apartado 2.9. Los apartados 2.4 a 2.8 son para desarrollo, soporte técnico o una instalación nueva.

## 2.1 Requisitos del sistema

### 2.1.1 Sistema operativo

| Elemento | Mínimo | Recomendado |
|---|---|---|
| Windows | Windows 10 versión 1809 (build 17763) | Windows 11 23H2 o superior |
| Arquitectura | x64 | x64 |
| Actualizaciones | .NET 8 y WebView2 al día | Igual |
| Idioma | Español (México) para el formato de moneda | Igual |

El proyecto declara `x86`, `x64` y `ARM64`. La ruta probada y documentada es **x64**. Las otras plataformas compilan, pero no se validaron con el modelo de IA en esta entrega.

### 2.1.2 Hardware

| Recurso | Mínimo | Recomendado |
|---|---|---|
| CPU | 2 núcleos con SSE3 | 4 núcleos con AVX2 |
| RAM | 4 GB | 8 GB o más |
| Disco libre | 1.5 GB (app + modelo) | 3 GB (margen para tickets y respaldos) |
| Pantalla | 1280 × 720 | 1920 × 1080 |
| GPU | No necesaria | No necesaria |

Morgana se ejecuta en CPU. La primera carga del modelo reserva alrededor de 1 GB de memoria; el resto de la aplicación funciona con mucho menos. Si el equipo tiene 4 GB y otras aplicaciones abiertas, la primera respuesta puede tardar más.

### 2.1.3 Software para desarrollo

| Herramienta | Versión | Uso |
|---|---|---|
| .NET SDK | 8.0 | Compilar y publicar |
| Visual Studio | 2022 17.13+ o 2026 | Editar, depurar y publicar con la interfaz |
| Carga de trabajo .NET Desktop | Incluida en VS | Compilación de escritorio |
| Carga de trabajo Windows App SDK | Incluida en VS | WinUI 3 y empaquetado |
| PowerShell | 5.1 o 7 | Comandos de esta guía |
| Git | Opcional | Clonar el repositorio |

Para compilar por línea de comandos no hace falta Visual Studio completo, pero sí el SDK de .NET 8 y los paquetes NuGet. La primera restauración requiere conexión a internet; después, la aplicación funciona sin red.

### 2.1.4 Requisitos de la IA

- Archivo `morgana.gguf` de **986,048,160 bytes** (~940 MiB).
- Carpeta `Documents\CafeteriaPOS\Modelos\`.
- CPU con AVX2 recomendado. LLamaSharp selecciona automáticamente el backend `avx512`, `avx2`, `avx` o `noavx` según el procesador.
- No se requiere Python, Ollama, LM Studio, GPU ni servidor.

## 2.2 Obtener el proyecto

### 2.2.1 Desde un repositorio

```powershell
git clone <URL-del-repositorio>
cd CompartirVCCafe
```

### 2.2.2 Desde una carpeta comprimida

1. Descomprimir el archivo en una ruta sin caracteres especiales; por ejemplo, `C:\Proyectos\CompartirVCCafe`.
2. Abrir PowerShell en esa carpeta.
3. Confirmar que existe `CafeteriaPOS.slnx`:

```powershell
Get-ChildItem
```

> Evita ejecutar la app desde una carpeta sincronizada por OneDrive mientras compilas. Los archivos `bin` y `obj` cambian con frecuencia y la sincronización puede bloquearlos.

## 2.3 Restaurar y compilar

### 2.3.1 Restauración

```powershell
dotnet restore CafeteriaPOS\CafeteriaPOS.csproj
```

La restauración descarga Windows App SDK, CommunityToolkit.Mvvm, LLamaSharp y el backend de CPU. Puede tardar varios minutos la primera vez.

### 2.3.2 Compilación de depuración

```powershell
dotnet build CafeteriaPOS\CafeteriaPOS.csproj -c Debug -p:Platform=x64 -v:minimal
```

Resultado esperado:

```text
Compilación correcta.
    52 Advertencia(s)
    0 Errores
```

Las 52 advertencias son conocidas y no bloqueantes:

- **10 × WMC1506:** bindings `OneWay` sobre propiedades que no notifican cambios. La interfaz funciona porque esos valores se actualizan al recargar la página.
- **42 × MVVMTK0045:** campos `[ObservableProperty]` que la versión moderna de CommunityToolkit sugiere convertir a propiedades parciales para compatibilidad AOT. La app no usa AOT.

No conviene silenciar estas advertencias sin corregir la causa; se documentan en la sección de mantenimiento.

### 2.3.3 Ejecución

```powershell
.\CafeteriaPOS\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\CafeteriaPOS.exe
```

También se puede abrir `CafeteriaPOS.slnx` en Visual Studio, seleccionar la plataforma `x64`, establecer `CafeteriaPOS` como proyecto de inicio y presionar **F5**.

### 2.3.4 Compilación de release

```powershell
dotnet build CafeteriaPOS\CafeteriaPOS.csproj -c Release -p:Platform=x64
```

Release no cambia el comportamiento funcional; solo optimiza el código y reduce el tamaño de los ensamblados.

## 2.4 Primer arranque

Al iniciar por primera vez, la aplicación crea la estructura de datos y una base mínima. No es necesario ejecutar ningún script adicional.

### 2.4.1 Carpetas creadas

```text
Documents\CafeteriaPOS\
├─ Data\
│  ├─ db.json
│  ├─ db.json.bak
│  └─ configuracion.json   (opcional, no se crea automáticamente)
├─ Tickets\
└─ Modelos\
```

La ruta exacta depende de la configuración de Windows. Si Documentos está redirigido a OneDrive, la base vivirá allí. Se puede comprobar desde PowerShell:

```powershell
[Environment]::GetFolderPath('MyDocuments')
```

### 2.4.2 Base inicial

Si `db.json` no existe, se crea con:

| Elemento | Valor |
|---|---|
| Usuario administrador | `admin` |
| Contraseña | `1234` |
| Puesto | Admin |
| Productos | Café Americano ($25), Capuchino ($40), Croissant ($30) |
| Categorías | Las que usan esos productos |

Si `db.json` ya existe, **no se sobrescribe**. La aplicación respeta los datos existentes aunque estén vacíos.

### 2.4.3 Primer inicio de sesión

1. Abrir `CafeteriaPOS.exe`.
2. Escribir `admin` en usuario.
3. Escribir `1234` en contraseña.
4. Presionar **Iniciar Sesión**.

Si el acceso falla, revisar que el archivo `db.json` exista y que la sección `empleados` tenga el usuario. La recuperación se explica en el documento de operación.

### 2.4.4 Cambiar la contraseña inicial

La aplicación no tiene una pantalla de “cambiar mi contraseña”. Un administrador debe:

1. Entrar a **Empleados**.
2. Seleccionar el usuario `admin`.
3. Escribir una contraseña nueva de máximo 4 dígitos.
4. Presionar **Actualizar**.

> La contraseña se guarda en texto plano. Para una instalación real, aplica primero la mejora de hash descrita en la sección de seguridad.

## 2.5 Instalar el modelo de Morgana

### 2.5.1 Copiar el archivo

Con el modelo en la carpeta actual:

```powershell
$destino = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS\Modelos'
New-Item -ItemType Directory -Force $destino | Out-Null
Copy-Item .\morgana.gguf $destino -Force
```

Verificar el tamaño:

```powershell
Get-Item (Join-Path $destino 'morgana.gguf') | Select-Object Name, Length
```

Debe mostrar `986048160` bytes. Un tamaño distinto suele indicar una copia incompleta.

### 2.5.2 Verificar el modelo

1. Abrir la aplicación e iniciar sesión.
2. Elegir **Asistente virtual** en el pie del menú.
3. Escribir `¿Cuánto cuesta el capuchino?`.
4. La primera pregunta carga el modelo; puede tardar entre 5 y 30 segundos según el equipo.
5. Las siguientes respuestas suelen tardar 1–3 segundos en CPU.

Si el archivo no está, el chat muestra:

```text
No encuentro el modelo entrenado, humano.

Cópialo en:
C:\Users\<usuario>\Documents\CafeteriaPOS\Modelos\morgana.gguf
```

El resto del POS sigue funcionando con normalidad.

### 2.5.3 Comportamiento si el modelo falla

| Situación | Qué ocurre |
|---|---|
| Archivo ausente | Mensaje con la ruta esperada; el POS sigue operativo |
| Archivo corrupto | Se registra el error y se muestra “No pude cargar el modelo” |
| Sin memoria suficiente | Igual que el caso anterior; cerrar otras aplicaciones |
| Pregunta fuera de alcance | Morgana responde con una frase de respaldo, sin inventar cifras |

## 2.6 Configuración opcional

### 2.6.1 Horario y formas de pago

Crear `Documents\CafeteriaPOS\Data\configuracion.json`:

```json
{
  "horario": "8:00 a 20:00",
  "pagos": "efectivo y tarjeta"
}
```

Morgana usa estos valores para las preguntas de horario y métodos de pago. Si el archivo no existe o tiene un error de formato, aplica los valores por defecto (`8:00 am a 6:00 pm` y `efectivo y tarjeta`). No es necesario reiniciar la aplicación: el archivo se lee en cada consulta.

### 2.6.2 Rutas de datos

Las rutas no se configuran desde la interfaz. `AppPaths` siempre usa `Documents\CafeteriaPOS`. Si se necesita otra ubicación, hay que modificar `Helpers\AppPaths.cs` y recompilar. Para respaldos o migraciones se recomienda copiar la carpeta completa, no cambiar la ruta.

### 2.6.3 Tema

El tema claro está forzado en `App.xaml`. No hay un selector de tema en la interfaz. Si se quisiera respetar el tema del sistema, habría que quitar `RequestedTheme="Light"` y revisar los contrastes de la paleta café/crema.

## 2.7 Publicación

### 2.7.1 Publicación autorretenida

```powershell
dotnet publish CafeteriaPOS\CafeteriaPOS.csproj `
  -c Release `
  -p:Platform=x64 `
  -p:PublishProfile=win-x64
```

La salida queda en:

```text
CafeteriaPOS\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\
```

El perfil `win-x64.pubxml` establece:

| Propiedad | Valor |
|---|---|
| `PublishProtocol` | FileSystem |
| `SelfContained` | true |
| `PublishSingleFile` | false |
| `PublishDir` | `bin\Release\net8.0-...\win-x64\publish\` |

La aplicación es **desempaquetada** (`WindowsPackageType=None`) y **autorretenida** (`WindowsAppSDKSelfContained=true` y `SelfContained=true`). Eso significa que el equipo destino no necesita instalar .NET ni el Windows App SDK.

### 2.7.2 Qué copiar al equipo destino

Copiar la carpeta `publish` completa, no solo el `.exe`. La carpeta incluye:

- `CafeteriaPOS.exe` y `.dll`;
- `Microsoft.WindowsAppRuntime.*`;
- `runtimes\win-x64\native\{avx2,avx512,avx,noavx}`;
- `Assets`;
- archivos de configuración y dependencias.

El modelo **no** se incluye en la publicación; se copia aparte a `Documents\CafeteriaPOS\Modelos`.

### 2.7.3 Publicación para otras arquitecturas

```powershell
dotnet publish CafeteriaPOS\CafeteriaPOS.csproj -c Release -p:Platform=x86 -p:PublishProfile=win-x86
dotnet publish CafeteriaPOS\CafeteriaPOS.csproj -c Release -p:Platform=ARM64 -p:PublishProfile=win-arm64
```

No se probó el modelo en x86 ni ARM64. En equipos ARM64, el rendimiento de la IA puede ser menor y el backend de CPU debe ser compatible.

### 2.7.4 Empaquetado MSIX

El proyecto conserva `Package.appxmanifest` y los recursos para generar un MSIX. El paquete de demostración de la entrega se firmó y se instaló con un certificado de prueba. Para producir un MSIX propio:

1. Abrir la solución en Visual Studio.
2. Clic derecho en el proyecto → **Publicar** → **Crear paquetes de aplicación**.
3. Elegir la arquitectura y la configuración `Release`.
4. Firmar con un certificado de confianza para el equipo destino.

La ruta desempaquetada es la recomendada para soporte y para actualizar sin reinstalar. El MSIX es útil cuando se necesita una instalación con identidad de aplicación y actualizaciones administradas.

## 2.8 Actualización

### 2.8.1 Actualizar la aplicación

1. Cerrar Cafetería Leblanc POS.
2. Respaldar `Documents\CafeteriaPOS\Data\db.json` y la carpeta `Tickets`.
3. Reemplazar la carpeta `publish` por la nueva versión.
4. Abrir la aplicación y verificar el acceso.

La base y los tickets viven fuera de la carpeta de la aplicación, por lo que una actualización no los borra.

### 2.8.2 Actualizar el modelo

1. Cerrar la aplicación.
2. Reemplazar `Documents\CafeteriaPOS\Modelos\morgana.gguf`.
3. Abrir la aplicación y probar tres preguntas conocidas.

No es necesario borrar la base ni volver a entrenar. Si el modelo nuevo cambia el formato de comodines, hay que actualizar `ComodinResolver` y volver a compilar.

### 2.8.3 Volver a una versión anterior

Conservar la carpeta `publish` anterior con otro nombre; por ejemplo, `publish_v1.0.0`. Para volver atrás, cerrar la app y renombrar las carpetas. La base no cambia de formato entre versiones 1.0.x.

## 2.9 Instalación desde el paquete de entrega

Si se recibió la carpeta `Entrega` con un MSIX firmado:

1. Abrir PowerShell como administrador en `Entrega\Paquete`.
2. Ejecutar `.\Instalar.ps1`.
3. Aceptar el aviso de UAC.
4. Instalar el certificado de prueba si Windows lo solicita.
5. Abrir la aplicación desde el menú Inicio.

Para abrir la versión ya registrada sin instalar, usar `Entrega\IniciarDemo.ps1`. Este flujo es el que se usó en el ensayo del 27/10/2026 y se conserva como referencia; la carpeta `Entrega` no debe modificarse.

## 2.10 Verificación de la instalación

Marcar cada punto después de una instalación nueva:

| # | Verificación | Resultado esperado |
|---|---|---|
| 1 | La app abre | Ventana de login con el logo |
| 2 | `admin` / `1234` | Entra a Punto de Venta |
| 3 | Menú lateral | Punto de Venta, Productos, Empleados, Reportes, Asistente, Cerrar Sesión |
| 4 | Productos | Al menos 3 productos iniciales |
| 5 | Venta de prueba | Carrito, propina, cobro y mensaje con ruta del ticket |
| 6 | Ticket | Archivo `.txt` en `Documents\CafeteriaPOS\Tickets` |
| 7 | Reportes | La venta de prueba aparece en los totales del día |
| 8 | Morgana | Responde una pregunta de precio sin errores |
| 9 | Cierre de sesión | Vuelve al login y cierra el chat |
| 10 | Reinicio | Los datos de la venta persisten |

Si la venta de prueba no debe quedar en la base real, hacerla en una copia de `db.json` o eliminarla después con autorización. Nunca se debe borrar la base completa para “limpiar” una prueba.

## 2.11 Solución de problemas

| Síntoma | Causa probable | Solución |
|---|---|---|
| `dotnet` no se reconoce | SDK ausente o fuera del PATH | Instalar .NET SDK 8 y abrir una terminal nueva |
| `error MSB4019` o paquetes faltantes | Carga de trabajo de Windows incompleta | Instalar Windows App SDK en Visual Studio y restaurar NuGet |
| `NETSDK1045` | SDK anterior a .NET 8 | Instalar .NET 8 y verificar `dotnet --list-sdks` |
| La app no abre y no muestra error | Falta WebView2 o Windows App Runtime | Instalar el runtime y reiniciar |
| Error `0x80070005` al publicar | Carpeta de salida bloqueada | Cerrar la app y borrar `bin\Release` |
| La app abre en tema oscuro | Tema del sistema | La app fuerza claro; revisar `App.xaml` |
| Morgana no encuentra el modelo | Ruta o nombre incorrecto | Copiar como `morgana.gguf` en `Modelos` |
| Morgana tarda mucho la primera vez | Carga inicial del GGUF | Esperar; las siguientes respuestas son más rápidas |
| “No pude cargar el modelo” | Archivo incompleto o RAM insuficiente | Verificar tamaño, cerrar apps y reintentar |
| Reportes en cero | No hay ventas de hoy | Revisar la fecha del sistema y registrar una venta |
| No aparece una venta en reportes | Fecha fuera del día actual | Los reportes de hoy filtran por fecha local |
| No se puede iniciar sesión | Usuario/contraseña o base dañada | Probar `admin`/`1234`; revisar `db.json` |
| La app no ve cambios externos en `db.json` | Archivo bloqueado o JSON inválido | Cerrar la app, validar el JSON y reabrir |
| El ticket no se genera | Carpeta `Tickets` sin permisos | Revisar permisos de `Documents\CafeteriaPOS` |
| El CSV se ve con caracteres raros | Se abrió sin UTF-8 | Usar Excel y confirmar la codificación UTF-8 con BOM |
| El instalador MSIX falla | Certificado no confiable | Instalar el certificado de prueba o firmar con uno propio |

### 2.11.1 Diagnóstico rápido

```powershell
dotnet --info
Get-ChildItem (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS') -Recurse
Get-Content (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS\Data\db.json') -TotalCount 5
```

Comprobar que el JSON sea válido:

```powershell
$db = Get-Content (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS\Data\db.json') -Raw | ConvertFrom-Json
"Productos: $($db.productos.Count) | Ventas: $($db.ventas.Count)"
```

### 2.11.2 Dónde buscar más ayuda

- `docs/05-operacion-mantenimiento.md`: respaldo, recuperación y seguridad.
- `docs/04-morgana-ia.md`: entrenamiento, comodines y límites del modelo.
- `Data\morgana_error.log`: errores de carga o inferencia.
- Registro de eventos de Windows: fallos de arranque de WinUI o del runtime.
