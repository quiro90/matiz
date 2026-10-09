## MODIFIED Requirements

### Requirement: Conjunto de puntos libres
La pestaña "Libre" SHALL mostrar un conjunto de colores formado por el color principal (el color actual, con la tarjeta base y su indicador) y de 0 a 64 puntos secundarios. Cada punto secundario SHALL definirse por su posición relativa respecto del principal (Δhue en grados, Δsat como fracción y, opcionalmente, Δbrillo como fracción) y su color SHALL calcularse a partir de esas coordenadas continuas y SHALL ser sRGB válido: los puntos con brillo propio SHALL reproducir exactamente ese brillo (v = clamp(brillo del principal + Δbrillo)) y los puntos sin brillo propio SHALL usar la misma regla de brillo que las armonías —opción "Equilibrar"—, que no SHALL tener efecto sobre los puntos con brillo propio. Al iniciar la aplicación el conjunto SHALL contener solo el principal. El conjunto SHALL conservarse durante la sesión: sobrevive a cambios de pestaña y del color actual y no SHALL persistirse entre sesiones. Con la pestaña Libre activa, la rueda SHALL mostrar un punto pequeño y discreto por cada secundario en su posición hue/saturación con líneas tenues desde el centro, con la misma interacción que los puntos de armonía (click selecciona punto y tarjeta y ofrece copiar en el formato principal sin cambiar el color actual; el marcador principal conserva prioridad; en otras pestañas los puntos libres no SHALL mostrarse). Las tarjetas de los secundarios SHALL etiquetarse con su número (1, 2, …) y al agregarse a la paleta o exportarse SHALL nombrarse con el prefijo del panel y el número (p. ej. "Color 1").

#### Scenario: Libre vacío por defecto
- **WHEN** la aplicación inicia y el usuario abre la pestaña Libre
- **THEN** solo se ve el color principal con su indicador de base, sin puntos secundarios y sin botón "−"

#### Scenario: Punto con posición relativa
- **WHEN** el color actual es hue 246.1°, S 63% y existe un punto libre con Δhue +120° y Δsat 0
- **THEN** su tarjeta muestra un color con hue 6.1° y saturación 63% y su punto en la rueda forma con el marcador principal el ángulo de 120°

#### Scenario: Punto con brillo propio
- **WHEN** "Equilibrar" está activa y un punto libre define Δbrillo propio porque fue cargado desde una paleta
- **THEN** su tarjeta muestra el color exacto de la paleta (mismo HEX) y la opción "Equilibrar" no altera su brillo

#### Scenario: Equilibrar aplica a los puntos libres
- **WHEN** "Equilibrar" está activa y el principal es `#5246BC`
- **THEN** la luminosidad OKLab de cada secundario sin brillo propio coincide con la del principal (±0.01), los secundarios con brillo propio conservan su brillo exacto y todos los colores son sRGB válidos

#### Scenario: Sobrevive a cambios de pestaña y del color
- **WHEN** el usuario vuelve a Escala, cambia el color desde el historial y regresa a Libre
- **THEN** el conjunto sigue intacto: los puntos están en las mismas posiciones relativas respecto del nuevo principal, trasladados con él

### Requirement: Añadir puntos secundarios
El sistema SHALL permitir añadir puntos secundarios desde cualquier pestaña: el click derecho sobre la rueda cromática SHALL añadir un punto en la posición del cursor (hue/saturación) y pasar automáticamente a la pestaña Libre; y el botón "+" discreto —junto a "Equilibrar", abajo a la derecha de los colores—, visible en las pestañas Armonías y Libre, SHALL añadir un punto opuesto al principal (Δhue 180°, Δsat 0) cuando el conjunto no tenga aún secundarios y, en caso contrario, SHALL añadirlo al lado del último punto del conjunto (hue a 30° del último, avanzando por pasos hasta una posición no ocupada por otro punto ni por el principal y conservando su saturación relativa) para que pulsaciones consecutivas del "+" no se solapen, y pasar automáticamente a Libre (si ya se está en Libre no cambia de pestaña). Al añadir desde Escala, Tints/Shades, Neutros o Libre el nuevo punto SHALL sumarse al conjunto libre existente. Al añadir desde Armonías —click derecho o "+"— el conjunto libre SHALL definirse a partir de la armonía actual: el base, un punto por cada color de la armonía en su posición canónica (Δhue = ángulo de la armonía, Δsat 0) y el punto nuevo; en Monocromática (posiciones coincidentes en la rueda) SHALL llevarse solo el principal. Al pasar desde Armonías SHALL reemplazarse el conjunto libre previo. La cantidad de secundarios SHALL limitarse a 64: al alcanzarlo el botón "+" SHALL verse deshabilitado y el click derecho SHALL no añadir nada y SHALL advertir con un toast con el texto "Máximo 64 colores por paleta".

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
- **WHEN** el conjunto libre ya tiene 64 secundarios y el usuario pulsa "+" o hace click derecho en la rueda
- **THEN** el botón "+" está deshabilitado, el click derecho no añade nada y muestra el toast "Máximo 64 colores por paleta", y el conjunto queda en 65 colores (principal + 64)