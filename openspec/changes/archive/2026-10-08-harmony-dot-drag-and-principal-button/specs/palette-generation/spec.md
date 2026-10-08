## ADDED Requirements

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

## MODIFIED Requirements

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