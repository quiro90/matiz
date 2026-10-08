## Purpose

Genera automáticamente, a partir del color actual, paletas y escalas útiles para diseño de interfaces y design systems, usando métodos perceptuales donde aportan.

## ADDED Requirements

### Requirement: Tipos de paleta generada
El sistema SHALL generar a partir del color actual: Complementaria (2), Análoga (5, pasos de 30°), Complementaria dividida (3, ±150°), Triádica (3, 120°), Tetrádica (4, 90°), Monocromática (5), Tints (base + 10–50% hacia blanco), Shades (base + 10–50% hacia negro), Neutros (escala de grises teñidos con el hue del color actual) y Design Scale (50–950). El color base SHALL aparecer en su posición en cada paleta.

#### Scenario: Triádica
- **WHEN** el color actual es `#5246BC` y el usuario elige "Triádica"
- **THEN** se muestran 3 colores: el base y dos con hue OKLCH rotado +120° y +240°

### Requirement: Armonías en espacio perceptual
Las armonías por rotación de hue SHALL calcularse rotando el hue en OKLCH y conservando L y C del color base; si el resultado queda fuera de gama sRGB, SHALL reducirse solo la croma hasta entrar en gama (manteniendo L y h).

#### Scenario: Resultado en gama
- **WHEN** se genera cualquier armonía a partir de cualquier color
- **THEN** todos los colores resultantes son sRGB válidos y su OKLCH L difiere del base en menos de 0.01

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
Las paletas generadas SHALL mostrarse como tarjetas con la muestra, el HEX y el RGB (y la etiqueta del paso en escalas). Cada tarjeta SHALL permitir: click para hacerlo color actual, copiar HEX, copiar RGB y "Agregar a paleta activa". El panel SHALL permitir "Agregar todo a la paleta activa" y "Copiar escala" en los formatos de `palette-export`. Las paletas generadas SHALL recalcularse en vivo con el color actual.

#### Scenario: Agregar escala completa
- **WHEN** el usuario pulsa "Agregar todo" sobre la Design Scale de `#5246BC` con prefijo de nombre "Primary"
- **THEN** la paleta activa recibe 11 colores nombrados `Primary 50` … `Primary 950`

#### Scenario: Usar un color generado
- **WHEN** el usuario hace click sobre la tarjeta del paso 300
- **THEN** ese color pasa a ser el color actual como cambio confirmado

### Requirement: Armonías visibles en la rueda
Con la pestaña Armonías activa, la rueda SHALL mostrar, además del marcador principal, un punto pequeño y discreto por cada color de la armonía (2 a 5) en su posición de hue/saturación, con líneas tenues desde el centro que hagan visible la relación geométrica. Hacer click en un punto SHALL seleccionarlo (resaltando el punto y su tarjeta) y ofrecer copiar su valor en el formato principal sin cambiar el color actual; doble click SHALL convertirlo en el color actual. En otras pestañas los puntos no SHALL mostrarse.

#### Scenario: Triádica en la rueda
- **WHEN** la pestaña es Armonías con "Triádica" y el color es `#5246BC`
- **THEN** la rueda muestra el marcador principal y 2 puntos adicionales en las posiciones de los colores +120° y +240°

#### Scenario: Seleccionar un punto
- **WHEN** el usuario hace click en el punto del color +120°
- **THEN** el punto y su tarjeta quedan resaltados, se ofrece "Copiar" con su HEX y el color actual no cambia

#### Scenario: Usar un punto
- **WHEN** el usuario hace doble click en un punto
- **THEN** ese color pasa a ser el color actual
