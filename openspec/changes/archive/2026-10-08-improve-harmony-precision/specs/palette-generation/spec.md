## MODIFIED Requirements

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
