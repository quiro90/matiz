## Purpose

Modo de vista temporal ("en caliente") de la paleta activa: una escala continua 0–100 % que mezcla cada color con su gris equivalente perceptual para previsualizar y entregar la paleta en escala de grises, sin alterar en ningún momento los colores guardados.

## ADDED Requirements

### Requirement: Modo de vista en escala de grises
La paleta activa SHALL ofrecer un modo de vista temporal continuo de 0 a 100 % ("Escala de grises"). A 0 % las muestras SHALL verse exactamente igual que con el modo desactivado. A 100 %, cada muestra SHALL mostrar individualmente su gris equivalente perceptual con la misma luminosidad percibida que su color original. Los valores intermedios SHALL mezclar proporcionalmente cada color con su gris equivalente (0 % = color original, 100 % = gris equivalente). El modo SHALL afectar solo los colores: los colores almacenados en la paleta SHALL permanecer sin alterar y ninguna acción del modo SHALL modificarlos ni las fechas de la paleta; al desactivar (0 %) la paleta SHALL verse exactamente como su estado original. El porcentaje vigente SHALL recordarse para cada paleta y SHALL restaurarse automáticamente (ver "Persistencia y portabilidad del porcentaje").

#### Scenario: Vista a 100 %
- **WHEN** la paleta activa contiene `#5246BC` y `#FF8A00` y el usuario arrastra la escala de grises a 100
- **THEN** cada muestra muestra su gris equivalente (misma luminosidad perceptual que el color original), el texto HEX sobre cada muestra corresponde al gris mostrado y los colores guardados de la paleta no cambian

#### Scenario: Valor intermedio
- **WHEN** la escala se deja en 60
- **THEN** cada muestra muestra una mezcla proporcional entre su color original y su gris equivalente (interpolación 60 %) y su HEX mostrado corresponde a esa mezcla

#### Scenario: Vuelta al estado previo
- **WHEN** el usuario deja la escala en 100 y luego la devuelve a 0 (o la arrastre de ida y vuelta)
- **THEN** las muestras vuelven a verse idénticas a como estaban antes de activar el modo y `palettes.json` no registró ningún cambio de color ni de fecha de modificación

#### Scenario: Restauración al reabrir la app
- **WHEN** la paleta quedó con 80 % guardado, el usuario cierra la aplicación y la vuelve a abrir
- **THEN** el slider arranca en 80 % y las muestras se muestran con esa mezcla, con los colores guardados intactos

#### Scenario: Acciones por color intactas
- **WHEN** con la escala en 100 el usuario usa el menú contextual de una muestra (usar como color actual o copiar su HEX)
- **THEN** la acción opera sobre el color original almacenado de esa muestra (no sobre la vista en gris)

### Requirement: Acceso al modo sin agrandar la barra
La barra inferior "Paleta activa" SHALL conservar su altura actual: los botones Copiar y Exportar SHALL situarse en la parte superior de su columna y el acceso al modo SHALL ser un botón compacto ("Escala gris") debajo de ellos que, al pulsarlo, SHALL abrir un panel flotante (superpuesto, sin aumentar la altura de la barra) que contiene la escala deslizante 0–100 %, su valor porcentual y una forma de restablecer (0 %). El panel SHALL cerrarse al hacer click fuera de él, al pulsar Esc o al perder el foco la ventana (el valor se conserva) y el botón SHALL reflejar visualmente cuando el modo está activo (valor > 0). La escala SHALL poder arrastrarse de forma continua, incluso fuera de los límites del panel.

#### Scenario: Abrir el panel y arrastrar
- **WHEN** el usuario pulsa el botón "Escala gris" y arrastra la escala
- **THEN** se abre un panel flotante con la escala 0–100 con su porcentaje y las muestras cambian en vivo durante el arrastre, sin que la altura de la barra de Paleta activa aumente

#### Scenario: Cerrar el panel
- **WHEN** el usuario hace click fuera del panel flotante
- **THEN** el panel se cierra y el valor de la escala queda como estaba, sin regresar a 0 automáticamente

### Requirement: Coherencia en caliente
Mientras el modo esté en un valor > 0, cualquier cambio de la paleta activa (agregar color, reemplazar, recargar, cambiar de paleta marcada o importar) SHALL presentar también los colores nuevos o existentes conforme al valor actual del modo, sin intervención del usuario.

#### Scenario: Agregar color con el modo activo
- **WHEN** con la escala en 100 el usuario agrega un color nuevo a la paleta activa
- **THEN** la nueva muestra aparece ya con su gris equivalente y su HEX de gris, y al devolver la escala a 0 el color agregado se muestra en su color original

### Requirement: Persistencia y portabilidad del porcentaje
Cada paleta SHALL recordar su porcentaje de escala de grises como campo opcional (`grayPercent`, null = 0 %) almacenado en `palettes.json` y conservado en el archivo `.mpalette` exportado, sin cambio de `schemaVersion`: los archivos antiguos sin el campo SHALL tratarse como 0 % y el campo nuevo SHALL ignorarse sin error por apps anteriores. Guardar el porcentaje (por arrastre o restablecimiento) SHALL no modificar colores ni fechas de la paleta. Al recargar la app, cambiar la paleta marcada o importar un `.mpalette` SHALL restaurarse el porcentaje de esa paleta (slider y muestras, sin intervención del usuario). Recargar la paleta marcada hacia la rueda (modo Personalizado) SHALL cargar los colores tal como se ven (mezclados con el % vigente) y SHALL conservar el porcentaje y el estado de la paleta sin cambios; crear una paleta nueva y duplicar SHALL dejar el porcentaje en 0 %.

#### Scenario: Cambiar de paleta marcada
- **WHEN** la paleta A está con escala 60 (recordada), el usuario marca la paleta B (0 %) y luego vuelve a marcar A
- **THEN** al marcar B el slider muestra 0 % con colores originales, y al volver a A el slider muestra 60 % con las muestras correspondientes a esa mezcla

#### Scenario: Importar `.mpalette` con porcentaje
- **WHEN** la paleta activa con escala 60 se exporta a un `.mpalette` y ese archivo se importa (en esta u otra instancia de la app)
- **THEN** la paleta importada queda marcada/activa con 60 %: el slider y las muestras reflejan el porcentaje guardado

#### Scenario: Compatibilidad con archivos antiguos
- **WHEN** se carga un `palettes.json` o `.mpalette` sin el campo `grayPercent`
- **THEN** el modo queda en 0 % y la paleta se muestra normal; a la inversa, un archivo con `grayPercent` leído por una versión anterior de la app ignora el campo sin error

#### Scenario: Recarga hacia la rueda con % vigente
- **WHEN** con la escala en 80 el usuario confirma "Recargar colores" (carga la paleta marcada en la rueda en modo Libre/Personalizado)
- **THEN** la rueda carga los colores tal como se ven en la paleta (mezcla del 80 %, bit a bit con el HEX visible de cada muestra), la paleta conserva su 80 % con las muestras mostrando la misma mezcla y el slider no varía; a 0 % la recarga carga los colores originales como siempre