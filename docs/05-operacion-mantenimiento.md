# 5. Operación y mantenimiento

Este capítulo reúne lo necesario para operar Cafetería Leblanc POS después de la instalación: respaldo, recuperación, seguridad, monitoreo, pruebas, limitaciones conocidas y mantenimiento. También incluye el glosario, los anexos, las conclusiones del equipo y las referencias.

## 5.1 Operación diaria

### 5.1.1 Apertura del turno

1. Encender el equipo y esperar a que Windows termine de iniciar.
2. Verificar que la fecha y la hora sean correctas. Los reportes de “hoy” dependen de ellas.
3. Abrir Cafetería Leblanc POS.
4. Iniciar sesión con el usuario propio.
5. Revisar el catálogo: productos agotados, stock bajo y precios.
6. Abrir **Reportes** si se necesita el contexto del día anterior.
7. Abrir **Asistente virtual** solo si se va a consultar a Morgana; la primera carga del modelo tarda unos segundos.

### 5.1.2 Durante el turno

- Registrar cada venta en el momento.
- Verificar el cambio antes de entregarlo.
- Revisar el ticket generado.
- Reponer stock desde **Productos** cuando sea necesario (solo Admin).
- No editar `db.json` con la aplicación abierta.
- No cerrar la aplicación a la fuerza durante un cobro.

### 5.1.3 Cierre del turno

1. Exportar el reporte del día a CSV o JSON.
2. Guardar el archivo con una convención clara: `Reporte_AAAAMMDD.csv`.
3. Revisar que los totales coincidan con la caja.
4. Cerrar sesión.
5. Copiar `Documents\CafeteriaPOS\Data\db.json` a una unidad externa o a una carpeta de respaldo.
6. Apagar el equipo de forma normal.

### 5.1.4 Convención de nombres para respaldos

```text
CafeteriaPOS_Backup_20261101_2000\
├─ Data\db.json
├─ Data\configuracion.json   (si existe)
└─ Tickets\
```

Incluir la fecha y la hora en el nombre evita sobrescribir respaldos anteriores. No usar espacios ni acentos en la ruta si se va a copiar a otro sistema.

## 5.2 Respaldo

### 5.2.1 Qué respaldar

| Elemento | Ruta | Prioridad |
|---|---|---|
| Base de datos | `Documents\CafeteriaPOS\Data\db.json` | Crítica |
| Respaldo automático | `Documents\CafeteriaPOS\Data\db.json.bak` | Alta |
| Configuración | `Documents\CafeteriaPOS\Data\configuracion.json` | Media |
| Tickets | `Documents\CafeteriaPOS\Tickets\` | Alta |
| Modelo | `Documents\CafeteriaPOS\Modelos\morgana.gguf` | Media (se puede volver a copiar) |
| Logs de IA | `Documents\CafeteriaPOS\Data\morgana_error.log` | Baja |

### 5.2.2 Respaldo manual

Con la aplicación cerrada:

```powershell
$origen = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS'
$fecha = Get-Date -Format 'yyyyMMdd_HHmm'
$destino = Join-Path $env:USERPROFILE "Backups\CafeteriaPOS_$fecha"
New-Item -ItemType Directory -Force $destino | Out-Null
Copy-Item (Join-Path $origen 'Data') $destino -Recurse -Force
Copy-Item (Join-Path $origen 'Tickets') $destino -Recurse -Force
```

Verificar el contenido:

```powershell
Get-ChildItem $destino -Recurse | Select-Object FullName, Length
```

### 5.2.3 Respaldo automático del sistema

Se puede programar una tarea con el Programador de tareas de Windows:

1. Crear una tarea básica.
2. Frecuencia: diaria, fuera del horario de la cafetería.
3. Acción: ejecutar un script `.ps1` con la copia.
4. Condición: iniciar solo si el equipo está encendido.
5. Configuración: no iniciar una nueva instancia si ya se está ejecutando.

El script debe cerrar la aplicación o al menos comprobar que no esté escribiendo. Copiar `db.json` mientras se guarda puede producir un archivo a medias; el `.bak` ayuda, pero no sustituye un respaldo consistente.

### 5.2.4 Respaldo en la nube

Si se usa OneDrive, Google Drive o una unidad de red:

- Copiar la carpeta de respaldo, no la carpeta activa.
- No sincronizar `bin` ni `obj`.
- Verificar que el proveedor no bloquee `db.json` mientras la app lo usa.
- Probar una restauración al menos una vez al mes.

### 5.2.5 Frecuencia recomendada

| Tamaño del negocio | Respaldo |
|---|---|
| Menos de 50 ventas/día | Diario al cierre |
| 50–200 ventas/día | Dos veces al día |
| Más de 200 ventas/día | Cada 4 horas o al cambio de turno |

## 5.3 Recuperación

### 5.3.1 Si `db.json` está dañado

Síntomas:

- la aplicación no inicia sesión;
- los reportes muestran cero aunque haya tickets;
- aparece un error de JSON al abrir.

Procedimiento:

1. Cerrar la aplicación.
2. Copiar `db.json` a una carpeta temporal para no perder la evidencia.
3. Copiar `db.json.bak` sobre `db.json`.
4. Abrir la aplicación y verificar.
5. Si el `.bak` también está dañado, restaurar el último respaldo manual.
6. Si no hay respaldo, intentar validar el JSON con un editor y corregir solo el error de sintaxis, sin borrar datos.

```powershell
$data = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS\Data'
Copy-Item (Join-Path $data 'db.json') (Join-Path $data 'db.json.danado') -Force
Copy-Item (Join-Path $data 'db.json.bak') (Join-Path $data 'db.json') -Force
```

> **Limitación actual:** la aplicación no conmuta automáticamente al `.bak` si `db.json` está corrupto. La recuperación es manual. Se propone como mejora en el roadmap.

### 5.3.2 Si se perdió la contraseña de todos los administradores

Con la aplicación cerrada:

1. Abrir `db.json` con un editor de texto.
2. Localizar el empleado administrador.
3. Cambiar `"password"` por `"1234"`.
4. Guardar y abrir la aplicación.
5. Cambiar la contraseña desde **Empleados**.

Esto es una operación de soporte y requiere acceso físico al equipo. Si la base está cifrada o en una unidad de red, hay que resolver primero el acceso.

### 5.3.3 Si se eliminó un producto por error

1. Cerrar la aplicación.
2. Restaurar el último respaldo.
3. Volver a abrir y verificar.

No hay “papelera” ni deshacer. Si no hay respaldo, se puede recrear el producto con el mismo nombre y precio; las ventas históricas conservan el detalle anterior.

### 5.3.4 Si se eliminó un empleado por error

1. Cerrar la aplicación.
2. Restaurar el respaldo.
3. Verificar que las ventas históricas sigan mostrando el nombre.

Las ventas guardan `IdEmpleado` y `NombreEmpleado` en la cabecera, por lo que el reporte histórico no depende de que el empleado exista.

### 5.3.5 Si el modelo no carga

1. Verificar el tamaño del archivo.
2. Verificar que el nombre sea exactamente `morgana.gguf`.
3. Revisar `Data\morgana_error.log`.
4. Probar con otra copia del modelo.
5. Si el problema es de memoria, cerrar aplicaciones y reiniciar.

El POS funciona sin el modelo; solo se pierde el chat.

### 5.3.6 Prueba de restauración

Una copia que nunca se restauró no es un respaldo confiable. Una vez al mes:

1. Crear una carpeta de prueba.
2. Copiar un respaldo.
3. Abrir la aplicación apuntando a esa base (o reemplazar temporalmente la base real en un equipo de prueba).
4. Verificar login, productos, ventas y reportes.
5. Documentar la fecha y el resultado.

## 5.4 Seguridad

### 5.4.1 Estado actual

| Aspecto | Estado | Riesgo |
|---|---|---|
| Contraseñas | Texto plano en `db.json` | Alto |
| Rol | Validado en UI y navegación | Medio |
| Acceso al archivo | Permisos de Windows | Medio |
| Red | Sin puertos ni API | Bajo |
| Modelo | Local, sin telemetría | Bajo |
| Tickets | Texto plano | Bajo |
| Respaldos | Sin cifrar | Medio |

### 5.4.2 Contraseñas en texto plano

Es la limitación más importante. Cualquier persona con acceso al archivo puede leer las contraseñas. Para una demo académica es aceptable; para producción no.

Mejora recomendada:

1. Agregar un campo `PasswordHash` y `PasswordSalt` al modelo `Empleado`.
2. Usar PBKDF2, bcrypt o Argon2id con un número de iteraciones razonable.
3. Migrar las contraseñas existentes en el primer inicio de sesión.
4. Comparar con `CryptographicOperations.FixedTimeEquals`.
5. No mostrar ni registrar la contraseña.

La implementación completa queda fuera del alcance de esta entrega, pero la base está preparada para agregar campos sin romper la lectura: `PropertyNameCaseInsensitive` y los valores por defecto permiten una migración gradual.

### 5.4.3 Permisos de archivos

Recomendaciones:

- La carpeta `Documents\CafeteriaPOS` debe pertenecer al usuario que opera la caja.
- Evitar compartir la carpeta por red.
- No dar permisos de escritura a usuarios invitados.
- Si hay varios turnos, usar cuentas de Windows separadas o una cuenta compartida con respaldo.
- Proteger la carpeta de respaldos con una contraseña o cifrado.

### 5.4.4 Protección contra edición accidental

- No abrir `db.json` en Excel; Excel puede reinterpretar números y fechas.
- Usar un editor de texto con validación JSON.
- Hacer una copia antes de cualquier edición manual.
- Validar con `ConvertFrom-Json` después de editar.
- No editar con la aplicación abierta.

### 5.4.5 Seguridad de la IA

- El modelo no tiene acceso a la red.
- No se ejecutan comandos a partir de la respuesta.
- La respuesta se muestra como texto, no como HTML.
- El contenido del usuario no se usa para entrenar.
- Los errores se registran sin incluir la pregunta completa (solo la excepción).

### 5.4.6 Checklist de seguridad

- [ ] Cambiar `admin`/`1234` en la primera instalación.
- [ ] Cada empleado tiene su propio usuario.
- [ ] Los usuarios `Cajero` no ven secciones administrativas.
- [ ] La carpeta de datos tiene permisos restringidos.
- [ ] Hay un respaldo reciente y probado.
- [ ] El equipo tiene antivirus y actualizaciones.
- [ ] Nadie comparte la contraseña por mensaje.
- [ ] Se revisa `morgana_error.log` si hay fallos.

## 5.5 Monitoreo y logs

### 5.5.1 Dónde mirar

| Fuente | Qué muestra |
|---|---|
| Mensajes en pantalla | Éxito (verde) o error/advertencia (rojo) |
| `Data\morgana_error.log` | Excepciones de carga o inferencia |
| `Tickets\` | Comprobantes generados |
| `db.json` | Estado completo de la base |
| Registro de eventos de Windows | Fallos de arranque o del runtime |

### 5.5.2 Revisar el log de Morgana

```powershell
$log = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS\Data\morgana_error.log'
if (Test-Path $log) { Get-Content $log -Tail 50 } else { 'Sin errores registrados' }
```

El log crece con cada error; no se rota automáticamente. Si se vuelve muy grande, archivarlo y crear uno nuevo con la aplicación cerrada.

### 5.5.3 Indicadores de salud

| Indicador | Sano | Revisar |
|---|---|---|
| La app abre en menos de 10 s | Sí | No |
| Login responde | Sí | No |
| Venta de prueba cobra | Sí | No |
| Reportes coinciden con tickets | Sí | No |
| Morgana responde | Sí | No |
| `db.json` válido | Sí | No |
| Respaldo del día | Sí | No |
| Espacio libre en disco | > 2 GB | < 1 GB |

### 5.5.4 Alertas manuales

No hay un sistema de alertas automáticas. Se recomienda una revisión diaria de:

- stock bajo;
- ventas del día;
- propinas;
- errores de Morgana;
- espacio en disco.

## 5.6 Mantenimiento

### 5.6.1 Tareas diarias

- Abrir y cerrar sesión correctamente.
- Revisar el reporte del día.
- Respaldar la base.
- Verificar que los tickets se generaron.

### 5.6.2 Tareas semanales

- Revisar stock y precios.
- Revisar usuarios y puestos.
- Revisar el log de Morgana.
- Verificar el espacio en disco.
- Probar una pregunta de cada tipo a Morgana.

### 5.6.3 Tareas mensuales

- Restaurar un respaldo en un equipo de prueba.
- Revisar permisos de la carpeta de datos.
- Actualizar Windows y .NET.
- Revisar la versión de la app y del modelo.
- Limpiar tickets antiguos (con respaldo previo).
- Revisar el rendimiento de la IA.

### 5.6.4 Tareas trimestrales

- Revisar la deuda técnica y el roadmap.
- Evaluar si el volumen justifica migrar a SQLite.
- Reentrenar o actualizar el modelo si hay datos suficientes.
- Revisar la documentación y las capturas.
- Probar la instalación desde cero en un equipo limpio.

### 5.6.5 Actualizar dependencias

```powershell
dotnet list CafeteriaPOS\CafeteriaPOS.csproj package --outdated
```

Antes de actualizar:

1. Crear una rama o copia del proyecto.
2. Actualizar un paquete a la vez.
3. Compilar y probar.
4. Revisar los cambios de comportamiento de WinUI, LLamaSharp y CommunityToolkit.
5. No actualizar LLamaSharp sin volver a probar el GGUF.

### 5.6.6 Limpieza de tickets

Los tickets son comprobantes; no se deben borrar sin autorización. Si se archivan:

1. Cerrar la aplicación.
2. Copiar la carpeta `Tickets` a un respaldo.
3. Mover los archivos antiguos a una carpeta de archivo.
4. No borrar los del día en curso.

## 5.7 Pruebas

### 5.7.1 Estado actual

No hay pruebas automatizadas. La validación es manual y se apoya en:

- compilación sin errores;
- checklist de flujo completo;
- comparación de reportes contra tickets;
- pruebas de Morgana con preguntas conocidas.

### 5.7.2 Checklist de regresión

| # | Caso | Pasos | Resultado esperado |
|---|---|---|---|
| 1 | Login válido | `admin`/`1234` | Entra a Punto de Venta |
| 2 | Login inválido | Usuario o contraseña incorrectos | Mensaje de error, no entra |
| 3 | Venta en efectivo | Producto + propina + pago | Venta, stock, ticket y cambio |
| 4 | Venta con tarjeta | Producto + tarjeta | Venta sin campo de cambio |
| 5 | Pago insuficiente | Efectivo menor al total | Bloquea el cobro |
| 6 | Stock límite | Intentar superar el stock | Bloquea y avisa |
| 7 | Producto agotado | Stock 0 | No se puede agregar |
| 8 | Vaciar carrito | Agregar y vaciar | Carrito y totales en cero |
| 9 | Crear producto | Formulario completo | Aparece en la lista |
| 10 | Editar producto | Cambiar precio/stock | Se refleja en la venta |
| 11 | Eliminar producto | Seleccionar y eliminar | Desaparece del catálogo |
| 12 | Categoría en uso | Intentar eliminar | Bloquea con conteo |
| 13 | Renombrar categoría | Cambiar nombre | Se actualiza en productos |
| 14 | Crear empleado | Datos válidos | Aparece en la lista |
| 15 | Usuario duplicado | Repetir usuario | Bloquea |
| 16 | Autoeliminación | Eliminar el usuario actual | Bloquea |
| 17 | Reportes | Abrir después de una venta | Métricas correctas |
| 18 | CSV | Exportar y abrir en Excel | Tildes correctas, columnas completas |
| 19 | JSON | Exportar y abrir | Estructura completa |
| 20 | Morgana precio | “¿Cuánto cuesta el capuchino?” | Precio real |
| 21 | Morgana stock | “¿Cuánto stock queda de ramen?” | Stock real |
| 22 | Morgana ventas | “¿Cuánto vendimos hoy?” | Coincide con Reportes |
| 23 | Morgana menú | “¿Qué hay en el menú?” | Solo productos con stock |
| 24 | Morgana fuera de alcance | “Cancela la última venta” | Negativa, no acción |
| 25 | Modelo ausente | Renombrar el GGUF y preguntar | Mensaje con la ruta |
| 26 | Cerrar sesión | Menú → Cerrar Sesión | Vuelve al login y cierra chat |
| 27 | Persistencia | Cerrar y abrir | Datos y ventas se conservan |
| 28 | Respaldo | Copiar `db.json` y restaurar | La app abre con los datos |

### 5.7.3 Pruebas de compilación

```powershell
dotnet build CafeteriaPOS\CafeteriaPOS.csproj -c Debug -p:Platform=x64 -v:minimal
```

Criterio de aceptación:

- 0 errores;
- 52 advertencias conocidas o menos;
- el ejecutable se genera;
- la app abre y permite iniciar sesión.

### 5.7.4 Pruebas de publicación

1. Publicar en Release.
2. Copiar la carpeta `publish` a un equipo limpio.
3. Copiar el modelo a `Documents\CafeteriaPOS\Modelos`.
4. Ejecutar sin instalar .NET.
5. Repetir los casos 1, 3, 17 y 20 del checklist.

### 5.7.5 Pruebas de IA

| Pregunta | Qué valida |
|---|---|
| ¿Cuánto cuesta el capuchino? | Comodín `PRECIO` y producto |
| ¿Cuánto stock queda de ramen? | Comodín `STOCK` |
| ¿Qué puedo comprar con $100? | `PRESUPUESTO` |
| ¿Qué hay en el menú? | `MENU` y stock |
| ¿Cuánto vendimos hoy? | `VENTAS_HOY` o respuesta determinista |
| ¿Cuántos tickets llevamos? | `TICKETS_HOY` |
| ¿Cuánto llevamos de propinas? | `PROPINAS_HOY` |
| ¿Cuánto lleva Saul? | `PROPINAS_POR_EMPLEADO` |
| ¿Cuáles son los más vendidos? | `TOP_PRODUCTOS` |
| ¿Qué está por agotarse? | `STOCK_BAJO` |
| ¿A qué hora cierran? | `HORARIO` |
| ¿Puedo pagar con tarjeta? | `PAGOS` |
| ¿Quién ganó el mundial? | Respaldo, sin inventar |

### 5.7.6 Pruebas de estrés

- Registrar 20 ventas seguidas y verificar stock y reportes.
- Abrir y cerrar el chat varias veces.
- Preguntar 10 veces seguidas para verificar el semáforo.
- Cerrar la app durante una venta (probar en una copia) y verificar que la base no quede a medias.
- Restaurar un respaldo y repetir una venta.

## 5.8 Limitaciones conocidas

| # | Limitación | Impacto | Mitigación | Prioridad |
|---|---|---|---|---|
| 1 | Contraseñas en texto plano | Alto | Acceso físico restringido; hash pendiente | Alta |
| 2 | Sin fallback automático al `.bak` | Alto | Recuperación manual documentada | Alta |
| 3 | Una sola caja | Medio | No usar dos instancias sobre la misma base | Media |
| 4 | Sin pruebas automatizadas | Medio | Checklist manual | Media |
| 5 | Modelo de 1.5B | Medio | Lista blanca, reintento y determinismo | Media |
| 6 | Sin validación formal del modelo | Medio | Pruebas manuales y comparativa | Media |
| 7 | 52 advertencias de compilación | Bajo | No bloquean; documentadas | Baja |
| 8 | Bindings `OneWay` sin notificación | Bajo | Se actualizan al recargar | Baja |
| 9 | Sin facturación fiscal | Medio | Fuera de alcance | — |
| 10 | Sin devoluciones/cancelaciones | Medio | Fuera de alcance | — |
| 11 | Sin multi-sucursal | Medio | Fuera de alcance | — |
| 12 | Log de IA sin rotación | Bajo | Archivado manual | Baja |
| 13 | Sin accesibilidad avanzada | Bajo | Contraste y tamaños razonables | Baja |
| 14 | Sin internacionalización | Bajo | Español de México | Baja |

### 5.8.1 Detalle de las advertencias

**WMC1506 (10).** El compilador de XAML avisa que un binding `OneWay` no tiene una fuente que notifique cambios. Ocurre en listas de reportes y en algún control de empleados. La interfaz se actualiza al recargar la página, por lo que no rompe el flujo. La corrección consiste en usar `OneTime` cuando el valor no cambia o implementar `INotifyPropertyChanged` en el modelo.

**MVVMTK0045 (42).** CommunityToolkit 8.4.2 sugiere usar propiedades parciales en lugar de campos con `[ObservableProperty]` para compatibilidad con AOT y WinRT. La app no usa AOT, así que las advertencias son informativas. La migración es mecánica pero amplia; se puede hacer en una rama aparte.

### 5.8.2 Riesgos aceptados

- La base JSON no tiene transacciones ACID; se acepta por el tamaño y el uso monousuario.
- El modelo puede fallar; se acepta porque el POS no depende de él.
- No hay autenticación de dos factores; se acepta por el contexto de una caja local.
- No hay cifrado en reposo; se acepta si el equipo está físicamente protegido.

## 5.9 Roadmap

### 5.9.1 Corto plazo

1. Hash + salt de contraseñas.
2. Fallback automático a `db.json.bak` con copia del archivo dañado.
3. Corregir los bindings `WMC1506`.
4. Agregar encabezados a las listas de reportes.
5. Rotación del log de Morgana.
6. Confirmación antes de eliminar producto, categoría o empleado.

### 5.9.2 Mediano plazo

1. Migrar a SQLite con migraciones versionadas.
2. Pruebas unitarias de servicios y de `ComodinResolver`.
3. Pruebas de UI con WinAppDriver o similar.
4. Conjunto de validación para el modelo.
5. Panel de configuración de horario y pagos.
6. Exportación a PDF del reporte.
7. Búsqueda por código de barras.

### 5.9.3 Largo plazo

1. Sincronización entre sucursales.
2. Facturación fiscal (CFDI) si el negocio lo requiere.
3. Terminal bancaria integrada.
4. Gestión de proveedores y compras.
5. Modelo más grande o con GPU opcional.
6. Aplicación móvil de consulta.

### 5.9.4 Criterios para priorizar

| Criterio | Peso |
|---|---|
| Riesgo de pérdida de datos | Alto |
| Seguridad | Alto |
| Impacto en la operación diaria | Alto |
| Esfuerzo de implementación | Medio |
| Valor para la demostración | Medio |
| Deuda técnica | Medio |

## 5.10 Glosario

| Término | Definición |
|---|---|
| **Admin** | Rol con acceso a productos, empleados y reportes |
| **Cajero** | Rol limitado a ventas y asistente |
| **Carrito** | Lista temporal de productos de una venta |
| **Comodín** | Marca que el resolvedor sustituye por un dato real |
| **CSV** | Texto separado por comas; se abre en Excel |
| **GGUF** | Formato de modelo para llama.cpp |
| **JSON** | Formato de texto para la base y las exportaciones |
| **LLamaSharp** | Biblioteca .NET que ejecuta modelos GGUF |
| **LoRA** | Ajuste de bajo rango sobre un modelo base |
| **Morgana** | Nombre de la asistente virtual |
| **MVVM** | Patrón Modelo-Vista-ViewModel |
| **POS** | Punto de venta |
| **Q4_K_M** | Cuantización de 4 bits usada por el modelo |
| **Semáforo** | Mecanismo que permite una inferencia a la vez |
| **Ticket** | Comprobante de venta en texto plano |
| **Token** | Unidad de texto que procesa el modelo |
| **WinUI 3** | Framework de interfaz de Windows App SDK |

## 5.11 Anexos

### 5.11.1 Anexo A: inventario de archivos de datos

```text
Documents\CafeteriaPOS\
├─ Data\
│  ├─ db.json               Base principal
│  ├─ db.json.bak           Versión anterior automática
│  ├─ db.json.tmp           Temporal durante la escritura
│  ├─ configuracion.json    Horario y pagos (opcional)
│  └─ morgana_error.log     Errores de IA (si ocurren)
├─ Tickets\
│  └─ V-AAAAMMDD-HHMMSS-XXX.txt
└─ Modelos\
   └─ morgana.gguf
```

### 5.11.2 Anexo B: comandos útiles

```powershell
# Ruta de datos
[Environment]::GetFolderPath('MyDocuments')

# Validar la base
$db = Get-Content (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS\Data\db.json') -Raw | ConvertFrom-Json
"Productos: $($db.productos.Count) | Ventas: $($db.ventas.Count) | Empleados: $($db.empleados.Count)"

# Respaldo rápido
$origen = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS'
$destino = Join-Path $env:USERPROFILE ("Backups\CafeteriaPOS_" + (Get-Date -Format 'yyyyMMdd_HHmm'))
New-Item -ItemType Directory -Force $destino | Out-Null
Copy-Item (Join-Path $origen 'Data') $destino -Recurse -Force

# Compilar
dotnet build CafeteriaPOS\CafeteriaPOS.csproj -c Debug -p:Platform=x64 -v:minimal

# Publicar
dotnet publish CafeteriaPOS\CafeteriaPOS.csproj -c Release -p:Platform=x64 -p:PublishProfile=win-x64

# Ver errores de Morgana
Get-Content (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'CafeteriaPOS\Data\morgana_error.log') -Tail 50
```

### 5.11.3 Anexo C: estructura de `db.json`

| Lista | Contenido |
|---|---|
| `productos` | Catálogo con precio, stock y mínimo |
| `categorias` | Nombres de categorías |
| `empleados` | Usuarios, contraseñas y puestos |
| `ventas` | Cabeceras de venta |
| `detalleVentas` | Renglones con producto, cantidad y precio histórico |

### 5.11.4 Anexo D: checklist de entrega

- [ ] Código compila en Debug x64 con 0 errores.
- [ ] Código compila en Release x64.
- [ ] La app abre en un equipo limpio.
- [ ] `admin`/`1234` funciona en la base inicial.
- [ ] Una venta completa genera ticket.
- [ ] Los reportes coinciden con la venta.
- [ ] Morgana responde con el modelo instalado.
- [ ] El POS funciona sin el modelo.
- [ ] README y wiki están actualizados.
- [ ] Documento Word y PDF generados.
- [ ] Conclusiones individuales y grupales incluidas.
- [ ] Referencias APA con enlaces.
- [ ] Respaldo de la base real intacto.
- [ ] Los dos `.gguf` de `Documents\CafeteriaPOS\Modelos` se conservan.
- [ ] La carpeta `Entrega` no se modificó.

### 5.11.5 Anexo E: preguntas para soporte

Antes de escalar un problema, recopilar:

1. Versión de la app y del modelo.
2. Versión de Windows y arquitectura.
3. Pasos para reproducir.
4. Mensaje exacto en pantalla.
5. Contenido de `morgana_error.log` si aplica.
6. Si el problema empezó después de una actualización.
7. Si la base fue editada manualmente.
8. Si hay respaldo reciente.

## 5.12 Conclusiones

### 5.12.1 Saúl

El mayor aprendizaje fue integrar una IA local en una aplicación de escritorio real sin convertirla en un servicio externo. LLamaSharp y un GGUF cuantizado permiten que Morgana responda en CPU dentro del mismo proceso, pero el proyecto también muestra que un modelo pequeño necesita límites claros: lista blanca de comodines, reintentos y respuestas deterministas. Esa combinación —IA para redactar, código para los datos— fue la decisión más importante para que el asistente fuera confiable.

También aprendí que documentar los fallos es parte del trabajo. La evaluación de 20 preguntas dejó 9 aciertos en la v2; ocultarlo habría sido más cómodo, pero no habría explicado por qué la app necesitaba un resolvedor. La arquitectura no niega las limitaciones del modelo: las contiene.

### 5.12.2 Javier

La parte más valiosa fue diseñar el punto de venta para que el cajero pueda trabajar sin pensar en la tecnología. El carrito, la propina, el cambio y el ticket siguen un orden natural; las validaciones evitan errores antes de que ocurran y los mensajes de color indican qué pasó. El tema claro y la paleta de la cafetería no son solo estética: reducen distracciones y hacen que la pantalla se sienta como parte del negocio.

Del lado técnico, entendí que una buena interfaz no es la que tiene más funciones, sino la que deja claro qué hacer en cada momento. El botón **Cobrar** deshabilitado con el carrito vacío, el stock que bloquea cantidades imposibles y el cambio que se calcula solo son pequeñas decisiones que evitan errores reales en la caja.

### 5.12.3 Felipe

El proyecto confirmó que la calidad no termina cuando la app compila. Probar el flujo completo, verificar que los reportes coincidan con las ventas, documentar las respuestas de Morgana y aceptar sus límites fue tan importante como programar. La documentación y las pruebas manuales permitieron detectar diferencias entre lo que el modelo “dice” y lo que los datos reales “son”, y dejaron una base para que otra persona pueda instalar, operar y mantener el sistema.

También aprendí a separar la deuda técnica de los errores. Las 52 advertencias no impiden operar, pero las contraseñas en texto plano y la falta de recuperación automática sí son riesgos que deben atenderse antes de llevar el sistema a producción. Dejar ese mapa escrito es parte de entregar un proyecto completo.

### 5.12.4 Conclusión grupal

Cafetería Leblanc POS demuestra que es posible construir un punto de venta completo y un asistente conversacional local con herramientas accesibles: WinUI 3 para la interfaz, .NET 8 para la lógica, JSON para la persistencia y un modelo Qwen afinado con LoRA para el lenguaje. El resultado no es un prototipo aislado: cubre venta, inventario, personal, reportes, exportaciones y consultas en lenguaje natural, con una arquitectura comprensible y sin dependencia de la nube.

El equipo considera que el sistema cumple el objetivo funcional y deja un camino claro de mejora: proteger las contraseñas con hash, hacer más robusta la recuperación de la base, ampliar las pruebas automatizadas y mejorar la calidad del asistente con un conjunto de validación. La principal lección es que la IA aporta valor cuando se integra con reglas de negocio explícitas y cuando el equipo asume sus límites con transparencia.

## 5.13 Referencias

> Formato APA 7.ª edición. Fechas de acceso: noviembre de 2026.

Bray, T. (2017). *The JavaScript Object Notation (JSON) data interchange format* (RFC 8259). Internet Engineering Task Force. https://doi.org/10.17487/RFC8259

Fowler, M. (2002). *Patterns of enterprise application architecture*. Addison-Wesley. https://martinfowler.com/books/eaa.html

Gamma, E., Helm, R., Johnson, R., & Vlissides, J. (1994). *Design patterns: Elements of reusable object-oriented software*. Addison-Wesley. https://www.pearson.com/us/higher-education/program/Gamma-Design-Patterns-Elements-of-Reusable-Object-Oriented-Software/PGM143806.html

GGML. (s. f.). *GGUF format specification*. GitHub. Recuperado el 1 de noviembre de 2026, de https://github.com/ggml-org/ggml/blob/master/docs/gguf.md

Hu, E. J., Shen, Y., Wallis, P., Allen-Zhu, Z., Li, Y., Wang, S., Wang, L., & Chen, W. (2021). *LoRA: Low-rank adaptation of large language models*. arXiv. https://arxiv.org/abs/2106.09685

Hugging Face. (s. f.). *PEFT documentation*. Recuperado el 1 de noviembre de 2026, de https://huggingface.co/docs/peft/index

Hugging Face. (s. f.). *TRL: SFTTrainer*. Recuperado el 1 de noviembre de 2026, de https://huggingface.co/docs/trl/sft_trainer

Microsoft. (s. f.). *.NET 8 documentation*. Recuperado el 1 de noviembre de 2026, de https://learn.microsoft.com/dotnet/

Microsoft. (s. f.). *CommunityToolkit.Mvvm documentation*. Recuperado el 1 de noviembre de 2026, de https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/

Microsoft. (s. f.). *FileSavePicker*. Recuperado el 1 de noviembre de 2026, de https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.filesavepicker

Microsoft. (s. f.). *Windows App SDK documentation*. Recuperado el 1 de noviembre de 2026, de https://learn.microsoft.com/windows/apps/windows-app-sdk/

Microsoft. (s. f.). *WinUI 3 documentation*. Recuperado el 1 de noviembre de 2026, de https://learn.microsoft.com/windows/apps/winui/winui3/

Microsoft. (s. f.). *XAML data binding*. Recuperado el 1 de noviembre de 2026, de https://learn.microsoft.com/windows/uwp/xaml-platform/xaml-data-binding

OWASP. (s. f.). *Password storage cheat sheet*. Recuperado el 1 de noviembre de 2026, de https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html

Qwen Team. (2024). *Qwen2.5: A party of foundation models*. arXiv. https://arxiv.org/abs/2412.15115

SciSharp. (s. f.). *LLamaSharp documentation*. Recuperado el 1 de noviembre de 2026, de https://scisharp.github.io/LLamaSharp/
