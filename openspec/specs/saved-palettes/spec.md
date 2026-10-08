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
El sistema SHALL permitir: crear, renombrar, editar descripción, duplicar (copia con nombre "<nombre> (copia)" y nuevas fechas) y eliminar paletas (con confirmación o posibilidad de deshacer); y dentro de una paleta: agregar el color actual (vía el botón "+ Paleta" de la zona superior del color actual o `Ctrl+S`; la cabecera de la paleta activa no SHALL presentar un botón "+" duplicado), agregar colores generados, renombrar un color, reemplazar un color por el color actual, eliminar un color (desde su menú contextual o desde un pequeño tachito de borrado `X` que SHALL hacerse visible al pasar el mouse sobre la muestra), reordenar colores (arrastrar y soltar, y acciones mover izquierda/derecha), copiar un color y copiar la paleta completa. Toda modificación SHALL actualizar la fecha de modificación.

#### Scenario: Agregar color actual
- **WHEN** la paleta activa es "PuchiApp" y el usuario pulsa "+ Paleta" (o `Ctrl+S`)
- **THEN** el color actual se agrega al final de "PuchiApp" y su fecha de modificación se actualiza

#### Scenario: Reordenar
- **WHEN** el usuario arrastra el tercer color a la primera posición
- **THEN** el orden persiste tras reiniciar la aplicación

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
