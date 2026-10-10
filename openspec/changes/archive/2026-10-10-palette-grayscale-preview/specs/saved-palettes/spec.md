## MODIFIED Requirements

### Requirement: Recargar la paleta marcada en la rueda
La barra inferior "Paleta activa" SHALL presentar un botón compacto discreto **"Recargar colores"** a la derecha, junto al botón "Escala gris" y debajo de la fila Copiar/Exportar, sin aumentar la altura de la barra; el panel Biblioteca SHALL dejar de exponer el acceso "Recargar". El botón SHALL cargar la paleta marcada (activa) en la rueda cromática en modo Personalizado. El botón SHALL deshabilitarse si no existe paleta marcada con colores. Al pulsarlo el sistema SHALL advertir antes de cargar —toast con el texto "Se cargará la paleta en la rueda cromática y se perderán las selecciones actuales." y un botón de confirmación "Recargar"— y sin confirmar SHALL no cambiar nada. Al confirmar: el primer color de la paleta SHALL pasar a ser el color actual (cambio confirmado) y el principal del conjunto; cada color restante SHALL añadirse como punto secundario en su posición hue/saturación relativa al principal con su brillo propio de modo que cada tarjeta SHALL reproducir el color exacto de la paleta; el conjunto libre previo SHALL reemplazarse; la paleta marcada SHALL evaluarse al confirmar (si entre el aviso y la confirmación se marca otra paleta, se carga esa); la operación SHALL respetar los límites (una paleta de 64 colores carga principal + 63 secundarios, dentro del límite de la rueda) y el panel Biblioteca SHALL cerrarse para mostrar la rueda. La paleta marcada con la escala de grises en un valor > 0 SHALL cargar los colores tal como se ven en ese momento: el principal y cada secundario mezclados con su gris equivalente (idénticos bit a bit a los HEX visibles de las muestras); a 0 % SHALL cargar los colores originales. La recarga SHALL conservar el porcentaje de escala de grises vigente sin alterarlo; no SHALL añadir colores a la paleta, modificarla (colores, fechas) ni reiniciar su porcentaje, de modo que el flujo de edición posterior continúe normalmente.

#### Scenario: Aviso antes de cargar
- **WHEN** hay una paleta marcada con 8 colores y el usuario pulsa "Recargar colores" en la barra Paleta activa
- **THEN** aparece el toast "Se cargará la paleta en la rueda cromática y se perderán las selecciones actuales." con botón "Recargar", y hasta confirmar no cambian la rueda, el color actual ni las selecciones

#### Scenario: Carga completa en Personalizado
- **WHEN** el usuario confirma "Recargar" sobre la paleta "PuchiApp" con colores `#5246BC`, `#E24347`, `#F7F9FB`
- **THEN** la pestaña pasa a Personalizado, el color actual pasa a ser `#5246BC` como cambio confirmado, aparecen los puntos 1 (`#E24347`) y 2 (`#F7F9FB`) en las posiciones hue/saturación de la rueda correspondientes con su brillo propio y el panel Biblioteca se cierra

#### Scenario: Conteo correcto al recargar una paleta llena
- **WHEN** la paleta marcada tiene 64 colores y el usuario confirma "Recargar colores"
- **THEN** el conjunto carga principal + 63 secundarios (65 colores en total) sin aviso de límite de rueda y la rueda queda editable

#### Scenario: Sin paleta con colores
- **WHEN** no existe ninguna paleta, o la única paleta existente no tiene colores
- **THEN** el botón "Recargar colores" está deshabilitado y no muestra el aviso

#### Scenario: Solo principal
- **WHEN** la paleta marcada tiene un único color y el usuario confirma "Recargar colores"
- **THEN** la pestaña pasa a Personalizado con solo la tarjeta base (ese color como color actual), sin puntos secundarios, y se cierra el panel Biblioteca

#### Scenario: Carga con % de grises vigente conservado
- **WHEN** la paleta marcada tiene escala de grises en 80 % y el usuario confirma "Recargar colores"
- **THEN** la rueda carga los colores tal como se ven (mezcla del 80 %, idéntica a los HEX visibles), el slider de la paleta permanece en 80 % con sus muestras sin cambios y el porcentaje no se reinicia; a 0 % la carga usa los colores originales