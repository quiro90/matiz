## MODIFIED Requirements

### Requirement: Presentación y acciones de tarjetas
En todas las pestañas de paletas generadas (Escala, Armonías, Tints/Shades, Neutros, Extraídas), las paletas SHALL mostrarse como tarjetas con la muestra, el HEX y el RGB (la etiqueta del paso en escalas; el color base con su indicador). Cada tarjeta SHALL mostrar siempre visibles, abajo a la izquierda, los botones "Copiar HEX" y "Agregar a paleta" (sin pasar por hover), y SHALL presentar, abajo a la derecha, un botón "principal" visible únicamente al pasar el mouse sobre la tarjeta (icono de ojo con tooltip "Usar como color principal"): convertir ese color en el color actual solo SHALL ocurrir al activar ese botón o la opción equivalente del menú contextual. El click sobre el cuerpo de la tarjeta SHALL copiar el color en el formato principal del usuario —lo que lo registra en el historial según la política de `color-history`— y SHALL no cambiar el color actual ni regenerar las paletas. Cada tarjeta SHALL además permitir copiar HEX, copiar RGB y "Agregar a paleta activa". El panel SHALL permitir "Agregar todo a la paleta activa" y "Copiar escala" en los formatos de `palette-export`. Las paletas generadas SHALL recalcularse en vivo con el color actual. El historial (mini-swatches de Recientes) conserva: click = usar como color actual.

#### Scenario: Agregar escala completa
- **WHEN** el usuario pulsa "Agregar todo" sobre la Design Scale de `#5246BC` con prefijo de nombre "Primary"
- **THEN** la paleta activa recibe 11 colores nombrados `Primary 50` … `Primary 950`

#### Scenario: Usar un color generado
- **WHEN** el usuario pasa el mouse sobre la tarjeta del paso 300, hace click en el botón "usar como color principal" (icono de ojo) que aparece abajo a la derecha
- **THEN** ese color pasa a ser el color actual como cambio confirmado, la rueda y el brillo se reposicionan y las paletas se regeneran desde él

#### Scenario: Click sobre el cuerpo de la tarjeta
- **WHEN** el usuario hace click sobre la muestra (cuerpo) de la tarjeta del paso 300
- **THEN** el color se copia al portapapeles en el formato principal con aviso visible, queda registrado en Recientes, y el color actual, la rueda y las paletas generadas no cambian

#### Scenario: Visibilidad de los iconos de acción
- **WHEN** el mouse no está sobre ninguna tarjeta generada
- **THEN** los botones "Copiar HEX" y "Agregar a paleta" son visibles en la esquina inferior izquierda de cada tarjeta y el botón "usar como color principal" no lo está; al pasar el mouse sobre la tarjeta del paso 300, el ojo aparece en la esquina inferior derecha y sigue visible mientras el cursor permanezca sobre esa tarjeta