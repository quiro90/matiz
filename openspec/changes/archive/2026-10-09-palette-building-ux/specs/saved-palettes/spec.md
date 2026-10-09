# saved-palettes Delta

## MODIFIED Requirements

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