## Context

Change 100% de presentación en la vista principal (`MainWindow.xaml`): no toca Core, ni ViewModels, ni persistencia. Los comandos ya existen y se reutilizan tal cual.

## Goals / Non-Goals

**Goals:**

- Quitar la duplicación del "+" en la cabecera de "Paleta activa".
- Borrado rápido de un color de la paleta activa al pasar el mouse (tachito X).
- Iconos de tarjetas: copiar/agregar siempre visibles; "usar como principal" como ojo solo al hover.

**Non-Goals:**

- No se cambian menús contextuales existentes (siguen funcionando con las mismas opciones).
- No se toca el botón "+ Paleta" de la zona superior (línea superior del color actual).
- No se toca la change activa `add-msix-store-packaging` ni ningún flujo de la rueda/armonías.

## Decisions

### D1. Botón "+" de la cabecera de Paleta activa: eliminar directamente
En `MainWindow.xaml` (~línea 572) se borra el `<Button Style="IconButton" Content="&#xE710;" Command="AddCurrentToPaletteCommand" ...>` del `StackPanel` de la cabecera. Quedan "Copiar ▾" y el botón PNG. El comando `AddCurrentToPalette` no se toca: lo usa el "+ Paleta" superior. Limpieza asociada: quitar las claves `palette.addCurrent.tooltip` y `palette.addCurrent.automation` de `Strings.resx` y `Strings.es.resx` (quedarían huérfanas).

### D2. Tachito X en muestras de Paleta activa: overlay hermana del Border
La plantilla `PaletteColorItem` hoy es `StackPanel(78px) > Border(38px, drag&drop + hex) + TextBox`. El Border maneja `PreviewMouseLeftButtonDown/Move/Up` (drag&drop) y click (usar el color). Para que el tachito no dispare el drag ni el click:
- El Border se envuelve en un `Grid` (con `x:Name="SwatchRoot"` p. ej.) y la X se añade como HERMANA del Border dentro de ese Grid (`HorizontalAlignment="Right" VerticalAlignment="Top"`), no como hija.
- El tachito reutiliza el estilo de iconos pequeños (`SmallIconButton`) con icono Segoe MDL2 `&#xE711;` (mismo `X` que cierra en la app), tamaño 16-18px, `FontSize` ~9-10.
- `Command="{Binding DataContext.RemovePaletteColorCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}" CommandParameter="{Binding}"` (el comando ya existe y ya es la acción del menú).
- Tooltip y `AutomationProperties.Name` con la clave existente `common.delete`.
- Visibilidad: trigger de estilo sobre `IsMouseOver` del contenedor raíz del item (`ElementName`), no del Border: al posarse sobre la propia X, `IsMouseOver` del Border se apaga y parpadearía. Opción elegida: trigger sobre el `Grid` raíz (`IsMouseOver` de un panel que contiene la X queda true también encima de la X).

### D3. Iconos de tarjeta: visibilidad invertida, posición intacta
En la plantilla `SwatchItem`:
- Cluster abajo-izquierda (`CopySwatchHexCommand` `&#xE8C8;`, `AddSwatchToPaletteCommand` `&#xE710;`, `SmallIconButton` 22px): se QUITA el `Visibility="{Binding IsMouseOver, ElementName=CardRoot, ...}"` → siempre visibles.
- Botón "principal" abajo-derecha (18px, Margin 0,0,2,2, ZIndex 1): se reemplaza el icono `&#xE735;` (estrella) por `&#xE890;` (Segoe MDL2 `View`, el ojo estándar de Windows) y se LE AÑADE ese mismo binding de visibilidad hover. Tooltip `swatch.useAsCurrent` ("Usar como color principal") y comando `UseSwatchCommand` sin cambios. El `Foreground` sigue viniendo del item (contraste calculado).

### D4. Sin claves resx nuevas para tarjetas
`swatch.copyHex`, `swatch.addToPalette` y `swatch.useAsCurrent` ya existen y siguen siendo los tooltips correctos. Solo se eliminan claves (D1), no se agregan.

## Risks / Trade-offs

- El tachito reduce la superficie útil de la muestra al hover (la X tapa la esquina superior derecha del hex/la muestra). Aceptable: es discreta (16-18px) y el hex está centrado.
- `RemovePaletteColorCommand` vía X no pide confirmación — coherente con el menú contextual actual; la paleta sigue siendo recuperable si el borrado entra al undo general (mismo camino que hoy vía menú).
- La X como hermana del Border requiere recalculo mínimo del layout (Grid envolvente); el TextBox de nombre debajo no se altera.

## Migration Plan

No aplica (no hay datos ni formato persistido involucrado).

## Open Questions

Ninguna — alcance validado con el usuario (elegido `propose` tras el análisis).