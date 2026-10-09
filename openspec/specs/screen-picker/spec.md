# screen-picker Specification

## Purpose
Permite capturar el color real de cualquier píxel visible en cualquier monitor, con precisión de píxel físico, mediante un modo de selección con lupa activable desde un atajo global.

## Requirements

### Requirement: Activación del modo captura
El sistema SHALL ofrecer un botón "Capturar" destacado como acción principal de la barra superior, que permita capturar directamente en cualquier punto del escritorio (cualquier monitor, incluida la propia ventana de Matiz). El sistema SHALL entrar en modo captura al pulsar ese botón o el atajo global (por defecto `Alt+C`, configurable), incluso si la ventana de Matiz está minimizada o no tiene el foco. Si el atajo no puede registrarse (ocupado por otra aplicación), el sistema SHALL notificarlo al usuario y permitir elegir otro.

#### Scenario: Atajo global desde otra aplicación
- **WHEN** el usuario trabaja en otra aplicación y pulsa `Alt+C`
- **THEN** Matiz entra en modo captura sobre todos los monitores

#### Scenario: Atajo ocupado
- **WHEN** al iniciar, `Alt+C` ya está registrado por otra aplicación
- **THEN** Matiz muestra un aviso no bloqueante indicando el conflicto y el botón "Capturar" sigue funcionando

### Requirement: Píxel real visible
El sistema SHALL obtener el color del píxel tal como está en el framebuffer del escritorio en el momento de activar el modo captura (instantánea congelada), no reconstruido a partir de controles de la aplicación. El color devuelto SHALL ser exactamente el valor sRGB de 8 bits de ese píxel.

#### Scenario: Píxel conocido
- **WHEN** hay un rectángulo de color `#5246BC` en pantalla y el usuario confirma sobre él
- **THEN** el color actual es exactamente `#5246BC`

### Requirement: Overlay, cursor y lupa
En modo captura el sistema SHALL cubrir todos los monitores con un overlay, mostrar un cursor de cruz y una lupa junto al cursor con una cuadrícula de al menos 11×11 píxeles físicos ampliados sin suavizado, resaltando el píxel central seleccionado, y mostrar el HEX y el RGB del píxel bajo el cursor. La lupa SHALL reposicionarse para permanecer visible cerca de los bordes de un monitor.

#### Scenario: Movimiento del ratón
- **WHEN** el usuario mueve el ratón en modo captura
- **THEN** la lupa, el píxel resaltado y los valores HEX/RGB se actualizan en cada movimiento sin retraso perceptible

#### Scenario: Borde de monitor
- **WHEN** el cursor está en la esquina inferior derecha de un monitor
- **THEN** la lupa se muestra completa hacia arriba/izquierda del cursor

### Requirement: Confirmación, cancelación y teclado
Click izquierdo o `Enter` SHALL confirmar el píxel bajo el cursor como color principal (cambio confirmado y añadido al historial) y, sin `Shift`, SHALL cerrar el overlay. Click derecho SHALL capturar el píxel bajo el cursor como punto secundario del conjunto libre (ver `free-points`): el color actual no cambia, no se aplica el copiado al portapapeles de "Copiar al capturar" y, sin `Shift`, el overlay se cierra. Una acción de captura (click izquierdo, click derecho, `Enter`) con `Shift` presionado SHALL ejecutarse sin cerrar el overlay: `Shift`+click izquierdo SHALL actualizar el color principal (los secundarios del conjunto libre lo siguen rígidamente, comportamiento del conjunto) y `Shift`+click derecho SHALL añadir otro secundario, de modo que una misma sesión de captura permita confirmar varios colores; el modo continúa hasta cerrar sin `Shift` (acción de captura o click derecho) o con `Esc`. `Esc` SHALL cancelar sin cambiar el color actual. Las flechas SHALL mover el cursor 1 píxel físico y `Shift`+flechas 10 píxeles. La rueda del ratón SHALL cambiar el aumento de la lupa. El tooltip del botón "Capturar" SHALL documentar estas interacciones: click = color principal, click derecho = añadir como secundario a la paleta, `Shift` = seguir capturando varios colores, `Esc` = cancelar.

#### Scenario: Confirmar
- **WHEN** el usuario hace click izquierdo
- **THEN** el overlay se cierra, el color del píxel pasa a ser el color actual (cambio confirmado) y se añade al historial

#### Scenario: Cancelar
- **WHEN** el usuario pulsa `Esc`
- **THEN** el overlay se cierra y el color actual no cambia

#### Scenario: Ajuste por teclado
- **WHEN** el usuario pulsa la flecha derecha
- **THEN** el cursor avanza exactamente un píxel físico y la lupa se actualiza

#### Scenario: Click derecho captura un secundario
- **WHEN** el color actual es `#5246BC` (hue 246.1°, S 63%) y el usuario hace click derecho sobre un píxel `#E34A63`
- **THEN** el overlay se cierra, el color actual sigue siendo `#5246BC` sin copia al portapapeles y la pestaña pasa a "Personalizado" con un punto secundario cuyo color reproduce el píxel `#E34A63` respecto del principal, con su tarjeta y punto arrastrables

#### Scenario: Modo continuo con Shift
- **WHEN** con `Shift` presionado el usuario hace `Shift`+click izquierdo sobre un píxel `#5246BC` y después `Shift`+click derecho sobre un píxel `#E34A63`
- **THEN** tras el primer click el color principal es `#5246BC`, los secundarios previos se desplazan rígidamente y el overlay sigue abierto; tras el segundo click ese píxel queda como secundario de la pestaña "Personalizado" y el overlay sigue abierto; al hacer un click sin `Shift` (o con `Esc`) el overlay se cierra

#### Scenario: Tooltip del botón Capturar
- **WHEN** el usuario apoya el mouse sobre el botón "Capturar"
- **THEN** la info flotante menciona las vías: click = color principal, click derecho = añadir como color secundario (paleta Personalizado), `Shift` = seguir capturando varios colores, `Esc` = cancelar

### Requirement: Acciones posteriores a la captura
Tras confirmar, el sistema SHALL copiar el color al portapapeles en el formato principal configurado (por defecto HEX; el mismo que usa `Ctrl+C`, ver `color-clipboard`) si el ajuste "Copiar al capturar" está activo (por defecto activo) y SHALL restaurar y traer al frente la ventana principal si el ajuste "Mostrar Matiz tras capturar" está activo (por defecto activo).

#### Scenario: Formato principal configurado
- **WHEN** el formato principal es "Dart/Flutter" y el usuario confirma un píxel `#5246BC`
- **THEN** el portapapeles contiene `Color(0xFF5246BC)`

#### Scenario: Botón capturar
- **WHEN** el usuario pulsa el botón "Capturar" de la barra superior
- **THEN** se entra en modo captura sobre todos los monitores y puede confirmar cualquier píxel visible del escritorio

#### Scenario: Flujo rápido
- **WHEN** "Copiar al capturar" está activo y el usuario confirma un píxel `#5246BC`
- **THEN** el portapapeles contiene `#5246BC` sin pasos adicionales

### Requirement: Multi-monitor y DPI
El modo captura SHALL funcionar en configuraciones de escritorio extendido con cualquier número de monitores, resoluciones distintas, escalas DPI distintas por monitor (por ejemplo 100% y 150%), monitores en coordenadas negativas (a la izquierda o encima del principal) y orientación vertical. El píxel confirmado SHALL ser el píxel físico exacto bajo la punta del cursor en todos los casos.

#### Scenario: Monitores con DPI mixto
- **WHEN** el monitor principal está al 150% y uno secundario al 100% a su izquierda, y el usuario captura en ambos
- **THEN** en cada monitor la lupa muestra píxeles físicos nítidos y el color capturado corresponde al píxel bajo el cursor

#### Scenario: Cruzar entre monitores
- **WHEN** el usuario mueve el cursor de un monitor a otro durante la captura
- **THEN** la lupa continúa funcionando sin saltos ni desplazamientos en el nuevo monitor

### Requirement: Limitaciones documentadas
El sistema SHALL documentar (ayuda/tooltip) que en monitores HDR, aplicaciones a pantalla completa exclusiva o contenido protegido el valor capturado puede no coincidir con el original.

#### Scenario: Información disponible
- **WHEN** el usuario consulta la ayuda del modo captura
- **THEN** se describen las limitaciones de HDR, pantalla completa exclusiva y contenido protegido
