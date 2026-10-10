# palette-export Specification

## Purpose
Lleva paletas y escalas a código y a imagen: formatos de texto extensibles listos para pegar en proyectos y una imagen PNG limpia para compartir.

## Requirements

### Requirement: Formatos de código para paletas
El sistema SHALL copiar una paleta guardada o una paleta/escala generada en al menos: CSS variables, JSON, Dart/Flutter y C#; y en una fase posterior Tailwind. Los nombres de los colores SHALL convertirse a identificadores válidos de cada lenguaje (CSS kebab-case, JSON el nombre original, Dart camelCase, C# PascalCase); los colores sin nombre SHALL nombrarse `color1`, `color2`… según su posición. Los formatos SHALL ser extensibles mediante registro, sin modificar la UI.

#### Scenario: CSS variables
- **WHEN** la paleta contiene "Primary" `#5246BC` y "Secondary" `#FF8A00` y el usuario copia como CSS
- **THEN** el portapapeles contiene:
  ```
  :root {
    --primary: #5246BC;
    --secondary: #FF8A00;
  }
  ```

#### Scenario: JSON
- **WHEN** la misma paleta se copia como JSON
- **THEN** el portapapeles contiene un objeto JSON válido `{ "Primary": "#5246BC", "Secondary": "#FF8A00" }` (con indentación)

#### Scenario: Dart/Flutter
- **WHEN** la misma paleta se copia como Dart
- **THEN** el resultado contiene `static const Color primary = Color(0xFF5246BC);` y `static const Color secondary = Color(0xFFFF8A00);` dentro de una clase con el nombre de la paleta en PascalCase

#### Scenario: C#
- **WHEN** la misma paleta se copia como C#
- **THEN** el resultado contiene `public static readonly Color Primary = Color.FromArgb(255, 82, 70, 188);` dentro de una clase estática con el nombre de la paleta en PascalCase

#### Scenario: Escala con pasos
- **WHEN** el usuario copia la Design Scale con prefijo "Primary" como CSS
- **THEN** el resultado contiene `--primary-50` … `--primary-950`

#### Scenario: Nombres duplicados
- **WHEN** dos colores producen el mismo identificador
- **THEN** el segundo recibe un sufijo numérico (`primary-2`) y el resultado es código válido

### Requirement: Exportar paleta como imagen PNG
El sistema SHALL exportar una paleta a PNG mostrando el título de la paleta, grandes bloques de color y debajo de cada uno el nombre, HEX y RGB; opcionalmente HSL y CMYK. El usuario SHALL poder elegir orientación (horizontal/vertical), tamaño (escala 1×, 2× o 3×) y fondo (claro, oscuro o transparente), con vista previa antes de guardar. El texto SHALL ser legible sobre el fondo elegido. En el fondo transparente el PNG SHALL conservar canal alfa y el texto SHALL usar el estilo del modo claro.

#### Scenario: Exportación básica
- **WHEN** el usuario exporta "PuchiApp" (3 colores) en horizontal a 2×
- **THEN** se guarda un PNG con el título "PuchiApp", 3 bloques en fila y sus nombres, HEX y RGB, y sus dimensiones son el doble de la versión 1×

#### Scenario: Paleta vacía
- **WHEN** la paleta no tiene colores
- **THEN** la acción de exportar a imagen está deshabilitada con una indicación del motivo

#### Scenario: Píxeles exactos
- **WHEN** se inspecciona el centro de un bloque de color en el PNG exportado
- **THEN** su valor RGB coincide exactamente con el color de la paleta

#### Scenario: Fondo transparente
- **WHEN** el usuario elige fondo "Transparente" y guarda el PNG
- **THEN** el PNG no tiene rectángulo de fondo (sus zonas de fondo tienen alfa 0) y título, valores y bordes conservan el estilo del modo claro

### Requirement: Entrada de exportación por desplegable
El botón de exportación de la paleta activa (icono de imagen + texto "Exportar") SHALL abrir un desplegable con exactamente dos opciones: **"Paleta"** (mismo logo) y **"Superpuestos"**. "Paleta" SHALL abrir la ventana clásica de exportación a PNG; "Superpuestos" SHALL abrir la ventana de exportación de superpuestos de la paleta. El atajo `Ctrl+E` SHALL seguir abriendo la exportación "Paleta".

#### Scenario: Elegir Paleta
- **WHEN** el usuario abre el desplegable y elige "Paleta"
- **THEN** se abre la ventana clásica de exportación de la paleta activa, ahora con la opción de fondo transparente

#### Scenario: Elegir Superpuestos
- **WHEN** el usuario abre el desplegable y elige "Superpuestos"
- **THEN** se abre la ventana de exportación de superpuestos con los colores de la paleta activa

### Requirement: Exportar paleta como superpuestos
La ventana de superpuestos SHALL mostrar el título de la paleta y los colores **apilados en capas superpuestas**: cada capa SHALL dibujarse opaca (sin combinar colores) una sobre otra con tamaño decreciente, de modo que de cada capa quede visible un borde o anillo. El usuario SHALL elegir:
- la **forma** de las capas: cuadrado, círculo o triángulo;
- el **orden** de los colores: primero→último (el primer color queda como capa mayor) o último→primero (el último color queda como capa mayor);
- el **tamaño** (escala 1×, 2× o 3×) y el **fondo** (claro, oscuro o transparente).

El incremento del tamaño SHALL calcularse según la cantidad de colores para que todas las capas queden con borde visible: con 64 colores los anillos son angostos pero visibles y con menos colores el anillo visible es mayor. Además de la pila, la imagen SHALL listar los valores de cada color (nombre, HEX y RGB, y opcionalmente HSL/CMYK), como en la exportación clásica. Con menos de dos colores SHALL verse una única capa sin anillos. La acción SHALL estar deshabilitada cuando la paleta no tenga colores.

#### Scenario: Superposición incremental
- **WHEN** el usuario exporta a superpuestos una paleta con 8 colores en forma cuadrado, orden primero→último
- **THEN** la imagen muestra 8 capas cuadradas apiladas desde la mayor (primer color) hasta la menor (último color), cada una con un anillo visible de la capa que está debajo y sin mezclar colores

#### Scenario: Invertir el orden
- **WHEN** el mismo usuario cambia el orden a último→primero
- **THEN** la capa mayor es el último color y las capas se apilan hasta el primero (que queda al centro de la pila)

#### Scenario: Un solo color
- **WHEN** la paleta tiene un solo color y se exporta a superpuestos
- **THEN** la imagen muestra una única capa de su color, sin anillos

#### Scenario: Muchos colores
- **WHEN** la paleta tiene 64 colores y se exporta a superpuestos
- **THEN** las 64 capas son distinguibles entre sí: cada anillo visible conserva su ancho de forma decreciente

#### Scenario: Formas y valores
- **WHEN** el usuario cambia la forma a círculo o triángulo
- **THEN** la imagen alterna la forma de las capas y conserva el listado de valores (nombre, HEX y RGB) junto al apilado

#### Scenario: Fondo transparente
- **WHEN** el usuario elige fondo "Transparente" y guarda el PNG de superpuestos
- **THEN** el PNG no tiene rectángulo de fondo (sus zonas de fondo tienen alfa 0) y los valores y bordes conservan el estilo del modo claro

### Requirement: Exportación refleja el modo de vista en escala de grises
Cuando el modo de vista en escala de grises de la paleta activa está en 0 %, toda exportación SHALL producir exactamente los mismos resultados actuales. Cuando el modo está en un valor > 0 %, toda exportación de la paleta activa SHALL ser fiel a la vista (WYSIWYG): los formatos de código (Copiar como: CSS variables, JSON, Dart/Flutter, C# y demás formatos del registro) SHALL entregar los colores mezclados con su gris equivalente según el valor actual del modo; la exportación a imagen PNG y a overlay SHALL renderizar los bloques con los colores mezclados y SHALL mostrar los valores de texto (HEX y RGB) del color resultante. Los nombres de colores SHALL conservarse sin cambios en todos los casos.

#### Scenario: Copiar como CSS con el modo activo
- **WHEN** la paleta contiene "Primary" `#5246BC`, "Secondary" `#FF8A00`, la escala está en 100 y el usuario copia como CSS
- **THEN** el portapapeles contiene los identificadores sin cambios (`--primary`, `--secondary`) pero con los HEX de los grises equivalentes de cada color

#### Scenario: Exportar a imagen con el modo activo
- **WHEN** la escala está en un valor intermedio y el usuario exporta la paleta a PNG
- **THEN** los bloques de la imagen se renderizan con los colores mezclados correspondientes a ese valor y bajo cada bloque se muestran el HEX y RGB de ese color mezclado, con coincidencia exacta de píxeles con el color mostrado en la paleta en pantalla

#### Scenario: Copiar formatos a 0 %
- **WHEN** la escala está en 0 y el usuario copia la paleta en cualquier formato
- **THEN** el resultado es idéntico carácter por carácter al resultante antes de la existencia de este modo
