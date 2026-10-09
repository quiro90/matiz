## Purpose

Modo Libre: paletas personalizadas de 1 a 17 colores construidas por el usuario —un color principal (el color actual) y hasta 16 puntos secundarios con posición relativa en la rueda—, complementando las armonías geométricas fijas de `palette-generation`.

## ADDED Requirements

### Requirement: Conjunto de puntos libres
La pestaña "Libre" SHALL mostrar un conjunto de colores formado por el color principal (el color actual, con la tarjeta base y su indicador) y de 0 a 16 puntos secundarios. Cada punto secundario SHALL definirse por su posición relativa respecto del principal (Δhue en grados y Δsat como fracción) y su color SHALL calcularse a partir de esas coordenadas continuas con la misma regla de brillo que las armonías —opción "Equilibrar"— y SHALL ser sRGB válido. Al iniciar la aplicación el conjunto SHALL contener solo el principal. El conjunto SHALL conservarse durante la sesión: sobrevive a cambios de pestaña y del color actual y no SHALL persistirse entre sesiones. Con la pestaña Libre activa, la rueda SHALL mostrar un punto pequeño y discreto por cada secundario en su posición hue/saturación con líneas tenues desde el centro, con la misma interacción que los puntos de armonía (click selecciona punto y tarjeta y ofrece copiar en el formato principal sin cambiar el color actual; el marcador principal conserva prioridad; en otras pestañas los puntos libres no SHALL mostrarse). Las tarjetas de los secundarios SHALL etiquetarse con su número (1, 2, …) y al agregarse a la paleta o exportarse SHALL nombrarse con el prefijo del panel y el número (p. ej. "Color 1").

#### Scenario: Libre vacío por defecto
- **WHEN** la aplicación inicia y el usuario abre la pestaña Libre
- **THEN** solo se ve el color principal con su indicador de base, sin puntos secundarios y sin botón "−"

#### Scenario: Punto con posición relativa
- **WHEN** el color actual es hue 246.1°, S 63% y existe un punto libre con Δhue +120° y Δsat 0
- **THEN** su tarjeta muestra un color con hue 6.1° y saturación 63% y su punto en la rueda forma con el marcador principal el ángulo de 120°

#### Scenario: Equilibrar aplica a los puntos libres
- **WHEN** "Equilibrar" está activa y el principal es `#5246BC`
- **THEN** la luminosidad OKLab de cada punto secundario coincide con la del principal (±0.01) y todos los colores son sRGB válidos

#### Scenario: Sobrevive a cambios de pestaña y del color
- **WHEN** el usuario vuelve a Escala, cambia el color desde el historial y regresa a Libre
- **THEN** el conjunto sigue intacto: los puntos están en las mismas posiciones relativas respecto del nuevo principal, trasladados con él

### Requirement: Añadir puntos secundarios
El sistema SHALL permitir añadir puntos secundarios desde cualquier pestaña: el click derecho sobre la rueda cromática SHALL añadir un punto en la posición del cursor (hue/saturación) y pasar automáticamente a la pestaña Libre; y el botón "+" discreto —junto a "Equilibrar", abajo a la derecha de los colores—, visible en las pestañas Armonías y Libre, SHALL añadir un punto opuesto al principal (Δhue 180°, Δsat 0) cuando el conjunto no tenga aún secundarios y, en caso contrario, SHALL añadirlo al lado del último punto del conjunto (hue a 30° del último, avanzando por pasos hasta una posición no ocupada por otro punto ni por el principal y conservando su saturación relativa) para que pulsaciones consecutivas del "+" no se solapen, y pasar automáticamente a Libre (si ya se está en Libre no cambia de pestaña). Al añadir desde Escala, Tints/Shades, Neutros o Libre el nuevo punto SHALL sumarse al conjunto libre existente. Al añadir desde Armonías —click derecho o "+"— el conjunto libre SHALL definirse a partir de la armonía actual: el base, un punto por cada color de la armonía en su posición canónica (Δhue = ángulo de la armonía, Δsat 0) y el punto nuevo; en Monocromática (posiciones coincidentes en la rueda) SHALL llevarse solo el principal. Al pasar desde Armonías SHALL reemplazarse el conjunto libre previo. La cantidad de secundarios SHALL limitarse a 16: al alcanzarlo el botón "+" SHALL verse deshabilitado y el click derecho SHALL no añadir más.

#### Scenario: Click derecho desde Escala
- **WHEN** estando en Escala con color hue 246.1° el usuario hace click derecho sobre la rueda en hue 66.1°, saturación 63%
- **THEN** se pasa automáticamente a Libre con un punto secundario en esa posición (color sRGB válido), que queda seleccionable y arrastrable

#### Scenario: Botón "+" en Libre
- **WHEN** en Libre con principal hue 246.1°, S 63% el usuario pulsa "+"
- **THEN** aparece un punto secundario opuesto (hue 66.1°, S 63%) y la pestaña sigue siendo Libre

#### Scenario: Otros "+" se colocan al lado del último
- **WHEN** en Libre ya hay un secundario en Δhue 180° y el usuario pulsa "+" dos veces más
- **THEN** se añaden puntos a ~210° y ~240° (al lado del último, sin solaparse con los anteriores ni con el principal), todos con su tarjeta y arrastrables

#### Scenario: "+" en Armonías convierte la armonía
- **WHEN** en Armonías con "Triádica" y color `#5246BC` el usuario pulsa "+"
- **THEN** Libre contiene el base `#5246BC`, dos puntos en +120° y +240° con la saturación del base y un punto nuevo al lado del último (+270°), sin solaparse con los anteriores; la tarjeta base conserva su indicador

#### Scenario: Reemplazo del conjunto previo al convertir una armonía
- **WHEN** Libre ya tiene 3 secundarios y en Armonías el usuario pulsa "+" o arrastra un punto
- **THEN** el conjunto libre queda definido por la armonía actual más el punto nuevo (o el punto arrastrado) y los secundarios previos se descartan

#### Scenario: Límite de 16 puntos
- **WHEN** el conjunto libre ya tiene 16 secundarios y el usuario pulsa "+" o hace click derecho en la rueda
- **THEN** el botón "+" está deshabilitado, el click derecho no añade nada y el conjunto queda en 17 colores (principal + 16)

### Requirement: Arrastre de puntos libres
Arrastrar un punto secundario con la pestaña Libre activa SHALL actualizar su posición relativa (Δhue/Δsat) de forma independiente, sin mover el marcador principal ni cambiar el color actual, y su tarjeta SHALL actualizarse en vivo durante el arrastre. La saturación SHALL limitarse al rango [0, 1] y el punto SHALL permanecer dentro del disco de la rueda. Mover el color principal por cualquier vía —arrastre de la rueda, control de brillo, campos numéricos, historial, captura de pantalla, imagen, entrada manual, deshacer/rehacer, click sobre el cuerpo de una tarjeta— SHALL conservar las posiciones relativas de los puntos: la figura SHALL trasladarse rígidamente al nuevo principal; en particular, usar un punto secundario como principal (click sobre su tarjeta) SHALL re-anclar la figura respecto de él conservando los desfases del resto.

#### Scenario: Arrastre independiente
- **WHEN** el usuario arrastra el punto 1 en Libre
- **THEN** durante el arrastre el punto sigue al cursor, su tarjeta cambia de color en vivo, el principal y los demás puntos no se mueven y el color actual no cambia

#### Scenario: Seguimiento rígido al mover el principal
- **WHEN** el punto 1 tiene Δhue +120° y el usuario gira el marcador principal 5°
- **THEN** el punto queda a 125° del nuevo hue del base con su saturación y su tarjeta refleja ese color

#### Scenario: Saturación en gama
- **WHEN** el usuario arrastra un punto hacia el borde exterior de la rueda
- **THEN** la saturación se limita a 1, el punto no sale del disco y el color resultante es sRGB válido

#### Scenario: Usar un punto como principal
- **WHEN** el usuario hace click sobre el cuerpo de la tarjeta del punto 1 (Δhue +120°)
- **THEN** ese color pasa a ser el color actual como cambio confirmado, el punto 1 pasa a ser el principal y los demás puntos conservan sus ángulos relativos respecto de él

### Requirement: Quitar puntos en Libre
Cada tarjeta de la pestaña Libre SHALL mostrar, al pasar el mouse, un botón "−" en su esquina inferior derecha que SHALL quitar ese color del conjunto. Quitar un secundario SHALL eliminarlo (tarjeta y punto en la rueda) sin cambiar el color actual ni la posición de los demás puntos. Quitar el color principal habiendo secundarios SHALL promover automáticamente al primer secundario (el siguiente en orden): su color SHALL pasar a ser el color actual como cambio confirmado, su tarjeta SHALL pasar a ser la base (con su indicador) y los demás puntos SHALL conservar sus posiciones absolutas en la rueda (recalculándose su posición relativa respecto del nuevo principal). Quitar puntos SHALL no ser deshacible (deshacer/rehacer cubren solo el color actual). Debe sobrevivir al menos un color: si el principal es el único color del conjunto, su tarjeta SHALL no mostrar "−".

#### Scenario: Quitar un secundario
- **WHEN** Libre tiene principal + 3 secundarios y el usuario pasa el mouse sobre la tarjeta "2" y pulsa "−"
- **THEN** el punto 2 desaparece de la rueda y de las tarjetas, el principal y los otros 2 puntos no cambian y el color actual no cambia

#### Scenario: Quitar el principal promueve al siguiente
- **WHEN** Libre tiene un principal en hue 0° y los puntos 1 (hue 30°) y 2 (hue 90°) y el usuario quita el principal
- **THEN** el color actual pasa a ser el del punto 1 como cambio confirmado (la rueda y el brillo se reposicionan), la tarjeta del punto 1 se vuelve la base y el punto 2 queda en hue 90° (posición absoluta intacta)

#### Scenario: El principal único no se puede quitar
- **WHEN** Libre tiene solo el principal, sin secundarios
- **THEN** su tarjeta no muestra el botón "−" y no existe forma de dejar el conjunto vacío