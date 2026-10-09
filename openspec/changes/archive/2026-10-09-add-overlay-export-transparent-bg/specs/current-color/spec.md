# Delta: current-color

## ADDED Requirements

### Requirement: Exportar Anterior y Actual como superpuestos
En la tarjeta "Color actual", en la fila de acciones que comienza con "Copiar todo", SHALL existir un botón "Exportar superpuestos" (única opción de exportación allí) que SHALL abrir la ventana de exportación de superpuestos con los dos colores "Anterior" y "Actual", en ese orden (Anterior como primera capa, Actual como última) y con las mismas opciones de forma, orden, escala, fondo y valores que la exportación de superpuestos de paletas.

#### Scenario: Exportar la dupla Anterior/Actual
- **WHEN** el color es `#675BCE`, "Anterior" es `#5246BC` y el usuario presiona "Exportar superpuestos" en la tarjeta de color actual
- **THEN** se abre la ventana de superpuestos con la dupla [Anterior `#5246BC`, Actual `#675BCE`], con la doble capa visible y sus valores (nombre/HEX/RGB) listados