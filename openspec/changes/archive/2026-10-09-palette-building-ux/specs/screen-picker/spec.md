# screen-picker Delta

## MODIFIED Requirements

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