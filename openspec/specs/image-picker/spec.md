# image-picker Specification

## Purpose
Permite tomar colores de una imagen (capturas, mockups, fotos) dentro de la aplicación y, opcionalmente, extraer automáticamente sus colores predominantes como paleta.

## Requirements

### Requirement: Cargar imagen
El sistema SHALL permitir cargar una imagen PNG, JPEG, BMP, GIF (primer fotograma), TIFF o WebP (si el códec del sistema está disponible) mediante diálogo "Abrir imagen" (`Ctrl+O`), arrastrar y soltar sobre la ventana, o pegar una imagen del portapapeles (`Ctrl+V` cuando el portapapeles contiene una imagen). La imagen SHALL mostrarse en el área central ajustada al espacio disponible, sustituyendo temporalmente a la rueda, con una acción clara para volver al selector.

#### Scenario: Pegar captura
- **WHEN** el portapapeles contiene una captura de pantalla y el usuario pulsa `Ctrl+V`
- **THEN** la imagen se muestra en el área central lista para seleccionar colores

#### Scenario: Archivo no soportado
- **WHEN** el usuario arrastra un archivo que no es una imagen decodificable
- **THEN** se muestra un mensaje de error y el estado no cambia

### Requirement: Seleccionar color de la imagen
Al mover el cursor sobre la imagen el sistema SHALL mostrar una lupa con cuadrícula de píxeles de la imagen original y el HEX del píxel bajo el cursor; el click SHALL establecer ese color (de la imagen original, no del render escalado) como color actual y añadirlo al historial. El usuario SHALL poder hacer zoom (rueda del ratón) y desplazarse por la imagen.

#### Scenario: Píxel exacto de la imagen
- **WHEN** la imagen está reducida al 25% en pantalla y el usuario hace click sobre un área
- **THEN** el color actual es el valor del píxel correspondiente de la imagen original sin interpolación

### Requirement: Extraer colores principales
El sistema SHALL ofrecer "Extraer colores" que detecte entre 3 y 10 colores predominantes (por defecto 6) y los presente como paleta generada con las mismas acciones que el resto de tarjetas. El campo de cantidad SHALL etiquetarse "Cantidad de colores:" a la izquierda del control numérico. Al cargar una imagen (diálogo "Abrir imagen", arrastrar y soltar, o portapapeles), el sistema SHALL ejecutar la extracción una vez de forma automática con la cantidad vigente, sin bloquear la UI, y la extracción manual SHALL seguir disponible para re-ejecutarla (por ejemplo tras cambiar la cantidad). El resultado SHALL ser determinista para una misma imagen y número de colores, y SHALL completarse sin bloquear la UI.

#### Scenario: Extracción automática al cargar
- **WHEN** el usuario pega una captura de pantalla con `Ctrl+V` con la cantidad en su valor por defecto (6)
- **THEN** la pestaña "Extraídas" queda activa con la paleta extraída y el aviso confirma la cantidad, sin que el usuario haya pulsado "Extraer colores"

#### Scenario: La cantidad ajustada se respeta
- **WHEN** el usuario ajusta la cantidad a 9 y carga otra imagen por drag&drop
- **THEN** la extracción automática produce 9 colores

#### Scenario: Determinismo
- **WHEN** el usuario ejecuta "Extraer colores" dos veces sobre la misma imagen con 6 colores
- **THEN** ambos resultados son idénticos

#### Scenario: Imagen grande
- **WHEN** la imagen es de 6000×4000 píxeles
- **THEN** la UI sigue respondiendo durante la extracción y el resultado aparece en menos de 1 segundo en hardware típico
