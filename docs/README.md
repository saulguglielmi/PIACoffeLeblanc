# Wiki técnica — Cafetería Leblanc POS

Bienvenido a la documentación de **Cafetería Leblanc POS**, una aplicación de escritorio para la operación de una cafetería con punto de venta, inventario, empleados, reportes y un asistente de IA local llamado **Morgana**.

Esta wiki está pensada para tres públicos:

| Público | Qué encontrará |
|---|---|
| Nuevos integrantes del equipo | Visión general, arquitectura, convenciones y rutas de instalación |
| Personal de operación | Manual paso a paso para vender, administrar el catálogo y consultar reportes |
| Mantenimiento técnico | Modelo de datos, decisiones de diseño, respaldo, pruebas y límites conocidos |

## Navegación

| Documento | Contenido |
|---|---|
| [01. Descripción, arquitectura y datos](01-descripcion-arquitectura.md) | Qué es el sistema, stack, capas, estructura de carpetas, modelo de datos y decisiones técnicas |
| [02. Instalación y configuración](02-instalacion.md) | Requisitos, compilación, primer arranque, modelo de IA, publicación y solución de problemas |
| [03. Manual de usuario](03-manual-usuario.md) | Roles, flujo de venta, inventario, empleados, reportes, asistente y preguntas frecuentes |
| [04. Morgana: IA local](04-morgana-ia.md) | Entrenamiento LoRA, dataset, comodines, conversión GGUF, evaluación y actualización |
| [05. Operación y mantenimiento](05-operacion-mantenimiento.md) | Respaldo, recuperación, seguridad, pruebas, limitaciones, glosario, anexos, conclusiones y referencias |

## Documento entregable

La versión formal para entrega está en:

- `Documentacion/CafeteriaPOS_Documentacion_Tecnica.docx`
- `Documentacion/CafeteriaPOS_Documentacion_Tecnica.pdf`

Incluye portada, control del documento, índice automático, manual de usuario, documentación técnica, conclusiones individuales y grupales, y referencias en formato APA 7.ª edición.

El documento contiene 10 figuras: los diagramas de arquitectura, flujo de venta y flujo de IA, y 7 capturas reales de la aplicación (inicio de sesión, punto de venta, carrito, productos, empleados, reportes y Morgana).

## Convenciones de esta documentación

- **Rutas:** se usa `Documents\CafeteriaPOS` como carpeta de datos; en Windows puede resolverse a `C:\Users\<usuario>\Documents\CafeteriaPOS` o a una ruta de OneDrive si Documentos está redirigido.
- **Comandos:** se muestran para PowerShell, desde la raíz del proyecto.
- **Plataforma:** los ejemplos usan `x64`; el proyecto también declara `x86` y `ARM64`.
- **Datos sensibles:** las credenciales de ejemplo (`admin` / `1234`) son solo para la base inicial de demostración.
- **IA:** “Morgana” es el nombre del asistente; “Qwen2.5-1.5B-Instruct” es el modelo base y “LoRA” es la técnica de ajuste.

## Estado de la documentación

| Elemento | Fecha | Estado |
|---|---|---|
| Código fuente revisado | Noviembre de 2026 | Verificado |
| Compilación `Debug x64` | Noviembre de 2026 | 0 errores, 52 advertencias no bloqueantes |
| Documento Word | Noviembre de 2026 | Generado |
| Modelo de IA | v2, Q4_K_M | Documentado |

> Las advertencias corresponden a `WMC1506` (bindings `OneWay` sobre propiedades sin notificación) y `MVVMTK0045` (campos `[ObservableProperty]` que la versión moderna del toolkit sugiere convertir a propiedades parciales). No impiden compilar ni ejecutar; se registran en la sección de limitaciones.
