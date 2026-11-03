# 4. Morgana: la IA local

Morgana es la asistente conversacional de Cafetería Leblanc POS. No es un chatbot conectado a la nube ni un servicio aparte: es un modelo de lenguaje que se ejecuta **dentro del proceso de la aplicación**, sobre la CPU del equipo, y que responde con los datos reales del negocio.

Este capítulo documenta cómo se construyó, cómo se integra y cuáles son sus límites. La intención es que cualquier persona del equipo pueda entender por qué Morgana responde como responde y cómo actualizarla sin romper el POS.

## 4.1 Resumen técnico

| Elemento | Valor |
|---|---|
| Modelo base | Qwen2.5-1.5B-Instruct (Alibaba / Qwen) |
| Técnica de ajuste | LoRA (PEFT) + SFT con TRL `SFTTrainer` |
| Versión en producción | v2 (150 conversaciones) |
| Entorno de entrenamiento | CPU, fp32, sin GPU |
| Adaptador | 73,911,112 bytes (~70.5 MiB) |
| Formato de despliegue | GGUF Q4_K_M |
| Tamaño del GGUF | 986,048,160 bytes (~940.4 MiB) |
| Runtime | LLamaSharp 0.30.0 + `LLamaSharp.Backend.Cpu` 0.30.0 |
| Contexto | 2048 tokens |
| Ubicación | `Documents\CafeteriaPOS\Modelos\morgana.gguf` |
| Dependencias en ejecución | Ninguna externa: sin API, sin servidor, sin Python |

## 4.2 Por qué una IA local

### 4.2.1 Privacidad

La base contiene ventas, propinas, nombres de empleados y precios. Enviar esas consultas a un servicio en la nube obligaría a confiar en un tercero y a exponer información del negocio. Con un GGUF local, el texto de la pregunta y la respuesta nunca salen del equipo.

### 4.2.2 Disponibilidad

Una cafetería no puede depender de que haya internet para consultar el menú o las ventas del día. El modelo y la base viven en el mismo disco; si la red falla, el POS y Morgana siguen funcionando.

### 4.2.3 Costo y latencia

No hay costo por token ni cuota mensual. La primera carga del modelo tarda unos segundos; después, una respuesta corta en CPU tarda entre 1 y 3 segundos en un equipo con AVX2. Para consultas de datos puros, la respuesta es inmediata porque no pasa por el modelo.

### 4.2.4 Límites que impone la elección

Un modelo de 1.5B no razona como uno de 70B. Puede confundir el formato, inventar un comodín o repetir una frase. El diseño de la aplicación asume esa realidad: la IA redacta, pero el código valida y sustituye los datos. Esa separación es la decisión más importante de todo el subsistema.

## 4.3 Modelo base y LoRA

### 4.3.1 Qwen2.5-1.5B-Instruct

Se eligió Qwen2.5-1.5B-Instruct por tres razones:

- **Tamaño:** cabe en memoria y corre en CPU sin GPU dedicada.
- **Calidad:** es un modelo instructivo moderno, con buen español y buen seguimiento de formato.
- **Licencia y disponibilidad:** está publicado en Hugging Face y es fácil de descargar y convertir.

El modelo completo en fp32 ocupa alrededor de 6 GB durante el entrenamiento; el adaptador final solo guarda los pesos nuevos.

### 4.3.2 Qué es LoRA

LoRA (Low-Rank Adaptation) congela los pesos del modelo base e inserta matrices de bajo rango en las capas de atención y MLP. En este proyecto:

| Parámetro | Valor |
|---|---|
| `r` (rango) | 16 |
| `lora_alpha` | 32 |
| `lora_dropout` | 0.05 |
| `bias` | none |
| `use_dora` | false |
| `use_rslora` | false |
| `target_modules` | `q_proj`, `k_proj`, `v_proj`, `o_proj`, `gate_proj`, `up_proj`, `down_proj` |

El adaptador resultante pesa ~70.5 MB, frente a los ~3 GB del modelo base. Eso permite versionarlo, copiarlo y fusionarlo sin mover el modelo completo.

### 4.3.3 Por qué no fine-tuning completo

Un ajuste completo de todos los pesos habría requerido más memoria, más tiempo y un archivo final mucho más grande. LoRA ofrece un equilibrio razonable para un dataset pequeño y un objetivo acotado: enseñar a Morgana el tono, el formato de comodines y las reglas del dominio.

## 4.4 El dataset

### 4.4.1 Formato

Archivo: `dataset\dataset.jsonl`. Una conversación por línea, UTF-8, sin comas entre líneas y **sin mensaje de sistema**:

```json
{"messages":[{"role":"user","content":"¿Cuánto cuesta el capuchino?"},{"role":"assistant","content":"El capuchino cuesta «PRECIO:capuchino», humano."}]}
```

### 4.4.2 Tamaño y crecimiento

| Versión | Ejemplos | Uso |
|---|---:|---|
| `dataset_20.jsonl` | 20 | Prueba de humo |
| `dataset_70.jsonl` | 70 | Primera ampliación |
| `dataset_120.jsonl` | 120 | Corrida v1 |
| `dataset.jsonl` | 150 | Corrida v2 (producción) |

Los 30 ejemplos nuevos están en `nuevos_121_150.jsonl` y se integraron al archivo final. No hubo split de validación: los 150 ejemplos se usaron completos para entrenar.

### 4.4.3 Regla de oro: comodines, no datos literales

Ningún precio, stock, venta o propina se escribe literal en el dataset. En su lugar se usan comodines entre guillemets:

```text
«PRECIO:capuchino»     «STOCK:ramen»        «PRESUPUESTO:100»
«VENTAS_HOY»           «TICKETS_HOY»        «PROPINAS_POR_EMPLEADO»
```

La aplicación sustituye esos comodines **después** de la inferencia. Así el modelo nunca ve las cifras y no puede memorizarlas ni inventarlas cuando cambian.

### 4.4.4 Tono

El dataset enseña un español de México breve y amable, con la personalidad de Morgana:

- “humano” en ~30 % de las respuestas;
- “¡Miau!” en ~20 %;
- “No me subestimes” en ~10 %;
- “No soy un gato” solo en 2 ejemplos;
- sin combos, descuentos ni acciones que la app no puede ejecutar.

El tono se aprende de los ejemplos, no de un prompt de sistema. La app no envía un mensaje de sistema al modelo; solo el texto del usuario.

## 4.5 Los comodines

### 4.5.1 Lista blanca del runtime

El dataset usa 18 claves. La lista blanca del código añade `MENU`, que se usa como clave de control para las respuestas deterministas de menú. En total, el runtime reconoce 19 entradas:

| Comodín | Argumento | Sustitución |
|---|---|---|
| `PRECIO` | nombre de producto | Precio con formato `$0.00` |
| `STOCK` | nombre de producto | “quedan N”, “queda 1” o “no queda nada” |
| `PRESUPUESTO` | monto | Combinación de hasta 3 productos que cabe en el monto |
| `MENU` | categoría opcional | Menú completo o de una categoría, solo productos con stock |
| `VENTAS_HOY` | — | Total con propinas de hoy (o histórico si la pregunta lo pide) |
| `TICKETS_HOY` | — | Cantidad de ventas de hoy |
| `TICKET_PROMEDIO` | — | Total de hoy / tickets de hoy |
| `VENTAS_EFECTIVO` | — | Total de hoy pagado en efectivo |
| `VENTAS_TARJETA` | — | Total de hoy pagado con tarjeta |
| `VENTAS_HISTORICO` | — | Total acumulado de todas las ventas |
| `TICKETS_HISTORICO` | — | Cantidad acumulada de tickets |
| `PROPINAS_HOY` | — | Propinas de hoy |
| `PROPINAS_EFECTIVO` | — | Propinas de hoy en efectivo |
| `PROPINAS_TARJETA` | — | Propinas de hoy con tarjeta |
| `PROPINAS_POR_EMPLEADO` | — | Desglose por empleado; si la pregunta nombra a uno, solo ese |
| `STOCK_BAJO` | — | Productos con stock igual o menor al mínimo |
| `TOP_PRODUCTOS` | — | Hasta 3 productos más vendidos (hoy, 7 días o histórico) |
| `HORARIO` | — | Valor de `configuracion.json` o el valor por defecto |
| `PAGOS` | — | Formas de pago configuradas |

### 4.5.2 Validación

`ComodinResolver` recorre la respuesta con una expresión regular que busca `«...»`. Para cada coincidencia:

1. Separa el nombre y el argumento por `:`.
2. Verifica que el nombre esté en la lista blanca.
3. Resuelve el valor con los servicios reales (`ProductoService`, `VentaService`).
4. Si el producto no existe o falta el argumento, invalida toda la respuesta.
5. Si al final queda un `«` o `»` suelto, también la invalida.

Una respuesta inválida **no se muestra nunca a medias**. Se descarta completa, se reintenta con otra semilla y, si vuelve a fallar, se usa una frase determinista.

### 4.5.3 Frases de respaldo

| Constante | Texto |
|---|---|
| `SinInformacion` | “De eso no tengo información, humano. No me subestimes: solo llevo menú, stock, ventas, propinas y horario.” |
| `ProductoNoExiste` | “Ese producto no está en el menú, humano. ¿Te digo qué sí hay?” |
| `FaltaProducto` | “¿De qué producto, humano? Dime cuál y te digo el dato.” |

## 4.6 Entrenamiento

### 4.6.1 Software

| Librería | Versión observada |
|---|---|
| PEFT | 0.21.2 |
| TRL | 1.16.0 |
| Transformers | 5.21.0 |
| PyTorch | 2.14.1 |
| Datasets | 5.1.0 |
| Tokenizers | 0.23.3 |

El entorno se creó en un `venv` aislado con Python 3.11. Los comandos se ejecutaron en PowerShell con `PYTHONIOENCODING=utf-8`.

### 4.6.2 Hiperparámetros de la corrida v2

| Parámetro | Valor |
|---|---|
| Épocas | 2 |
| Batch por dispositivo | 1 |
| Acumulación de gradiente | 4 |
| Batch efectivo | 4 conversaciones por paso |
| Learning rate | `1e-4` constante |
| Scheduler | constant |
| `max_length` | 512 tokens |
| `packing` | false |
| `logging_steps` | 1 |
| `save_strategy` | epoch |
| `save_total_limit` | 2 |
| `use_cpu` | true |
| Precisión | fp32 (`bf16=false`, `fp16=false`) |
| Semilla | 42 |
| `use_cache` | false |

El script `entrenar.py` tiene `2e-4` por defecto; la v2 se lanzó con `--lr 1e-4`, como confirman los logs. La v1 (120 ejemplos) sí usó `2e-4`.

### 4.6.3 Comando de entrenamiento

```powershell
python scripts\entrenar.py --epocas 2 --lr 1e-4 --salida salida\morgana-lora-v2
```

Prueba rápida antes de una corrida completa:

```powershell
python scripts\entrenar.py --epocas 1 --limite 20 --salida salida\prueba
```

### 4.6.4 Resultados de la v2

| Métrica | Valor |
|---|---:|
| Ejemplos | 150 |
| Pasos | 76 |
| Duración | 2,512 s (41 min 52 s) |
| Pérdida paso 1 | 4.8242 |
| Pérdida paso 76 | 0.5717 |
| `train_loss` promedio | 1.2513 |
| Token accuracy final | 85.04 % |
| Velocidad | 0.119 muestras/s; 0.03 pasos/s |
| Tokens procesados | ~20,440 |
| Checkpoints | `checkpoint-38` y `checkpoint-76` (~222.6 MB cada uno) |

La pérdida baja rápido en los primeros 20 pasos y luego oscila entre ~0.5 y ~0.9. No hay sobreajuste evidente con 2 épocas, pero tampoco hay validación formal: el dataset es pequeño y se entrenó completo.

> **Nota sobre `train_loss`:** el valor 1.2513 es el promedio ponderado de todo el entrenamiento, no la pérdida del último paso. La pérdida puntual del paso 76 es 0.5717. Por eso el resumen parece más alto que la curva final.

### 4.6.5 Comparación con la v1

| Métrica | v1 (120) | v2 (150) |
|---|---:|---:|
| Learning rate | `2e-4` | `1e-4` |
| Pasos | 60 | 76 |
| Duración | 38 min 46 s | 41 min 52 s |
| Pérdida final | 0.7007 | 0.5717 |
| `train_loss` | 1.1431 | 1.2513 |
| Token accuracy | 84.6 % | 85.04 % |
| Evaluación 20 preguntas | 12/20 (60 %) | 9/20 (45 %) |

La v2 tiene mejor pérdida y mejor precisión de token, pero en la prueba automática de 20 preguntas acertó menos. El motivo principal fueron comodines inventados y respuestas repetitivas. La app mitiga ese riesgo con la lista blanca, el reintento y el respaldo determinista.

## 4.7 Fusión, conversión y cuantización

### 4.7.1 Pipeline

```text
dataset.jsonl
  → SFT + LoRA sobre Qwen2.5-1.5B-Instruct (CPU)
  → adaptador LoRA
  → merge_and_unload() con el modelo base
  → convert_hf_to_gguf.py (f16)
  → llama-quantize.exe (Q4_K_M)
  → morgana.gguf
  → Documents\CafeteriaPOS\Modelos\morgana.gguf
  → LLamaSharp lo carga en la app
```

### 4.7.2 Pasos

1. Cargar el modelo base en fp16 con `low_cpu_mem_usage`.
2. Aplicar el adaptador con `PeftModel.from_pretrained` y fusionarlo con `merge_and_unload()`.
3. Guardar el modelo fusionado con `safe_serialization` y shards de 2 GB.
4. Convertir a GGUF f16 con `convert_hf_to_gguf.py` (`--outtype f16`).
5. Cuantizar con `llama-quantize.exe` a `Q4_K_M`.

### 4.7.3 Tamaños

| Etapa | Tamaño |
|---|---:|
| Modelo f16 | 2,944.68 MiB (~3.09 GB) |
| Modelo Q4_K_M | 934.69 MiB (5.08 bits por peso) |
| GGUF final | 986,048,160 bytes (~940.4 MiB) |
| Tiempo de cuantización | 42.5 s |

La cuantización Q4_K_M reduce el modelo de ~3 GB a ~940 MB con una pérdida de calidad pequeña. Es el equilibrio habitual para ejecutar en CPU.

### 4.7.4 Comando de conversión

```powershell
python scripts\convertir_gguf.py `
  --adaptador salida\morgana-lora-v2 `
  --salida salida\morgana-v2-ep2-q4_k_m.gguf `
  --tipo q4_k_m
```

Requiere las herramientas de `llama.cpp` en `tools\`. Los intermedios se borran al terminar, salvo que se use `--conservar`.

## 4.8 Integración en la aplicación

### 4.8.1 Componentes

| Componente | Responsabilidad |
|---|---|
| `AiChatWindow` | Interfaz del chat, historial visual, envío y limpieza |
| `MorganaService` | Carga del GGUF, prompt, inferencia, semáforo y errores |
| `ComodinResolver` | Lista blanca, detección de intención y sustitución |
| `JsonDatabase` | Lectura de productos, ventas y detalles |
| `AppPaths` | Ruta del modelo y de los datos |

### 4.8.2 Carga del modelo

`MorganaService` es un singleton perezoso. La primera vez que se abre el chat, `PrecalentarAsync` carga el modelo en segundo plano para que la primera pregunta no espere. La carga usa:

```csharp
var parametros = new ModelParams(AppPaths.Modelo)
{
    ContextSize = 2048,
    GpuLayerCount = 0,
    BatchSize = 512,
    Threads = Math.Clamp(Environment.ProcessorCount / 2, 2, 8)
};
```

`ConfigurarNativos` detecta AVX-512, AVX2, AVX o no-AVX y llama a `SetDllDirectory` sobre la carpeta `runtimes\win-x64\native\{carpeta}`. Es necesario porque el paquete `netstandard2.0` de LLamaSharp no busca los nativos en la ruta habitual de .NET 8.

### 4.8.3 Prompt

Se usa la plantilla ChatML de Qwen, sin mensaje de sistema:

```text
<|im_start|>user
{pregunta}
<|im_end|>
<|im_start|>assistant
```

El modelo fue entrenado sin system prompt; el rol y el tono se aprenden de los ejemplos. Añadir uno en runtime cambiaría la distribución con la que se entrenó y podría empeorar las respuestas.

### 4.8.4 Parámetros de generación

| Parámetro | Valor |
|---|---|
| `MaxTokens` | 120 |
| `Temperature` | 0.3 |
| `TopP` | 0.9 |
| `TopK` | 40 |
| `RepeatPenalty` | 1.1 |
| `Seed` | 42 en el primer intento; 42 + 7919 en el segundo |
| `AntiPrompts` | `<|im_end|>`, `<|im_start|>` |

La temperatura baja favorece respuestas repetibles y reduce la creatividad no deseada. El segundo intento cambia la semilla para intentar salir de una salida inválida.

### 4.8.5 Flujo de una pregunta

![Figura 4.1. Flujo de una pregunta desde el chat hasta la respuesta final](recursos/flujo-ia.png){width=16cm}

```text
Usuario escribe
  → AiChatWindow muestra “Pensando...”
  → MorganaService.PreguntarAsync
      → ¿Es consulta determinista? (menú, recaudado, propinas, conteo, top)
          → sí: responde con datos reales, sin modelo
      → no: carga el modelo si hace falta
          → genera hasta 2 veces
          → ComodinResolver valida y sustituye
          → si falla: frase de respaldo
  → se muestra la respuesta
```

### 4.8.6 Consultas deterministas

Antes de llamar al modelo, `MorganaService` detecta intenciones de datos puros:

| Intención | Detección | Respuesta |
|---|---|---|
| Menú | `EsConsultaMenu` | `RespuestaMenu` |
| Recaudado de hoy | `EsConsultaRecaudadoHoy` | `RespuestaRecaudado` |
| Propinas | `EsConsultaPropinas` | `RespuestaPropinas` |
| Cantidad de ventas | `EsConsultaCantidadVentas` | `RespuestaCantidadVentas` |
| Top de productos | `EsConsultaTopProductos` | `RespuestaTopProductos` |

Estas respuestas no dependen del modelo y siempre coinciden con la base. Es una decisión de diseño: un modelo de 1.5B puede enumerar mal un catálogo o contar ventas de forma inconsistente; el código no.

### 4.8.7 Concurrencia y errores

- `SemaphoreSlim(1,1)` serializa las preguntas: no hay dos inferencias a la vez.
- El botón de enviar se deshabilita mientras se espera.
- Los errores se registran en `Data\morgana_error.log` con fecha y detalle.
- Si el modelo no está, se devuelve un mensaje con la ruta esperada.
- Si la inferencia falla, se devuelve “No pude responder, humano. Intenta otra vez.”

### 4.8.8 Liberación de recursos

`MorganaService` implementa `IDisposable`: libera los pesos y el ejecutor. La ventana del asistente se cierra al cerrar sesión o la ventana principal, pero el singleton puede conservar el modelo cargado hasta que termine el proceso. En una sesión larga, eso evita recargar el GGUF en cada apertura del chat.

## 4.9 Evaluación

### 4.9.1 Prueba automática

Se usaron 20 preguntas con dos semillas, comparando v1 y v2:

| Versión | Aciertos | Porcentaje |
|---|---:|---:|
| v1 (120 ejemplos, lr `2e-4`) | 12/20 | 60 % |
| v2 (150 ejemplos, lr `1e-4`) | 9/20 | 45 % |

### 4.9.2 Fallos observados

- Comodines inventados: `«VENTAS»`, `«PROPINAS»`, `«STOCK_RAMEN»`, `«PRECIO_DESCUENTO»`, `«TICKETS:capuchino»`, `«PAGOS Efectivo»`.
- Repetición del mismo comodín en una frase.
- Respuestas que prometen acciones imposibles (cancelar ventas, aplicar descuentos).
- Confusión con productos que no existen (té, pizza, combos).
- Uso de `«PRECIO:»` o `«STOCK:»` sin el nombre del producto.

### 4.9.3 Pruebas manuales

Además de la comparativa automática, se hicieron pruebas manuales del primer GGUF y de la época 1 frente a la época 2. Los resultados están en `logs\pruebas_modelo.md` y `logs\pruebas_v2.md`.

### 4.9.4 Conclusión de la evaluación

El modelo aprendió el formato, el tono y el uso correcto de los comodines en la mayoría de los casos, pero **no es fiable al 100 %**. La app está diseñada asumiendo esa limitación. La calidad visible para el usuario es mayor que el 45 % de la prueba automática porque las consultas más frecuentes (menú, ventas, propinas, top) se responden con código y porque el resolvedor descarta las salidas inválidas.

## 4.10 Límites y riesgos

| Límite | Impacto | Mitigación |
|---|---|---|
| Modelo de 1.5B | Menor capacidad de razonamiento y de español fino | Consultas deterministas + lista blanca |
| Sin split de validación | No hay métrica de generalización | Pruebas manuales y comparativa de 20 preguntas |
| Dataset pequeño (150) | Puede repetir fórmulas o confundir productos | Reintento y frases de respaldo |
| Comodines inventados | Respuesta inválida | `ComodinResolver` descarta y reintenta |
| Productos nuevos | El modelo no los conoce por nombre | `BuscarProducto` hace coincidencia flexible |
| Contexto 2048 | No hay memoria de conversación larga | Cada pregunta es independiente |
| Sin GPU | Latencia mayor | Respuestas cortas y consultas deterministas |
| Modelo en disco | ~940 MB | Copia única, sin cargarlo en el repositorio |

### 4.10.1 Lo que Morgana nunca debe hacer

- Inventar precios, stock o ventas.
- Aplicar descuentos o cancelar ventas.
- Modificar la base.
- Prometer acciones que la aplicación no tiene.
- Enviar datos a internet.

El código no le da herramientas para ejecutar acciones: solo puede generar texto. Cualquier intento de “hacer” algo se queda en una respuesta.

## 4.11 Actualizar o reentrenar

### 4.11.1 Cuándo reentrenar

- Cambia el tono o la personalidad de Morgana.
- Se añaden nuevas intenciones que el modelo debe redactar.
- Se detectan fallos sistemáticos en las pruebas.
- Se quiere mejorar el español o la variedad de respuestas.

No es necesario reentrenar para cambiar precios, stock, productos, horarios o formas de pago: esos datos se resuelven en runtime.

### 4.11.2 Requisitos

El paquete `Morgana_IA_Local_20261028` **no** incluye, por tamaño:

- `base\Qwen2.5-1.5B-Instruct\` (~2.94 GB);
- `venv\`;
- `tools\llama.cpp-b11932\` y `llama-quantize.exe`;
- `salida\*.gguf`;
- checkpoints del adaptador.

Sí incluye el adaptador final, el dataset, los scripts, los logs y las copias de referencia de la integración.

### 4.11.3 Reproducir desde cero

```powershell
py -3.11 -m venv venv
.\venv\Scripts\Activate.ps1
pip install -U pip
pip install torch transformers datasets peft trl accelerate safetensors

pip install -U "huggingface_hub[cli]"
huggingface-cli download Qwen/Qwen2.5-1.5B-Instruct --local-dir base\Qwen2.5-1.5B-Instruct

python scripts\entrenar.py --epocas 2 --lr 1e-4 --salida salida\morgana-lora-v2

python scripts\convertir_gguf.py `
  --adaptador salida\morgana-lora-v2 `
  --salida salida\morgana-v2-ep2-q4_k_m.gguf `
  --tipo q4_k_m

Copy-Item "salida\morgana-v2-ep2-q4_k_m.gguf" `
  "$([Environment]::GetFolderPath('MyDocuments'))\CafeteriaPOS\Modelos\morgana.gguf" -Force
```

### 4.11.4 Buenas prácticas para un nuevo dataset

1. Mantener la regla de oro: ningún dato variable literal.
2. Cubrir variaciones de la misma pregunta (formal, informal, con error).
3. Incluir negativas y preguntas fuera de alcance.
4. No prometer acciones que la app no tiene.
5. Separar un conjunto de validación antes de entrenar.
6. Medir con las mismas 20 preguntas para comparar versiones.
7. Versionar el dataset y el adaptador con fecha.

### 4.11.5 Validación antes de publicar

| Prueba | Criterio |
|---|---|
| Precio de un producto existente | Devuelve el precio real |
| Producto inexistente | Responde “no está en el menú” |
| Stock bajo | Lista los productos correctos |
| Ventas de hoy | Coincide con Reportes |
| Propinas por empleado | Coincide con Reportes |
| Menú | Solo productos con stock |
| Pregunta fuera de alcance | No inventa; usa respaldo |
| Modelo ausente | El POS sigue funcionando |

## 4.12 Privacidad y consideraciones éticas

- **Datos locales:** la base, los tickets y el modelo viven en el equipo. No hay telemetría.
- **Sin entrenamiento con datos del cliente:** el dataset es sintético y de dominio; no se enviaron ventas reales a un servicio externo.
- **Sesgo y errores:** un modelo pequeño puede equivocarse. La interfaz no presenta sus respuestas como verdad absoluta y el usuario puede verificar en Reportes.
- **Transparencia:** el manual y este documento explican los límites. No se oculta que la evaluación fue de 45 % en la prueba automática.
- **Propiedad intelectual:** Qwen2.5 se usa bajo su licencia; la inspiración de *Persona 5* es académica y no comercial.

## 4.13 Glosario del capítulo

| Término | Definición |
|---|---|
| Adaptador | Pesos nuevos de LoRA que se aplican sobre el modelo base |
| Comodín | Marca entre `«»` que la app sustituye por un dato real |
| ChatML | Formato de prompt con roles `user` y `assistant` |
| GGUF | Formato de archivo de modelos para llama.cpp |
| LoRA | Ajuste de bajo rango que congela el modelo base |
| Q4_K_M | Cuantización de 4 bits con mezcla de precisiones |
| SFT | Entrenamiento supervisado con ejemplos pregunta-respuesta |
| Token | Unidad mínima de texto que procesa el modelo |
| `train_loss` | Pérdida promedio de todo el entrenamiento |
| Token accuracy | Porcentaje de tokens que el modelo predice correctamente |
