## Purpose

Garantiza que exista un único "Color Actual" como fuente de verdad, que todas las vistas lo reflejen en tiempo real y que el usuario pueda comparar y revertir cambios.

## ADDED Requirements

### Requirement: Fuente única de verdad
El sistema SHALL mantener un único color actual. La rueda, el control de brillo, las entradas numéricas, los campos de texto, los formatos mostrados, las paletas generadas y la Design Scale SHALL derivarse de él y SHALL actualizarse cuando cambie, cualquiera sea su origen (rueda, brillo, entrada manual, captura de pantalla, imagen, historial, paleta, escala de grises, deshacer).

#### Scenario: Entrada manual sincroniza el selector
- **WHEN** el usuario introduce `#5246BC` en el campo HEX
- **THEN** el marcador de la rueda se sitúa en hue 246° y saturación 63%, el control de brillo en 74%, y todos los formatos y paletas generadas se recalculan

#### Scenario: Captura sincroniza el selector
- **WHEN** se confirma una captura de pantalla
- **THEN** el color capturado pasa a ser el color actual y la rueda y el brillo se reposicionan

### Requirement: Actualización en tiempo real sin aplicar
El sistema SHALL actualizar vista previa, formatos y paletas generadas mientras el usuario arrastra cualquier control, sin botón "Aplicar", con una latencia percibida menor a un frame (≈16 ms a 60 Hz) en hardware de oficina típico.

#### Scenario: Arrastre continuo
- **WHEN** el usuario arrastra el marcador por la rueda
- **THEN** el HEX, los demás formatos y las paletas visibles cambian de forma continua durante el arrastre

### Requirement: Preservación de coordenadas del selector
El sistema SHALL conservar el hue cuando la saturación llega a 0 o el brillo llega a 0, y la saturación cuando el brillo llega a 0, siempre que el cambio provenga del selector visual o de entradas HSV. Cuando el color proviene de una entrada RGB/HEX/captura acromática, el sistema SHALL conservar el hue anterior.

#### Scenario: Oscurecer hasta negro y volver
- **WHEN** el color es hue 246°, S 63%, B 74% y el usuario baja el brillo a 0% y lo sube de nuevo a 74%
- **THEN** el color vuelve a `#5246BC`

#### Scenario: Entrada de gris
- **WHEN** el hue actual es 246° y el usuario introduce `#808080`
- **THEN** el marcador queda en el centro de la rueda y el hue del selector sigue siendo 246°

### Requirement: Color anterior y color actual
El sistema SHALL mostrar dos muestras contiguas "Anterior" y "Actual". El color anterior SHALL ser el color actual previo al último cambio confirmado. Un cambio confirmado es: fin de un arrastre, confirmación de un campo de texto (Enter o pérdida de foco), captura, selección desde imagen/historial/paleta/grises, o deshacer/rehacer. Hacer click en la muestra "Anterior" SHALL restaurar ese color como cambio confirmado.

#### Scenario: Comparar tras arrastre
- **WHEN** el color es `#5246BC` y el usuario arrastra el marcador hasta `#675BCE` y suelta
- **THEN** "Anterior" muestra `#5246BC` y "Actual" muestra `#675BCE`

#### Scenario: Volver al anterior
- **WHEN** el usuario hace click en la muestra "Anterior"
- **THEN** el color actual pasa a ser `#5246BC` y "Anterior" pasa a ser `#675BCE`

### Requirement: Deshacer y rehacer
El sistema SHALL mantener una pila de deshacer de al menos 50 cambios confirmados del color actual. `Ctrl+Z` SHALL deshacer y `Ctrl+Y` (o `Ctrl+Shift+Z`) SHALL rehacer, cuando el foco no esté en un campo de texto. Un nuevo cambio confirmado SHALL descartar la pila de rehacer. Los pasos intermedios de un arrastre no SHALL generar entradas individuales.

#### Scenario: Deshacer un arrastre completo
- **WHEN** el usuario arrastra la rueda pasando por cien colores intermedios y suelta, y luego pulsa Ctrl+Z
- **THEN** el color actual vuelve al que había antes de empezar el arrastre

### Requirement: Entrada manual validada
El sistema SHALL ofrecer campos editables para HEX y para R, G, B (0–255), y un campo de "pegar/escribir color" que acepte cualquier formato definido en `color-model`. Los campos SHALL validar rangos, indicar visualmente los errores y no modificar el color actual mientras el valor sea inválido.

#### Scenario: Valor RGB inválido
- **WHEN** el usuario escribe `256` en el campo R
- **THEN** el campo se marca como inválido y el color actual no cambia

#### Scenario: Pegar desde el portapapeles
- **WHEN** el foco no está en un campo de texto, el portapapeles contiene `rgb(82, 70, 188)` y el usuario pulsa Ctrl+V
- **THEN** el color actual pasa a ser `#5246BC`
