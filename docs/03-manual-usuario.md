# 3. Manual de usuario

Este manual describe cómo usar Cafetería Leblanc POS en el día a día. Está organizado por tareas y por rol, no por archivos ni por código. Si es tu primera vez, lee las secciones 3.1 a 3.4 y practica con una venta de prueba.

## 3.1 Antes de empezar

### 3.1.1 Qué necesitas

- La aplicación instalada o publicada en el equipo.
- Un usuario y contraseña asignados por el administrador.
- La impresora o carpeta de tickets configurada por el responsable técnico.
- El modelo `morgana.gguf` si vas a usar el asistente.

### 3.1.2 Roles

| Acción | Administrador | Cajero |
|---|:---:|:---:|
| Iniciar sesión | Sí | Sí |
| Registrar ventas | Sí | Sí |
| Consultar productos | Sí | Sí (solo desde la venta) |
| Crear, editar o eliminar productos | Sí | No |
| Administrar categorías | Sí | No |
| Administrar empleados | Sí | No |
| Ver reportes y exportar | Sí | No |
| Usar a Morgana | Sí | Sí |
| Cerrar sesión | Sí | Sí |

El menú lateral muestra u oculta las secciones según el rol. Un cajero no ve **Productos**, **Empleados** ni **Reportes**; si intentara navegar por otro medio, el código vuelve a validar el rol.

### 3.1.3 Convenciones del manual

- **Negritas:** nombre exacto de un botón, campo o sección.
- `Código`: valores, rutas o comandos.
- ⚠️: advertencia que evita perder datos.
- ✅: confirmación de que la operación salió bien.

## 3.2 Iniciar sesión

1. Abrir **Cafetería Leblanc POS** desde el menú Inicio o el acceso directo.
2. En la pantalla de acceso, escribir el **Usuario**. Solo se aceptan letras, sin espacios ni números, hasta 20 caracteres.
3. Escribir la **Contraseña**. Se aceptan hasta 4 dígitos.
4. Presionar **Iniciar Sesión** o la tecla **Enter**.

![Figura 3.1. Pantalla de inicio de sesión](recursos/capturas/login.png){width=16cm}

Credenciales de la base inicial:

```text
Usuario:    admin
Contraseña: 1234
```

Si los datos son incorrectos, aparece un mensaje en rojo debajo del botón. El mensaje no indica si falló el usuario o la contraseña, por seguridad.

### 3.2.1 Si olvidaste la contraseña

No hay recuperación automática. Un administrador debe:

1. Entrar con otro usuario administrador.
2. Ir a **Empleados**.
3. Seleccionar tu usuario.
4. Asignar una contraseña nueva y presionar **Actualizar**.

Si no queda ningún administrador con acceso, hay que editar `db.json` con la aplicación cerrada. Es una operación de soporte; se detalla en el documento de operación.

### 3.2.2 Cierre por inactividad

La aplicación no cierra la sesión automáticamente. Si el equipo queda solo, usa **Cerrar Sesión** antes de alejarte.

## 3.3 Navegación general

La ventana principal tiene dos zonas:

- **Menú lateral:** Punto de Venta, Productos, Empleados, Reportes, Asistente virtual y Cerrar Sesión.
- **Área de contenido:** la página seleccionada.

Al iniciar sesión siempre se abre **Punto de Venta**. El elemento activo queda resaltado. El asistente se abre en una ventana aparte para no perder el carrito.

### 3.3.1 Atajos y detalles de la interfaz

| Elemento | Comportamiento |
|---|---|
| **Enter** en el login | Intenta iniciar sesión |
| **Enter** en el chat | Envía la pregunta |
| Botón **−** en el carrito | Reduce la cantidad; si llega a 1, quita el producto |
| Botón **+** | Aumenta la cantidad hasta el stock disponible |
| Botón **Vaciar** | Quita todos los productos del carrito |
| Botón **Cobrar** | Se habilita solo con productos en el carrito |

## 3.4 Registrar una venta

Este es el flujo más importante. La aplicación está diseñada para que una venta completa tome pocos clics.

![Figura 3.2. Flujo completo de una venta, del login al ticket](recursos/flujo-venta.png){width=16cm}

### 3.4.1 Paso 1: abrir el punto de venta

Entrar a **Punto de Venta** desde el menú lateral. La pantalla se divide en:

- catálogo de productos (izquierda);
- carrito y cobro (derecha).

![Figura 3.3. Punto de venta con el catálogo, la orden actual y el cobro](recursos/capturas/ventas.png){width=16cm}

### 3.4.2 Paso 2: buscar y agregar productos

1. Usar el buscador **🔍 Buscar producto...** para filtrar por nombre.
2. Usar el selector de **Categoría** para filtrar por tipo.
3. Hacer clic en la tarjeta de un producto para agregarlo al carrito.
4. Repetir para cada producto.

Reglas del catálogo:

- Un producto con stock 0 aparece deshabilitado y no se puede agregar.
- Si el producto ya está en el carrito, el clic aumenta la cantidad.
- No se puede superar el stock disponible. Si lo intentas, aparece un mensaje de advertencia.
- El stock mostrado es el stock actual de la base, no una copia de la pantalla.

### 3.4.3 Paso 3: ajustar el carrito

En **Orden actual**:

- **+** aumenta una unidad.
- **−** reduce una unidad; si la cantidad es 1, elimina el producto.
- **Vaciar** elimina todo el carrito.

Cada línea muestra nombre, precio unitario, cantidad y subtotal. El subtotal general se actualiza en tiempo real.

![Figura 3.4. Carrito con dos productos, propina y total actualizado](recursos/capturas/carrito.png){width=16cm}

### 3.4.4 Paso 4: aplicar propina

Hay dos formas:

1. **Porcentaje:** presionar **5%**, **10%** o **15%**. La propina se calcula sobre el subtotal y se redondea a dos decimales.
2. **Monto libre:** escribir el importe en el campo **Libre**.

La propina se suma al total. Si se cambia el carrito después de fijar la propina, el porcentaje no se recalcula automáticamente: hay que volver a presionar el botón o ajustar el monto. Esto es intencional para no modificar una propina ya acordada con el cliente.

> **Propina y reportes:** la propina se guarda en la venta y se desglosa en los reportes por empleado y por método de pago. No forma parte del “total vendido sin propinas”.

### 3.4.5 Paso 5: elegir el método de pago

- **Efectivo:** aparece el campo **Paga con**. Escribir el monto recibido. El cambio se calcula automáticamente y se muestra en **Cambio:**.
- **Tarjeta:** no se captura efectivo ni cambio. El cobro se registra con el total exacto.

Si el monto recibido es menor que el total, la aplicación muestra una advertencia y no permite cobrar. Para corregir, aumentar el monto o revisar el carrito.

### 3.4.6 Paso 6: cobrar

1. Verificar el **Subtotal**, la **Propina** y el **TOTAL**.
2. Presionar **Cobrar**.

La aplicación, en una sola operación:

- genera el identificador de la venta;
- guarda la venta y su detalle;
- descuenta el stock de cada producto;
- genera el ticket en texto plano;
- limpia el carrito y la propina;
- recarga el catálogo con el stock actualizado.

Al terminar, aparece un mensaje verde con el identificador y la ruta del ticket:

```text
✅ Venta V-20261101-101500-123 registrada.
Ticket: C:\Users\<usuario>\Documents\CafeteriaPOS\Tickets\V-20261101-101500-123.txt
```

### 3.4.7 Paso 7: entregar el ticket

Abrir el archivo `.txt` desde la ruta indicada o imprimirlo desde el visor de texto. El ticket incluye:

- nombre del negocio y leyenda;
- identificador, fecha, hora y cajero;
- cantidad, producto e importe por línea;
- subtotal, propina y total;
- método de pago;
- monto recibido y cambio (solo efectivo).

El ticket es un comprobante, no una factura fiscal.

### 3.4.8 Errores comunes en una venta

| Mensaje | Qué significa | Qué hacer |
|---|---|---|
| ⚠️ El carrito está vacío | Se presionó Cobrar sin productos | Agregar al menos un producto |
| ⚠️ El pago del cliente es insuficiente | El efectivo recibido es menor al total | Aumentar “Paga con” o revisar el total |
| ⚠️ Solo hay N unidades de X | Se intentó superar el stock | Reducir la cantidad |
| ⚠️ X sin stock | El producto se agotó | Elegir otro producto |
| ⚠️ Stock máximo alcanzado | Se llegó al límite disponible | No se puede vender más de lo que hay |

## 3.5 Administrar productos

Esta sección solo está disponible para el rol **Admin**.

### 3.5.1 Ver y filtrar

1. Entrar a **Productos**.
2. Usar el buscador **🔍 Buscar producto por nombre...**.
3. Filtrar por **Categoría** con el selector.
4. La lista muestra producto, categoría, precio y stock.

El stock se colorea según el estado:

- normal: color de texto habitual;
- bajo: ámbar cuando es igual o menor al stock mínimo;
- agotado: rojo cuando es 0.

![Figura 3.5. Administración de productos con la lista y el formulario](recursos/capturas/productos.png){width=16cm}

### 3.5.2 Crear un producto

1. Presionar **Nuevo producto**.
2. Llenar el formulario de la derecha:
   - **Nombre:** máximo 20 caracteres.
   - **Categoría:** debe existir previamente.
   - **Precio:** mayor que 0, hasta 9999.999.
   - **Stock actual:** entero, 0 o más.
   - **Stock mínimo (alerta):** entero, 0 o más.
3. Presionar **Guardar**.

Si la categoría no existe, el formulario muestra un aviso. Primero hay que crearla en **⚙️ Administrar categorías**.

### 3.5.3 Editar un producto

1. Hacer clic en el producto en la lista.
2. El formulario se llena y el título cambia a **✏️ Editar producto**.
3. Modificar los campos.
4. Presionar **Actualizar**.

El identificador del producto no cambia, por lo que las ventas históricas siguen apuntando al mismo producto.

### 3.5.4 Eliminar un producto

1. Seleccionar el producto.
2. Presionar **🗑️ Eliminar**.

El producto desaparece del catálogo, pero las ventas anteriores conservan su nombre y precio en el detalle. No hay confirmación adicional: si se elimina por error, hay que recuperarlo desde un respaldo o volver a crearlo.

> ⚠️ Eliminar un producto no elimina las ventas ni los tickets. Tampoco se puede “deshacer” desde la interfaz.

### 3.5.5 Reponer stock

El stock solo cambia por dos vías:

- una venta lo descuenta;
- el administrador lo edita manualmente en **Productos**.

Para reponer:

1. Seleccionar el producto.
2. Cambiar **Stock actual** al nuevo valor.
3. Presionar **Actualizar**.

No hay un módulo de compras ni de proveedores. El stock es un número informativo, no un inventario contable.

## 3.6 Administrar categorías

1. Entrar a **Productos**.
2. Expandir **⚙️ Administrar categorías**.

### 3.6.1 Crear una categoría

1. Escribir el nombre en **Nueva categoría (máx. 15)**.
2. Presionar **➕ Agregar**.

Reglas:

- solo letras, sin números ni símbolos;
- máximo 15 caracteres;
- no se permiten duplicados, aunque cambien mayúsculas y minúsculas.

### 3.6.2 Renombrar una categoría

1. Seleccionar la categoría en la lista.
2. Escribir el nuevo nombre en **Nuevo nombre**.
3. Presionar **Actualizar**.

El cambio se aplica a la categoría y a todos los productos que la usaban, en una sola escritura. Si la categoría estaba seleccionada como filtro o en el formulario, la selección se actualiza.

### 3.6.3 Eliminar una categoría

1. Seleccionar la categoría.
2. Presionar **🗑️ Eliminar**.

No se puede eliminar una categoría que tenga productos asignados. El mensaje indica cuántos productos la usan. Para eliminarla, primero hay que mover o eliminar esos productos.

## 3.7 Administrar empleados

Esta sección solo está disponible para **Admin**.

### 3.7.1 Ver y buscar

La lista muestra nombre, usuario y puesto. El buscador filtra por nombre o usuario.

![Figura 3.6. Administración de empleados y formulario de alta](recursos/capturas/empleados.png){width=16cm}

### 3.7.2 Crear un empleado

1. Presionar **➕ Nuevo empleado**.
2. Llenar:
   - **Nombre completo:** solo letras y espacios, máximo 30 caracteres.
   - **Usuario:** solo letras, máximo 20 caracteres, único.
   - **Contraseña:** máximo 4 dígitos; obligatoria en el alta.
   - **Puesto:** `Admin` o `Cajero`.
3. Presionar **Guardar**.

El usuario se compara sin distinguir mayúsculas: `Saul` y `saul` son el mismo usuario.

### 3.7.3 Editar un empleado

1. Seleccionar el empleado.
2. Cambiar nombre, usuario o puesto.
3. Si se deja la contraseña vacía, se conserva la actual. Si se escribe una nueva, debe tener máximo 4 dígitos.
4. Presionar **Actualizar**.

### 3.7.4 Eliminar un empleado

1. Seleccionar el empleado.
2. Presionar **🗑️ Eliminar**.

No se puede eliminar el usuario con el que se inició sesión. Las ventas históricas conservan el nombre del empleado en la cabecera, aunque el usuario ya no exista.

> ⚠️ Un empleado eliminado no puede volver a iniciar sesión. Si solo necesita un cambio de puesto, edítalo en lugar de eliminarlo.

## 3.8 Consultar reportes

Esta sección solo está disponible para **Admin**. Los reportes se calculan al abrir la página y al presionar **🔄 Actualizar**.

### 3.8.1 Métricas del día

| Indicador | Qué muestra |
|---|---|
| **Total vendido** | Suma de subtotales de hoy, sin propinas |
| **Ventas hoy** | Suma de totales de hoy, con propinas |
| **Tickets** | Cantidad de ventas de hoy |
| **Propinas hoy** | Suma de propinas de hoy |
| **Promedio** | Total con propinas dividido entre tickets de hoy |

Si no hay ventas, las métricas muestran cero y las listas muestran **Sin ventas hoy**.

![Figura 3.7. Reportes del día y resumen de los últimos siete días](recursos/capturas/reportes.png){width=16cm}

### 3.8.2 Productos más vendidos (hoy)

Muestra hasta 10 productos, ordenados por cantidad vendida, con:

- nombre del producto;
- cantidad total;
- monto total vendido.

El agrupamiento usa el nombre y el id guardados en el detalle, por lo que un producto renombrado después de la venta conserva el nombre histórico en ese reporte.

### 3.8.3 Propinas por empleado (hoy)

Agrupa por empleado y muestra:

- ventas realizadas;
- propinas en efectivo;
- propinas con tarjeta;
- total de propinas.

El orden es de mayor a menor total de propinas.

### 3.8.4 Últimos 7 días

Siempre muestra siete filas, de hace seis días a hoy, incluso si un día no tuvo ventas. Columnas:

| Columna | Descripción |
|---|---|
| Fecha | `dd/MM/yyyy` |
| Ventas | Cantidad de tickets |
| Total Sin Propinas | Suma de subtotales |
| Total Con Propinas | Suma de totales |
| Promedio de ventas | Total con propinas / tickets del día |

La ventana es móvil: al día siguiente, el día más antiguo sale y entra el nuevo.

### 3.8.5 Exportar CSV

1. Presionar **📄 Exportar CSV**.
2. Elegir carpeta y nombre. El sugerido es `Reporte_AAAAMMDD_HHmm.csv`.
3. Guardar.

El archivo incluye:

- resumen de los últimos 7 días;
- una línea en blanco;
- propinas por empleado.

Se guarda en UTF-8 con BOM para que Excel muestre correctamente tildes y ñ. Los importes se escriben con punto decimal, sin símbolo de moneda.

### 3.8.6 Exportar JSON

1. Presionar **📄 Exportar JSON**.
2. Elegir carpeta y nombre.
3. Guardar.

El JSON incluye:

- `GeneradoEn`;
- `ResumenHoy`;
- `TopProductos`;
- `PropinasPorEmpleado`;
- `Ultimos7Dias`.

Se guarda en UTF-8 sin BOM, con indentación, para lectura humana o integración con otra herramienta.

### 3.8.7 Buenas prácticas de reportes

- Exportar al cierre del turno y guardar el archivo con la fecha.
- No editar el CSV/JSON a mano si se va a usar como respaldo.
- Revisar que la fecha del sistema sea correcta; los reportes de “hoy” dependen de ella.
- Si un reporte no coincide con la caja, revisar primero los tickets del día y luego la base.

## 3.9 Usar a Morgana

Morgana es la asistente virtual. Se abre desde **Asistente virtual** en el pie del menú lateral y funciona en una ventana flotante.

### 3.9.1 Primer uso

1. Abrir **Asistente virtual**.
2. La ventana muestra un saludo y la lista de temas que puede consultar.
3. Escribir una pregunta y presionar **Enter** o el botón de enviar.
4. La primera pregunta carga el modelo; aparece **💭 Pensando...** mientras trabaja.
5. La respuesta se muestra en una burbuja.

El botón de la esquina superior derecha limpia el chat y vuelve al saludo. No borra datos ni reinicia el modelo.

![Figura 3.8. Morgana respondiendo una consulta de precio con el dato real](recursos/capturas/morgana.png){width=8cm}

### 3.9.2 Qué le puedes preguntar

| Tema | Ejemplos |
|---|---|
| Precio | ¿Cuánto cuesta el capuchino? |
| Stock | ¿Cuánto stock queda de ramen? |
| Menú | ¿Qué hay en el menú? / ¿Qué bebidas tienen? |
| Presupuesto | ¿Qué puedo comprar con $100? |
| Ventas de hoy | ¿Cuánto vendimos hoy? / ¿Cuántos tickets llevamos? |
| Ventas históricas | ¿Cuánto hemos vendido en total? |
| Propinas | ¿Cuánto llevamos de propinas? / ¿Cuánto lleva Saul? |
| Más vendidos | ¿Cuáles son los productos más vendidos? |
| Stock bajo | ¿Qué productos están por agotarse? |
| Horario | ¿A qué hora cierran? |
| Pagos | ¿Puedo pagar con tarjeta? |

### 3.9.3 Cómo responde

Morgana combina dos mecanismos:

1. **Respuestas deterministas:** para menú, ventas, propinas, conteos y top de productos, la aplicación consulta la base y redacta la respuesta sin pasar por el modelo. Así las cifras siempre coinciden.
2. **Modelo local:** para preguntas de precio, stock, presupuesto y conversación, el modelo genera una frase con comodines y la aplicación los sustituye por los datos reales.

Si el modelo produce un comodín desconocido o una respuesta vacía, la aplicación reintenta una vez y, si vuelve a fallar, responde con una frase segura.

### 3.9.4 Lo que Morgana no hace

- No cancela ventas.
- No aplica descuentos ni promociones.
- No genera facturas.
- No modifica el inventario.
- No consulta datos fuera de la base local.
- No navega a internet.

Si le pides algo fuera de su alcance, responde con una negativa amable. No inventa cifras: si no tiene el dato, lo dice.

### 3.9.5 Si Morgana no responde

| Situación | Solución |
|---|---|
| “No encuentro el modelo entrenado” | Copiar `morgana.gguf` a `Documents\CafeteriaPOS\Modelos` |
| “No pude cargar el modelo” | Verificar tamaño del archivo y memoria disponible |
| “No pude responder, humano” | Reintentar; revisar `Data\morgana_error.log` |
| Respuesta lenta | La primera carga tarda; las siguientes son más rápidas |
| Respuesta fuera de tema | Reformular con una pregunta de la tabla anterior |

## 3.10 Cerrar sesión

1. Presionar **Cerrar Sesión** en el pie del menú.
2. La aplicación cierra la ventana de Morgana, borra la sesión en memoria y vuelve al login.

La sesión no se guarda entre ejecuciones. Al cerrar la aplicación también se cierra el asistente y se libera el modelo.

## 3.11 Preguntas frecuentes

**¿Puedo usar la aplicación sin internet?**
Sí. La venta, los reportes y Morgana funcionan sin conexión. Solo la primera restauración de paquetes NuGet y la descarga del modelo requieren internet.

**¿Dónde quedan mis datos?**
En `Documents\CafeteriaPOS\Data\db.json`. Los tickets en `Documents\CafeteriaPOS\Tickets`. El modelo en `Documents\CafeteriaPOS\Modelos`.

**¿Puedo tener varias cajas?**
No en esta versión. El diseño es de una caja y una base local. Dos instancias sobre el mismo `db.json` pueden pisarse.

**¿Puedo cambiar un precio sin reentrenar a Morgana?**
Sí. Morgana no memoriza precios; usa comodines que se resuelven con la base actual.

**¿Por qué la propina no se recalcula sola?**
Para respetar el monto acordado con el cliente. Si cambias el carrito, vuelve a presionar el porcentaje o ajusta el campo **Libre**.

**¿Por qué el reporte “Total vendido” no incluye propinas?**
Porque separa lo que corresponde al negocio de lo que corresponde al empleado. “Ventas hoy” sí incluye propinas.

**¿Puedo recuperar una venta eliminada?**
No hay eliminación de ventas desde la interfaz. Si la base se dañó, se recupera desde `db.json.bak` o un respaldo manual.

**¿Qué pasa si elimino un producto?**
Desaparece del catálogo, pero las ventas anteriores conservan su nombre y precio en el detalle.

**¿Puedo cambiar el logo o los colores?**
Sí, desde el código y los recursos (`Assets`, `App.xaml`, estilos de las páginas). No hay un editor visual en la aplicación.

**¿Morgana puede ver mis datos personales?**
Solo consulta la base local del negocio. No envía información a internet ni a un servidor.

## 3.12 Buenas prácticas de operación

- Iniciar sesión con el usuario propio, no compartir credenciales.
- Verificar el stock antes de abrir el turno.
- Registrar la venta en el momento, no al final del día.
- Revisar el ticket generado antes de entregarlo.
- Exportar el reporte al cierre y guardarlo con la fecha.
- Cerrar sesión al terminar.
- No editar `db.json` con la aplicación abierta.
- No borrar `db.json.bak`; es la última red de seguridad.
- Antes de una actualización, copiar `Data` y `Tickets` a una unidad externa.
