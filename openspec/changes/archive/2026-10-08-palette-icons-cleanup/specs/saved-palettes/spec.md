## MODIFIED Requirements

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