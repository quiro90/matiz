## ADDED Requirements

### Requirement: Exportación refleja el modo de vista en escala de grises
Cuando el modo de vista en escala de grises de la paleta activa está en 0 %, toda exportación SHALL producir exactamente los mismos resultados actuales. Cuando el modo está en un valor > 0 %, toda exportación de la paleta activa SHALL ser fiel a la vista (WYSIWYG): los formatos de código (Copiar como: CSS variables, JSON, Dart/Flutter, C# y demás formatos del registro) SHALL entregar los colores mezclados con su gris equivalente según el valor actual del modo; la exportación a imagen PNG y a overlay SHALL renderizar los bloques con los colores mezclados y SHALL mostrar los valores de texto (HEX y RGB) del color resultante. Los nombres de colores SHALL conservarse sin cambios en todos los casos.

#### Scenario: Copiar como CSS con el modo activo
- **WHEN** la paleta contiene "Primary" `#5246BC`, "Secondary" `#FF8A00`, la escala está en 100 y el usuario copia como CSS
- **THEN** el portapapeles contiene los identificadores sin cambios (`--primary`, `--secondary`) pero con los HEX de los grises equivalentes de cada color

#### Scenario: Exportar a imagen con el modo activo
- **WHEN** la escala está en un valor intermedio y el usuario exporta la paleta a PNG
- **THEN** los bloques de la imagen se renderizan con los colores mezclados correspondientes a ese valor y bajo cada bloque se muestran el HEX y RGB de ese color mezclado, con coincidencia exacta de píxeles con el color mostrado en la paleta en pantalla

#### Scenario: Copiar formatos a 0 %
- **WHEN** la escala está en 0 y el usuario copia la paleta en cualquier formato
- **THEN** el resultado es idéntico carácter por carácter al resultante antes de la existencia de este modo