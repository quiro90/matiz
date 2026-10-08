# color-history Specification

## Purpose
Mantiene a mano los colores usados recientemente para poder volver a ellos al instante, especialmente al tomar varios colores seguidos de un diseño.

## Requirements

### Requirement: Política de inserción
El sistema SHALL añadir un color al historial cuando: se confirma una captura de pantalla, se selecciona un color de una imagen, se confirma una entrada manual (HEX/RGB/pegar) o se copia un color al portapapeles. Los movimientos de la rueda o del brillo no SHALL añadir entradas por sí solos.

#### Scenario: Arrastres no ensucian el historial
- **WHEN** el usuario arrastra la rueda varias veces sin copiar
- **THEN** el historial no cambia

#### Scenario: Copiar registra el color
- **WHEN** el usuario copia el HEX del color actual
- **THEN** ese color aparece primero en el historial

### Requirement: Orden, duplicados y límite
El historial SHALL estar ordenado del más reciente al más antiguo, SHALL contener como máximo 30 colores y SHALL mover al principio un color ya presente en lugar de duplicarlo.

#### Scenario: Duplicado
- **WHEN** el historial contiene `#5246BC` en la posición 5 y se vuelve a capturar `#5246BC`
- **THEN** `#5246BC` pasa a la posición 1 y aparece una sola vez

#### Scenario: Límite
- **WHEN** el historial tiene 30 colores y se añade uno nuevo
- **THEN** se descarta el más antiguo

### Requirement: Uso y persistencia
Hacer click en un color del historial SHALL convertirlo en el color actual. El historial SHALL persistir entre sesiones en `%APPDATA%\Matiz\history.json` y SHALL poder vaciarse con una acción explícita.

#### Scenario: Persistencia
- **WHEN** el usuario cierra y vuelve a abrir la aplicación
- **THEN** el historial muestra los mismos colores en el mismo orden
