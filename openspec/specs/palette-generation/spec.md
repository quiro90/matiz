# palette-generation Specification

## Purpose
Genera automáticamente, a partir del color actual, paletas y escalas útiles para diseño de interfaces y design systems, usando métodos perceptuales donde aportan.

## Requirements

### Requirement: Tipos de paleta generada
El sistema SHALL generar a partir del color actual: Complementaria (2), Análoga (5, pasos de 30°), Complementaria dividida (3, ±150°), Triádica (3, 120°), Tetrádica (4, 90°), Monocromática (5), Tints (base + 10–50% hacia blanco), Shades (base + 10–50% hacia negro), Neutros (escala de grises teñidos con el hue del color actual) y Design Scale (50–950). El color base SHALL aparecer en su posición en cada paleta.

#### Scenario: Triádica
- **WHEN** el color actual es `#5246BC` y el usuario elige "Triádica"
- **THEN** se muestran 3 colores: el base y dos con hue OKLCH rotado +120° y +240°

### Requirement: Armonías en espacio perceptual
Las armonías por rotación de hue SHALL calcularse girando el hue **del selector (rueda HSV)** exactamente el ángulo de la armonía y conservando la saturación del color base, a partir de las coordenadas continuas del color actual (no del HEX redondeado), de modo que sus posiciones en la rueda formen la figura geométrica exacta para cualquier brillo. Con la opción "Luminosidad equilibrada" activa (por defecto), el brillo de cada color SHALL ajustarse para que su luminosidad perceptual (OKLab L) coincida con la del color base; si no es alcanzable, SHALL usarse el brillo más cercano posible (100% o 0%). Con la opción desactivada, todos los colores SHALL conservar el brillo del base. Todos los colores resultantes SHALL ser sRGB válidos.

#### Scenario: Geometría exacta
- **WHEN** el color actual es hue 246.1°, S 63%, B 74% y se genera una triádica
- **THEN** los otros dos colores están en hue 6.1° y 126.1° con saturación 63%, y sus puntos en la rueda forman un triángulo equilátero con el marcador principal

#### Scenario: Estable al bajar el brillo
- **WHEN** el usuario baja el brillo del color base de 74% a 15%
- **THEN** los hue y la saturación de los colores de la armonía no cambian; solo cambia su brillo

#### Scenario: Luminosidad equilibrada
- **WHEN** "Luminosidad equilibrada" está activa y la luminosidad del base es alcanzable para un color de la armonía
- **THEN** su OKLab L difiere de la del base en menos de 0.01

#### Scenario: Sin equilibrar
- **WHEN** "Luminosidad equilibrada" está desactivada
- **THEN** todos los colores de la armonía tienen el mismo brillo (HSV V) que el base

#### Scenario: Resultado en gama
- **WHEN** se genera cualquier armonía a partir de cualquier color
- **THEN** todos los colores resultantes son sRGB válidos

### Requirement: Tints y shades convencionales
Tint N% SHALL ser la mezcla en sRGB del color base con blanco al N%, y Shade N% la mezcla con negro al N% (misma definición que Sass/Bootstrap `tint-color`/`shade-color`), para resultados predecibles y comparables con otras herramientas.

#### Scenario: Tint 50%
- **WHEN** el color base es `#5246BC`
- **THEN** Tint 50% es `#A9A3DE`

#### Scenario: Shade 50%
- **WHEN** el color base es `#5246BC`
- **THEN** Shade 50% es `#29235E`

### Requirement: Design Scale perceptual
El sistema SHALL generar una escala de 11 pasos (50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950) en OKLCH: la luminosidad L SHALL decrecer estrictamente del 50 al 950 siguiendo una curva de referencia; la croma SHALL derivarse de la del color base reduciéndose hacia los extremos claros y oscuros; el hue SHALL mantenerse; cada paso SHALL ajustarse a la gama sRGB reduciendo croma. El color base SHALL quedar exactamente en el paso ancla. El modo de anclaje SHALL ser configurable: "Base en 500" (por defecto) o "Automático" (el paso cuya L de referencia sea más cercana a la del base).

#### Scenario: Base en 500
- **WHEN** el color base es `#5246BC` y el anclaje es "Base en 500"
- **THEN** el paso 500 es exactamente `#5246BC`, 50–400 son progresivamente más claros y 600–950 progresivamente más oscuros

#### Scenario: Anclaje automático con color claro
- **WHEN** el color base es `#FACC15` y el anclaje es "Automático"
- **THEN** el base se ubica en un paso claro (300 o inferior) y los pasos restantes mantienen luminosidades estrictamente decrecientes y distinguibles (ΔL ≥ 0.02 entre pasos consecutivos)

#### Scenario: Todos en gama
- **WHEN** se genera la escala de cualquier color de entrada
- **THEN** los 11 colores son sRGB válidos y su L OKLCH es estrictamente decreciente

### Requirement: Neutros teñidos
La paleta "Neutros" SHALL producir 11 pasos con las mismas luminosidades de referencia de la Design Scale, el hue del color actual y croma muy baja (≤ 0.02), útiles como grises de interfaz. Para un color de entrada acromático SHALL producir grises puros.

#### Scenario: Neutros de un violeta
- **WHEN** el color base es `#5246BC`
- **THEN** los neutros son grises ligeramente violáceos con croma OKLCH ≤ 0.02

### Requirement: Presentación y acciones de tarjetas
En todas las pestañas de paletas generadas (Escala, Armonías, Tints/Shades, Neutros, Extraídas), las paletas SHALL mostrarse como tarjetas con la muestra, el HEX y el RGB (la etiqueta del paso en escalas; el color base con su indicador). Cada tarjeta SHALL presentar, abajo a la derecha, un botón "principal" siempre visible (icono con tooltip "Usar como color principal"): convertir ese color en el color actual solo SHALL ocurrir al activar ese botón o la opción equivalente del menú contextual. El click sobre el cuerpo de la tarjeta SHALL copiar el color en el formato principal del usuario —lo que lo registra en el historial según la política de `color-history`— y SHALL no cambiar el color actual ni regenerar las paletas. Cada tarjeta SHALL además permitir copiar HEX, copiar RGB y "Agregar a paleta activa". El panel SHALL permitir "Agregar todo a la paleta activa" y "Copiar escala" en los formatos de `palette-export`. Las paletas generadas SHALL recalcularse en vivo con el color actual. El historial (mini-swatches de Recientes) conserva: click = usar como color actual.

#### Scenario: Agregar escala completa
- **WHEN** el usuario pulsa "Agregar todo" sobre la Design Scale de `#5246BC` con prefijo de nombre "Primary"
- **THEN** la paleta activa recibe 11 colores nombrados `Primary 50` … `Primary 950`

#### Scenario: Usar un color generado
- **WHEN** el usuario activa el botón "usar como color principal" de la tarjeta del paso 300 (antes: cualquier click sobre la tarjeta)
- **THEN** ese color pasa a ser el color actual como cambio confirmado, la rueda y el brillo se reposicionan y las paletas se regeneran desde él

#### Scenario: Click sobre el cuerpo de la tarjeta
- **WHEN** el usuario hace click sobre la muestra (cuerpo) de la tarjeta del paso 300
- **THEN** el color se copia al portapapeles en el formato principal con aviso visible, queda registrado en Recientes, y el color actual, la rueda y las paletas generadas no cambian

### Requirement: Armonías visibles en la rueda
Con la pestaña Armonías activa, la rueda SHALL mostrar, además del marcador principal, un punto pequeño y discreto por cada color de la armonía (2 a 5) en su posición de hue/saturación, con líneas tenues desde el centro que hagan visible la relación geométrica; la cantidad de puntos SHALL ser exactamente la definida por el tipo de armonía y ningún ajuste personalizado SHALL agregar ni quitar puntos. Hacer click en un punto SHALL seleccionarlo (resaltando el punto y su tarjeta) y ofrecer copiar su valor en el formato principal sin cambiar el color actual. Arrastrar un punto SHALL ajustar su desfase (hue y saturación) de forma independiente, sin mover el marcador principal ni cambiar el color actual, y su tarjeta en el panel inferior SHALL actualizarse en vivo durante el arrastre. Doble click sobre un punto SHALL restablecer su posición canónica (desfase cero) sin cambiar el color actual. El marcador principal SHALL conservar prioridad: iniciar el arrastre sobre él mueve el color actual como hoy. En otras pestañas los puntos no SHALL mostrarse (sus colores comparten hue/saturación y se superpondrían).

#### Scenario: Triádica en la rueda
- **WHEN** la pestaña es Armonías con "Triádica" y el color es `#5246BC`
- **THEN** la rueda muestra el marcador principal y 2 puntos adicionales en las posiciones de los colores +120° y +240°

#### Scenario: Seleccionar un punto
- **WHEN** el usuario hace click en el punto del color +120°
- **THEN** el punto y su tarjeta quedan resaltados, se ofrece "Copiar" con su HEX y el color actual no cambia

#### Scenario: Arrastrar un punto de forma independiente
- **WHEN** el usuario arrastra el punto del color +120° hacia otra posición de la rueda
- **THEN** durante el arrastre el punto sigue al cursor, su tarjeta cambia de color en vivo, el marcador principal y los demás puntos no se mueven y el color actual no cambia

#### Scenario: Doble click reinicia el punto
- **WHEN** el punto del color +120° tiene desfase personalizado y el usuario hace doble click sobre él
- **THEN** el punto y su tarjeta vuelven a la posición canónica de la armonía (+120°, saturación del base) y el color actual no cambia

#### Scenario: Usar un punto
- **WHEN** el usuario quiere usar como principal un color de un punto de la rueda (antes: doble click sobre el punto)
- **THEN** el doble click ahora solo reinicia la posición canónica del punto sin cambiar el color actual, y la vía para hacerla principal es el botón "usar como color principal" de su tarjeta

#### Scenario: Prioridad del marcador principal
- **WHEN** el usuario inicia el arrastre sobre el marcador principal con puntos de armonía visibles
- **THEN** el arrastre mueve el color actual (rueda y brillo) como en el comportamiento sin puntos y ningún punto de la armonía se ajusta

### Requirement: Desfases personalizados de la armonía
Cada punto de la armonía SHALL admitir un desfase personalizado, expresado como incremento de hue (grados) y de saturación (fracción) respecto de su posición canónica. El color del punto SHALL recalcularse a partir de las coordenadas personalizadas con la misma regla de brillo vigente (opción "Luminosidad equilibrada") y SHALL ser sRGB válido. Al mover el color actual desde la rueda o el control de brillo, los desfases SHALL conservarse: los puntos siguen a la armonía manteniendo su desfase (armonía personalizada). Los desfases SHALL reiniciarse a la posición canónica cuando el color actual cambia por cualquier otra vía (botón "usar como color principal", historial, captura, imagen, entrada manual, deshacer/rehacer), cuando el usuario cambia el tipo de armonía y al iniciar la aplicación; los desfases no SHALL persistirse entre sesiones. La saturación personalizada SHALL limitarse al rango [0, 1] y el punto SHALL permanecer dentro del disco de la rueda.

#### Scenario: Seguimiento rígido al mover el principal
- **WHEN** el punto de +120° tiene un desfase personalizado de +10° de hue y el usuario gira el marcador principal 5°
- **THEN** el punto queda en 135° desde el nuevo hue del base (120° + 5° + 10°) con su saturación personalizada, y su tarjeta refleja ese color

#### Scenario: Reinicio al cambiar el color principal por otra vía
- **WHEN** un punto tiene desfase personalizado y el usuario activa "Usar como color principal" sobre una tarjeta
- **THEN** todos los puntos vuelven a sus posiciones canónicas respecto del nuevo color actual

#### Scenario: Saturación en gama
- **WHEN** el usuario arrastra un punto hacia el borde exterior de la rueda
- **THEN** la saturación se limita a 1, el punto no sale del disco y el color resultante es sRGB válido

#### Scenario: Sin desfase es comportamiento actual
- **WHEN** ningún punto fue arrastrado desde que la aplicación abrió
- **THEN** la armonía coincide exactamente con la definida en "Armonías en espacio perceptual" (geometría exacta, figura intacta)
