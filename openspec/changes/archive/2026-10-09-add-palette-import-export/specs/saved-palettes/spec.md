## ADDED Requirements

### Requirement: Presentación del panel Biblioteca
El panel Biblioteca SHALL titularse "Paletas de Colores" (EN: "Color Palettes") y, debajo del editor de nombre y descripción de la paleta marcada, SHALL presentar una fila de acciones con el botón "Exportar" seguido del botón "Importar" —siempre en ese orden y con "Importar" a la derecha— alineada al borde inferior derecho. Cuando no existe paleta marcada (lista vacía) SHALL quedar visible únicamente el botón "Importar", de modo que importar sea alcanzable desde una biblioteca vacía; "Exportar" SHALL deshabilitarse cuando la paleta marcada no tenga colores.

#### Scenario: Fila con paleta marcada
- **WHEN** hay una paleta marcada con colores
- **THEN** debajo de la descripción se ven los botones "Exportar" e "Importar" en ese orden, con "Importar" a la derecha

#### Scenario: Sin paleta marcada
- **WHEN** la biblioteca no tiene paletas y se abre el panel
- **THEN** solo se muestra el botón "Importar" (sin los campos de nombre/descripción ni "Exportar")

### Requirement: Preview de la biblioteca sin truncar
La mini-vista previa de cada paleta de la lista SHALL mostrar todos los colores en su orden (hasta el máximo de 64) dispuestos en filas que envuelven, en lugar de truncarse a los primeros 12: la primera fila SHALL mostrar el máximo que entra en el ancho del panel y los restantes SHALL envolver a la fila siguiente.

#### Scenario: Paleta con 13 colores
- **WHEN** la paleta marcada tiene 13 colores y se ve la lista en el panel Biblioteca
- **THEN** el preview de la paleta muestra los 13 colores (los primeros ~17 en la primera fila), ya no quedan ocultos por el límite de 12

#### Scenario: Paleta que envuelve a 2 filas
- **WHEN** la paleta marcada tiene 20 colores y se ve la lista
- **THEN** el preview los muestra en al menos 2 filas de muestras (por ejemplo 17 + 3), envueltas al ancho disponible del panel, sin truncar a 12