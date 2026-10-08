# palette-export Specification

## Purpose
Lleva paletas y escalas a código y a imagen: formatos de texto extensibles listos para pegar en proyectos y una imagen PNG limpia para compartir.

## Requirements

### Requirement: Formatos de código para paletas
El sistema SHALL copiar una paleta guardada o una paleta/escala generada en al menos: CSS variables, JSON, Dart/Flutter y C#; y en una fase posterior Tailwind. Los nombres de los colores SHALL convertirse a identificadores válidos de cada lenguaje (CSS kebab-case, JSON el nombre original, Dart camelCase, C# PascalCase); los colores sin nombre SHALL nombrarse `color1`, `color2`… según su posición. Los formatos SHALL ser extensibles mediante registro, sin modificar la UI.

#### Scenario: CSS variables
- **WHEN** la paleta contiene "Primary" `#5246BC` y "Secondary" `#FF8A00` y el usuario copia como CSS
- **THEN** el portapapeles contiene:
  ```
  :root {
    --primary: #5246BC;
    --secondary: #FF8A00;
  }
  ```

#### Scenario: JSON
- **WHEN** la misma paleta se copia como JSON
- **THEN** el portapapeles contiene un objeto JSON válido `{ "Primary": "#5246BC", "Secondary": "#FF8A00" }` (con indentación)

#### Scenario: Dart/Flutter
- **WHEN** la misma paleta se copia como Dart
- **THEN** el resultado contiene `static const Color primary = Color(0xFF5246BC);` y `static const Color secondary = Color(0xFFFF8A00);` dentro de una clase con el nombre de la paleta en PascalCase

#### Scenario: C#
- **WHEN** la misma paleta se copia como C#
- **THEN** el resultado contiene `public static readonly Color Primary = Color.FromArgb(255, 82, 70, 188);` dentro de una clase estática con el nombre de la paleta en PascalCase

#### Scenario: Escala con pasos
- **WHEN** el usuario copia la Design Scale con prefijo "Primary" como CSS
- **THEN** el resultado contiene `--primary-50` … `--primary-950`

#### Scenario: Nombres duplicados
- **WHEN** dos colores producen el mismo identificador
- **THEN** el segundo recibe un sufijo numérico (`primary-2`) y el resultado es código válido

### Requirement: Exportar paleta como imagen PNG
El sistema SHALL exportar una paleta a PNG mostrando el título de la paleta, grandes bloques de color y debajo de cada uno el nombre, HEX y RGB; opcionalmente HSL y CMYK. El usuario SHALL poder elegir orientación (horizontal/vertical), tamaño (escala 1×, 2× o 3×) y fondo (claro/oscuro), con vista previa antes de guardar. El texto SHALL ser legible sobre el fondo elegido.

#### Scenario: Exportación básica
- **WHEN** el usuario exporta "PuchiApp" (3 colores) en horizontal a 2×
- **THEN** se guarda un PNG con el título "PuchiApp", 3 bloques en fila y sus nombres, HEX y RGB, y sus dimensiones son el doble de la versión 1×

#### Scenario: Paleta vacía
- **WHEN** la paleta no tiene colores
- **THEN** la acción de exportar a imagen está deshabilitada con una indicación del motivo

#### Scenario: Píxeles exactos
- **WHEN** se inspecciona el centro de un bloque de color en el PNG exportado
- **THEN** su valor RGB coincide exactamente con el color de la paleta
