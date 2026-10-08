## 1. Paleta activa

- [x] 1.1 Quitar en `src\Matiz.App\Views\MainWindow.xaml` el botón "+" (`&#xE710;`, `AddCurrentToPaletteCommand`) de la cabecera de "Paleta activa" (junto a "Copiar ▾"); verificar que el "+ Paleta" superior (línea ~340) queda intacto
- [x] 1.2 Eliminar las claves `palette.addCurrent.tooltip` y `palette.addCurrent.automation` de `src\Matiz.App\Localization\Strings.resx` y `Strings.es.resx`
- [x] 1.3 Añadir el tachito X de borrado al hover en la plantilla `PaletteColorItem`: Border envuelto en Grid con la X hermana (icono `&#xE711;`, estilo `SmallIconButton`, comando `RemovePaletteColorCommand`, tooltip `common.delete`, visibilidad por `IsMouseOver` del contenedor raíz) sin interferir drag&drop ni click de usar

## 2. Tarjetas generadas

- [x] 2.1 En la plantilla `SwatchItem`, quitar el binding de `Visibility` hover del cluster "Copiar HEX"/"Agregar a paleta" (quedan siempre visibles)
- [x] 2.2 Cambiar el icono del botón "usar como color principal" de estrella `&#xE735;` a ojo `&#xE890;` (Segoe MDL2 View) y añadirle visibilidad solo al hover (`IsMouseOver` de `CardRoot`); tooltip `swatch.useAsCurrent` sin cambios

## 3. Validación

- [x] 3.1 `dotnet build Matiz.sln` sin errores y `dotnet test` en verde
- [x] 3.2 Saneo de referencias: ninguna clave resx huérfana de este cambio y sin referencias XAML al botón eliminado (`grep` de `palette.addCurrent.`)