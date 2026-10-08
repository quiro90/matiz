## Purpose

Hace que copiar valores de color sea inmediato: cada formato se copia con un click o atajo, existen fragmentos de código listos para pegar y el usuario define su formato por defecto.

## ADDED Requirements

### Requirement: Panel de formatos del color actual
El sistema SHALL mostrar para el color actual una vista previa grande y los valores HEX, RGB, HSL, HSV y CMYK (aproximado), cada uno con una acción de copiar. Un panel "Más formatos" SHALL ofrecer OKLCH y, si alpha ≠ 255, ARGB.

#### Scenario: Copiar RGB
- **WHEN** el color actual es `#5246BC` y el usuario pulsa copiar junto a RGB
- **THEN** el portapapeles contiene `82, 70, 188` y se muestra una confirmación breve no intrusiva

### Requirement: Copiar como código
El sistema SHALL permitir copiar el color actual como fragmento de código en al menos estos formatos:
- HEX: `#5246BC`
- CSS rgb: `rgb(82, 70, 188)`
- CSS hsl: `hsl(246, 47%, 51%)`
- CSS oklch: `oklch(L C h)` con los redondeos de `color-model`
- C# WPF: `Color.FromRgb(82, 70, 188)`
- C# ARGB: `Color.FromArgb(255, 82, 70, 188)`
- XAML: `#FF5246BC`
- Dart/Flutter: `Color(0xFF5246BC)`
- Entero ARGB: `0xFF5246BC`
Los formatos SHALL implementarse de forma extensible: añadir uno nuevo no SHALL requerir cambios en la UI más allá de registrarlo.

#### Scenario: Copiar para Flutter
- **WHEN** el usuario elige "Copiar como → Dart/Flutter" con `#5246BC`
- **THEN** el portapapeles contiene `Color(0xFF5246BC)`

### Requirement: Copiar todo
El sistema SHALL ofrecer "Copiar todo", que copie un bloque de texto con una línea por formato principal.

#### Scenario: Bloque completo
- **WHEN** el usuario ejecuta "Copiar todo" con `#5246BC`
- **THEN** el portapapeles contiene exactamente:
  ```
  HEX: #5246BC
  RGB: 82, 70, 188
  HSL: 246°, 47%, 51%
  HSV: 246°, 63%, 74%
  CMYK: 56%, 63%, 0%, 26%
  ```

### Requirement: Formato por defecto y opciones de HEX
El sistema SHALL permitir configurar el formato por defecto (usado por `Ctrl+C`, por "Copiar al capturar" y por click en muestras), si el HEX se escribe en mayúsculas o minúsculas y si incluye `#`. Por defecto: HEX en mayúsculas con `#`.

#### Scenario: Ctrl+C con formato por defecto
- **WHEN** el formato por defecto es "CSS rgb", el foco no está en un campo de texto y el usuario pulsa `Ctrl+C`
- **THEN** el portapapeles contiene `rgb(82, 70, 188)`

#### Scenario: Ctrl+Shift+C
- **WHEN** el usuario pulsa `Ctrl+Shift+C`
- **THEN** el portapapeles contiene el HEX del color actual independientemente del formato por defecto

### Requirement: Robustez del portapapeles
Si el portapapeles está bloqueado temporalmente por otra aplicación, el sistema SHALL reintentar brevemente y, si no lo consigue, SHALL informar del fallo sin cerrarse ni bloquear la UI.

#### Scenario: Portapapeles ocupado
- **WHEN** otra aplicación mantiene abierto el portapapeles durante la copia
- **THEN** Matiz reintenta y, si falla, muestra "No se pudo copiar" sin excepción no controlada
