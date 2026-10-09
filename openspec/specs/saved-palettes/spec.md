# saved-palettes Specification

## Purpose
Permite crear, organizar y conservar paletas de colores con nombre, guardadas localmente de forma automática y segura.

## Requirements

### Requirement: Modelo de paleta
Una paleta SHALL tener identificador único, nombre (obligatorio, no vacío), descripción opcional, fecha de creación, fecha de última modificación y una lista ordenada de colores. Cada color de paleta SHALL tener identificador, nombre opcional (por ejemplo "Primary") y valor ARGB.

#### Scenario: Nueva paleta
- **WHEN** el usuario crea una paleta sin indicar nombre
- **THEN** se crea con nombre "Paleta sin título" (con sufijo numérico si ya existe), fechas de creación y modificación iguales y sin colores

### Requirement: Operaciones de gestión
El sistema SHALL permitir: crear, renombrar, editar descripción, duplicar (copia con nombre "<nombre> (copia)" y nuevas fechas) y eliminar paletas (con confirmación o posibilidad de deshacer); y dentro de una paleta: agregar el color actual (vía el botón "+ Paleta" de la zona superior del color actual o `Ctrl+S`; la cabecera de la paleta activa no SHALL presentar un botón "+" duplicado), agregar colores generados, renombrar un color, reemplazar un color por el color actual, eliminar un color (desde su menú contextual o desde un pequeño tachito de borrado `X` que SHALL hacerse visible al pasar el mouse sobre la muestra), reordenar colores (arrastrar y soltar, y acciones mover izquierda/derecha), copiar un color y copiar la paleta completa. Toda modificación SHALL actualizar la fecha de modificación. El reordenamiento por arrastre SHALL ser evidente: al pasar el mouse sobre una muestra, junto al tachito `X` SHALL existir una indicación visible de que la muestra se puede arrastrar para moverla (por ejemplo flechas o el texto del tooltip que distinga click = usar como color principal de arrastre = mover); durante el arrastre la muestra de origen SHALL verse atenuada (ligeramente más pequeña o menos opaca) y SHALL mostrarse un separador vertical de inserción que señale en todo momento entre qué muestras caerá el color arrastrado; soltar la muestra sobre el espacio vacío al final del área SHALL mover el color a la última posición. El orden resultante SHALL persistir.

#### Scenario: Agregar color actual
- **WHEN** la paleta activa es "PuchiApp" y el usuario pulsa "+ Paleta" (o `Ctrl+S`)
- **THEN** el color actual se agrega al final de "PuchiApp" y su fecha de modificación se actualiza

#### Scenario: Reordenar
- **WHEN** el usuario arrastra el tercer color a la primera posición
- **THEN** el orden persiste tras reiniciar la aplicación

#### Scenario: Indicación al pasar el mouse
- **WHEN** el usuario pasa el mouse sobre un color de la paleta activa
- **THEN** junto al tachito `X` se muestra la indicación de arrastre (flechas o texto del tipo "arrastrar para mover") y el tooltip distingue click (usar como principal) de arrastre (mover)

#### Scenario: Guía visual durante el arrastre
- **WHEN** el usuario arrastra el segundo color hacia la cuarta posición
- **THEN** la muestra de origen se ve atenuada y un separador vertical señala en todo momento entre qué muestras caerá, mientras ninguna tarjeta cambia su orden hasta soltar

#### Scenario: Soltar al final
- **WHEN** el usuario arrastra el primer color y lo suelta sobre el espacio vacío después de la última muestra
- **THEN** el color pasa a la última posición, la fecha de modificación se actualiza y el orden persiste tras reiniciar la aplicación

#### Scenario: Eliminar paleta
- **WHEN** el usuario elimina una paleta
- **THEN** la paleta desaparece de la lista y puede recuperarse con "Deshacer" mientras el aviso esté visible

#### Scenario: Borrar un color al pasar el mouse
- **WHEN** el usuario pasa el mouse sobre el segundo color de "PuchiApp", se muestra el tachito `X` en la esquina superior derecha de la muestra y hace click sobre él
- **THEN** ese color se elimina de "PuchiApp", la fecha de modificación se actualiza y el cambio persiste tras reiniciar la aplicación; el color eliminado no copia nada al portapapeles ni cambia el color actual, y ni el click en la muestra ni el arrastre de reordenación se interrumpen fuera del tachito

### Requirement: Paleta activa
El sistema SHALL mantener una paleta activa visible permanentemente en la ventana principal como fila de muestras con sus nombres, que sea el destino de "Agregar". Hacer click en una muestra SHALL convertirla en el color actual. Si no existe ninguna paleta, "Agregar" SHALL crear una nueva automáticamente.

#### Scenario: Primera vez
- **WHEN** no hay paletas y el usuario pulsa "Agregar a paleta"
- **THEN** se crea "Paleta sin título", pasa a ser la activa y contiene el color actual

### Requirement: Persistencia local automática
Las paletas SHALL guardarse automáticamente tras cada cambio (sin acción "Guardar") en un archivo JSON legible en `%APPDATA%\Matiz\palettes.json` con un campo de versión de esquema. La escritura SHALL ser atómica (archivo temporal + reemplazo) para no corromper datos ante cierres inesperados.

#### Scenario: Persistencia entre sesiones
- **WHEN** el usuario crea una paleta con 3 colores, cierra y vuelve a abrir la aplicación
- **THEN** la paleta aparece con los mismos nombres, colores, orden y fechas

#### Scenario: Archivo corrupto
- **WHEN** `palettes.json` no es JSON válido al iniciar
- **THEN** el sistema lo renombra a `palettes.corrupt-<fecha>.json`, inicia con lista vacía y avisa al usuario sin cerrarse

#### Scenario: Versión de esquema
- **WHEN** se lee un archivo con una versión de esquema anterior conocida
- **THEN** se migra al esquema actual sin pérdida de datos

### Requirement: Máximo de colores por paleta
Toda paleta guardada SHALL tener como máximo 64 colores. Regla dura aplicada en todos los flujos que añaden colores a una paleta —agregar el color actual, agregar colores generados (individual o "Añadir todo"), recargar la paleta en la rueda, cambiar de paleta o seleccionar colores—: nunca SHALL superarse el límite y ninguna paleta existente SHALL recortarse. Si al intentar añadir la paleta ya alcancó el límite, o al añadir en lote la cantidad final excedería el límite, el sistema SHALL bloquear la operación completa, SHALL no añadir ningún color y SHALL advertir con un toast con el texto "Máximo 64 colores por paleta" (las paletas guardadas con más colores por archivos externos SHALL conservarse intactas: el límite aplica solo a añadir nuevos colores, sin bloquear lectura, edición de otros aspectos ni exportación). El conteo SHALL ser siempre correcto: al recargar, al cambiar la paleta marcada o al seleccionar colores, el sistema SHALL evaluar el límite sobre el número real de colores de la paleta.

#### Scenario: Añadir al alcanzar el límite
- **WHEN** la paleta activa tiene 64 colores y el usuario pulsa "+ Paleta" (o `Ctrl+S`)
- **THEN** no se añade ningún color, la paleta sigue con 64 colores, su fecha de modificación no cambia y se muestra el toast "Máximo 64 colores por paleta"

#### Scenario: Lote que no cabe no añade nada
- **WHEN** la paleta activa tiene 60 colores y el usuario pulsa "Añadir todo" sobre 6 colores generados
- **THEN** no se añade ningún color (60 + 6 excedería 64), la paleta queda con 60 y se muestra el toast "Máximo 64 colores por paleta"

#### Scenario: Lote con hueco disponible
- **WHEN** la paleta activa tiene 3 colores y el usuario pulsa "Añadir todo" sobre 10 colores generados
- **THEN** se añaden los 10 colores al final en orden y la paleta queda con 13

#### Scenario: Paleta externa de más de 64 colores se conserva
- **WHEN** el archivo `palettes.json` contiene una paleta con 70 colores (creada fuera del límite) y el usuario la abre
- **THEN** la paleta se muestra y se exporta completa (70 colores), se puede renombrar y borrar/reordenar colores, pero añadir está bloqueado con el aviso hasta que quede bajo el límite

### Requirement: Recargar la paleta marcada en la rueda
El panel Biblioteca ("Paletas") SHALL presentar un botón "Recargar" al lado de "Nueva" que cargue la paleta marcada (activa) en la rueda cromática en modo Libre. El botón SHALL deshabilitarse si no existe paleta marcada con colores o si la rueda ya muestra esa paleta sin cambios que editar. Al pulsarlo el sistema SHALL advertir antes de cargar —toast con el texto "Se cargará la paleta en la rueda cromática y se perderán las selecciones actuales." y un botón de confirmación "Recargar"— y sin confirmar SHALL no cambiar nada. Al confirmar: el primer color de la paleta SHALL pasar a ser el color actual (cambio confirmado) y el principal del conjunto; cada color restante SHALL añadirse como punto secundario en su posición hue/saturación relativa al principal con su brillo propio de modo que cada tarjeta SHALL reproducir el color exacto de la paleta; el conjunto libre previo SHALL reemplazarse; la paleta marcada SHALL evaluarse al confirmar (si entre el aviso y la confirmación se marca otra paleta, se carga esa); la operación SHALL respetar los límites (una paleta de 64 colores carga principal + 63 secundarios, dentro del límite de la rueda) y el panel Biblioteca SHALL cerrarse para mostrar la rueda. Si la paleta tiene un solo color SHALL cargar solo el principal. No SHALL añadir colores a la paleta ni modificarla.

#### Scenario: Aviso antes de cargar
- **WHEN** hay una paleta marcada con 8 colores y el usuario pulsa "Recargar"
- **THEN** aparece el toast "Se cargará la paleta en la rueda cromática y se perderán las selecciones actuales." con botón "Recargar", y hasta confirmar no cambian la rueda, el color actual ni las selecciones

#### Scenario: Carga completa en Libre
- **WHEN** el usuario confirma "Recargar" sobre la paleta "PuchiApp" con colores `#5246BC`, `#E24347`, `#F7F9FB`
- **THEN** la pestaña pasa a Libre, el color actual pasa a ser `#5246BC` como cambio confirmado, aparecen los puntos 1 (`#E24347`) y 2 (`#F7F9FB`) en las posiciones hue/saturación de la rueda correspondientes con su brillo propio y el panel Biblioteca se cierra

#### Scenario: Conteo correcto al recargar una paleta llena
- **WHEN** la paleta marcada tiene 64 colores y el usuario confirma "Recargar"
- **THEN** el conjunto carga principal + 63 secundarios (65 colores en total) sin aviso de límite de rueda y la rueda queda editable

#### Scenario: Sin paleta con colores
- **WHEN** no existe ninguna paleta, o la única paleta existente no tiene colores, y el usuario abre el panel "Paletas"
- **THEN** el botón "Recargar" está deshabilitado y no muestra el aviso

#### Scenario: Solo principal
- **WHEN** la paleta marcada tiene un único color y el usuario confirma "Recargar"
- **THEN** la pestaña pasa a Libre con solo la tarjeta base (ese color como color actual), sin puntos secundarios, y se cierra el panel Biblioteca
