## MODIFIED Requirements

### Requirement: Armonías visibles en la rueda
Con la pestaña Armonías activa, la rueda SHALL mostrar, además del marcador principal, un punto pequeño y discreto por cada color de la armonía (2 a 5) en su posición de hue/saturación, con líneas tenues desde el centro que hagan visible la relación geométrica; la cantidad de puntos SHALL ser exactamente la definida por el tipo de armonía. La armonía SHALL permanecer canónica mientras la pestaña esté activa: no SHALL existir desfases personalizados ni ajuste que agregue, quite o reposicione puntos dentro de Armonías. Hacer click en un punto SHALL seleccionarlo (resaltando el punto y su tarjeta) y ofrecer copiar su valor en el formato principal sin cambiar el color actual. Arrastrar un punto SHALL pasar automáticamente la pestaña a Libre (modo `free-points`), donde el punto arrastrado queda en la posición de destino y los demás conservan su posición canónica; el color actual no cambia. El marcador principal SHALL conservar prioridad: iniciar el arrastre sobre él mueve el color actual como hoy y no pasa de pestaña. En otras pestañas los puntos de la armonía no SHALL mostrarse (sus colores comparten hue/saturación y se superpondrían).

#### Scenario: Triádica en la rueda
- **WHEN** la pestaña es Armonías con "Triádica" y el color es `#5246BC`
- **THEN** la rueda muestra el marcador principal y 2 puntos adicionales en las posiciones de los colores +120° y +240°

#### Scenario: Seleccionar un punto
- **WHEN** el usuario hace click en el punto del color +120°
- **THEN** el punto y su tarjeta quedan resaltados, se ofrece "Copiar" con su HEX y el color actual no cambia

#### Scenario: Arrastrar un punto de forma independiente
- **WHEN** el usuario arrastra el punto del color +120° hacia otra posición de la rueda
- **THEN** durante el arrastre el punto sigue al cursor, la pestaña pasa automáticamente a Libre (modo `free-points`), el punto arrastrado queda en la posición de destino, los demás conservan su posición canónica del triángulo, el marcador principal y el color actual no cambian

#### Scenario: Doble click reinicia el punto
- **WHEN** el usuario hace doble click sobre un punto de la armonía
- **THEN** nada se reinicia ni cambia: los desfases personalizados ya no existen en Armonías (la figura permanece canónica y el color actual no cambia); la personalización de posiciones vive en Libre

#### Scenario: Usar un punto
- **WHEN** el usuario quiere usar como principal un color de un punto de la rueda
- **THEN** la vía es el click sobre el cuerpo de su tarjeta (en Libre re-ancla la figura conservando los desfases del resto, ver `free-points`); el doble click no lo convierte en principal

#### Scenario: Prioridad del marcador principal
- **WHEN** el usuario inicia el arrastre sobre el marcador principal con puntos de armonía visibles
- **THEN** el arrastre mueve el color actual (rueda y brillo) como en el comportamiento sin puntos, ningún punto de la armonía se ajusta y la pestaña no cambia

### Requirement: Presentación y acciones de tarjetas
En todas las pestañas de paletas generadas (Escala, Tints/Shades, Neutros, Armonías, Libre, Extraídas —en ese orden; Extraídas solo visible cuando hay colores extraídos de una imagen—), las paletas SHALL mostrarse como tarjetas con la muestra, el HEX y el RGB (la etiqueta del paso en escalas; el color base con su indicador). Cada tarjeta SHALL mostrar siempre visibles, abajo a la izquierda, los botones "Copiar HEX" y "Agregar a paleta" (sin pasar por hover) y SHALL no presentar ningún control visible únicamente al pasar el mouse sobre la tarjeta, salvo el botón "−" de quitar punto definido en el modo Libre (`free-points`), que SHALL verse solo al pasar el mouse y únicamente en la pestaña Libre. El click sobre el cuerpo de la tarjeta SHALL activar ese color como color principal —el mismo cambio confirmado de la opción "Usar como color principal" del menú contextual—, lo que reposiciona rueda y brillo y regenera las paletas; en la pestaña Libre, los demás puntos SHALL conservar sus posiciones relativas respecto del nuevo principal (ver `free-points`); copiar en el formato principal del usuario SHALL seguir disponible desde el menú contextual. Cada tarjeta SHALL además permitir copiar HEX, copiar RGB y "Agregar a paleta activa". El panel SHALL permitir "Agregar todo a la paleta activa" y "Copiar escala" en los formatos de `palette-export`. Las paletas generadas SHALL recalcularse en vivo con el color actual. El historial (mini-swatches de Recientes) conserva: click = usar como color actual.

#### Scenario: Agregar escala completa
- **WHEN** el usuario pulsa "Agregar todo" sobre la Design Scale de `#5246BC` con prefijo de nombre "Primary"
- **THEN** la paleta activa recibe 11 colores nombrados `Primary 50` … `Primary 950`

#### Scenario: Usar un color generado
- **WHEN** el usuario hace click sobre la muestra (cuerpo) de la tarjeta del paso 300
- **THEN** ese color pasa a ser el color actual como cambio confirmado, la rueda y el brillo se reposicionan y las paletas se regeneran desde él; si está en Libre, los demás puntos libres conservan sus posiciones relativas respecto del nuevo principal

#### Scenario: Copiar un color generado
- **WHEN** el usuario activa "Copiar HEX" sobre la tarjeta del paso 300 (o copia desde el menú contextual)
- **THEN** el color se copia al portapapeles en el formato elegido con aviso visible, queda registrado en Recientes, y el color actual, la rueda y las paletas generadas no cambian

#### Scenario: Visibilidad de los iconos de acción
- **WHEN** el mouse se mueve sobre la tarjeta del paso 300
- **THEN** los botones "Copiar HEX" y "Agregar a paleta" permanecen visibles en la esquina inferior izquierda de cada tarjeta y no aparece ningún control dependiente del hover, salvo el "−" en las tarjetas de la pestaña Libre

#### Scenario: Orden de pestañas
- **WHEN** se muestra la fila de pestañas de paletas generadas y no hay colores extraídos
- **THEN** se ven Escala, Tints/Shades, Neutros, Armonías y Libre en ese orden; con colores extraídos, Extraídas aparece tras Libre

## REMOVED Requirements

### Requirement: Desfases personalizados de la armonía
**Reason**: La armonía ya no se "rompe": permanece siempre canónica (ver "Armonías visibles en la rueda" modificado) y las posiciones personalizadas se gestionan en el modo Libre (`free-points`), al que se pasa directamente al arrastrar un punto o al añadir uno. Desaparecen el botón "Restaurar", el reinicio por doble click y el reinicio de desfases ante cambios del color por otra vía.
**Migration**: Para personalizar la posición de un color, arrastrar su punto (o usar "+") pasa automáticamente a Libre, donde la posición queda fija, se puede borrar con "−" y respeta las posiciones relativas al mover el principal.