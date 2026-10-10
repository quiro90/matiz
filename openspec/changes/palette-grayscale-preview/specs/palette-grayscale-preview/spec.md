## Purpose

Modo de vista temporal ("en caliente") de la paleta activa: una escala continua 0–100 % que mezcla cada color con su gris equivalente perceptual para previsualizar y entregar la paleta en escala de grises, sin alterar en ningún momento los colores guardados.

## ADDED Requirements

### Requirement: Modo de vista en escala de grises
La paleta activa SHALL ofrecer un modo de vista temporal continuo de 0 a 100 % ("Escala de grises"). A 0 % las muestras SHALL verse exactamente igual que con el modo desactivado. A 100 %, cada muestra SHALL mostrar individualmente su gris equivalente perceptual con la misma luminosidad percibida que su color original. Los valores intermedios SHALL mezclar proporcionalmente cada color con su gris equivalente (0 % = color original, 100 % = gris equivalente). El modo SHALL afectar solo la presentación: los colores almacenados en la paleta SHALL permanecer sin alterar y ninguna acción del modo SHALL modificar el archivo de paletas; al desactivar (0 %) la paleta SHALL volver exactamente a su estado previo. El modo SHALL iniciar en 0 % en cada sesión y SHALL no persistirse entre sesiones.

#### Scenario: Vista a 100 %
- **WHEN** la paleta activa contiene `#5246BC` y `#FF8A00` y el usuario arrastra la escala de grises a 100
- **THEN** cada muestra muestra su gris equivalente (misma luminosidad perceptual que el color original), el texto HEX sobre cada muestra corresponde al gris mostrado y los colores guardados de la paleta no cambian

#### Scenario: Valor intermedio
- **WHEN** la escala se deja en 60
- **THEN** cada muestra muestra una mezcla proporcional entre su color original y su gris equivalente (interpolación 60 %) y su HEX mostrado corresponde a esa mezcla

#### Scenario: Vuelta al estado previo
- **WHEN** el usuario deja la escala en 100 y luego la devuelve a 0 (o la arrastre de ida y vuelta)
- **THEN** las muestras vuelven a verse idénticas a como estaban antes de activar el modo y `palettes.json` no registró ningún cambio de color ni de fecha de modificación

#### Scenario: No persistente
- **WHEN** el usuario deja la escala en 80, cierra la aplicación y la vuelve a abrir
- **THEN** el modo inicia en 0 % y las muestras se muestran con sus colores originales

#### Scenario: Acciones por color intactas
- **WHEN** con la escala en 100 el usuario usa el menú contextual de una muestra (usar como color actual o copiar su HEX)
- **THEN** la acción opera sobre el color original almacenado de esa muestra (no sobre la vista en gris)

### Requirement: Acceso al modo sin agrandar la barra
La barra inferior "Paleta activa" SHALL conservar su altura actual: los botones Copiar y Exportar SHALL situarse en la parte superior de su columna y el acceso al modo SHALL ser un botón compacto ("Escala gris") debajo de ellos que, al pulsarlo, SHALL abrir un panel flotante (superpuesto, sin aumentar la altura de la barra) que contiene la escala deslizante 0–100 %, su valor porcentual y una forma de restablecer (0 %). El panel SHALL cerrarse al hacer click fuera de él y el botón SHALL reflejar visualmente cuando el modo está activo (valor > 0). La escala SHALL poder arrastrarse de forma continua.

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