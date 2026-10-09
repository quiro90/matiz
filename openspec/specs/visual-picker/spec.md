# visual-picker Specification

## Purpose
Selector visual principal: una rueda bidimensional de hue y saturación con un control de brillo independiente y una escala de grises, para elegir colores de forma rápida e intuitiva y ajustar con precisión cuando haga falta.

## Requirements

### Requirement: Rueda hue/saturación
El sistema SHALL mostrar una rueda interactiva donde el ángulo representa el hue (0° rojo arriba, creciente en sentido horario: rojo, naranja, amarillo, verde, cian, azul, violeta, magenta) y la distancia al centro representa la saturación HSV: centro = 0% (blanco), borde = 100% (color puro). La rueda SHALL dibujarse con brillo 100%, de modo que el centro sea siempre blanco, y SHALL recalcularse dinámicamente (no es una imagen estática) cuando cambien su tamaño, el DPI o la distribución radial.

#### Scenario: Selección en el borde
- **WHEN** el usuario hace click en el borde de la rueda a 120° con brillo 100%
- **THEN** el color actual es `#00FF00`

#### Scenario: Selección en el centro
- **WHEN** el usuario hace click en el centro exacto con brillo 100%
- **THEN** el color actual es `#FFFFFF`

#### Scenario: Arrastre fuera de la rueda
- **WHEN** el usuario arrastra el marcador más allá del borde
- **THEN** la saturación se limita a 100% y el hue sigue el ángulo del cursor

### Requirement: Marcador sincronizado
El sistema SHALL mostrar un marcador en la posición (hue, saturación) del color actual, con contraste suficiente sobre cualquier color de la rueda, y SHALL moverlo cuando el color actual cambie por cualquier otra vía.

#### Scenario: Entrada externa
- **WHEN** el usuario introduce `#5246BC`
- **THEN** el marcador se ubica a 246° y al radio correspondiente a saturación 63% según la distribución activa

### Requirement: Control de brillo representativo del color
El sistema SHALL mostrar un control de brillo independiente (HSV Value) cuyo fondo sea un degradado del propio color actual: desde su versión con brillo 100% (hue y saturación actuales) hasta negro. Arrastrarlo SHALL modificar solo el brillo. El color del degradado SHALL actualizarse al cambiar hue o saturación.

#### Scenario: Degradado del color actual
- **WHEN** el color actual es `#5246BC`
- **THEN** el control muestra un degradado de violeta claro (hue 246°, S 63%, B 100%) a negro, con el indicador en 74%

#### Scenario: Oscurecer
- **WHEN** el usuario arrastra el indicador de brillo hasta 37%
- **THEN** el color actual pasa a ser `#29235E` y la rueda y el marcador no cambian de posición

### Requirement: Distribución radial de saturación (enfoque)
El sistema SHALL ofrecer un control "Enfoque" continuo entre "Pastel" y "Vivo" que cambie cómo se distribuye la saturación a lo largo del radio, sin cambiar el color actual. En la posición neutra la distribución SHALL ser lineal; hacia "Pastel" SHALL dedicar más radio a saturaciones bajas; hacia "Vivo" más radio a saturaciones altas. En todas las posiciones el centro SHALL ser 0% y el borde 100%. El ajuste SHALL persistir entre sesiones.

#### Scenario: Cambiar enfoque no altera el color
- **WHEN** el color actual es `#5246BC` y el usuario mueve el enfoque hacia "Pastel"
- **THEN** el color actual sigue siendo `#5246BC` y el marcador se desplaza hacia el exterior para representar la misma saturación

#### Scenario: Más precisión en pasteles
- **WHEN** el enfoque está en "Pastel"
- **THEN** el rango de saturación 0–30% ocupa más de la mitad del radio

### Requirement: Ajuste fino y entradas numéricas
El sistema SHALL ofrecer campos numéricos editables para Hue (0–360, un decimal), Saturación (0–100%) y Brillo (0–100%), que se actualicen en vivo y acepten edición directa, rueda del ratón y flechas (±1; Shift ±10). Arrastrar la rueda con Shift pulsado SHALL reducir la velocidad del marcador para ajuste fino. El tamaño de la rueda SHALL escalar con el espacio disponible de la ventana.

#### Scenario: Edición numérica
- **WHEN** el usuario escribe `200` en Hue
- **THEN** el marcador gira a 200° y el color actual se actualiza manteniendo saturación y brillo

#### Scenario: Valor fuera de rango
- **WHEN** el usuario escribe `120` en Saturación
- **THEN** el campo se marca como inválido y el color no cambia

### Requirement: Selección de grises
El sistema SHALL permitir seleccionar grises desde el centro de la rueda cromática (saturación 0, manteniendo la luminosidad actual) y SHALL ofrecer una acción "Gris equivalente" que convierta el color actual al gris de igual luminosidad perceptual (OKLab L). El estado del color SHALL conservar el hue y la saturación de origen cuando el color actual pasa a gris o negro, de modo que bajar el brillo a cero y volver recupera el color.

#### Scenario: Gris equivalente
- **WHEN** el color actual es `#5246BC` y el usuario ejecuta "Gris equivalente"
- **THEN** el color actual es un gris neutro cuya luminosidad OKLab coincide con la de `#5246BC` (±0.005)

### Requirement: Rendimiento de la rueda
La interacción con la rueda y el brillo SHALL ser fluida: el mapa de la rueda SHALL generarse como bitmap en caché y reutilizarse mientras no cambien tamaño, DPI o distribución; mover el marcador o el brillo no SHALL regenerarlo.

#### Scenario: Arrastre sin regeneración
- **WHEN** el usuario arrastra el marcador o el brillo de forma continua
- **THEN** el bitmap de la rueda no se regenera y la interfaz se mantiene a la tasa de refresco del monitor
