## 1. Comportamiento

- [x] 1.1 En `src\Matiz.App\ViewModels\MainViewModel.Image.cs`: separar `ExtractColorsCoreAsync()` del comando `ExtractColors` y dispararla (fire-and-forget) al final de `ShowImage()`
- [x] 1.2 En `src\Matiz.App\Views\MainWindow.xaml` (cabecera de imagen, Grid.Column=2): TextBlock "Cantidad de colores:" antes del NumericBox
- [x] 1.3 Nueva clave `image.extractCount.label` en `Strings.resx` ("Number of colors:") y `Strings.es.resx` ("Cantidad de colores:")

## 2. Validación

- [x] 2.1 `dotnet build Matiz.sln` sin errores y `dotnet test` en verde
- [x] 2.2 `openspec validate` del change en verde antes de commit/archive