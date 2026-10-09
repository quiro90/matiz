## ADDED Requirements

### Requirement: Máximo de colores por paleta
Toda paleta guardada SHALL tener como máximo 64 colores. Regla dura aplicada en todos los flujos que añaden colores a una paleta —agregar el color actual, agregar colores generados (individual o "Añadir todo"), recargar la paleta en la rueda, cambiar de paleta o seleccionar colores—: nunca SHALL superarse el límite y ninguna paleta existente SHALL recortarse. Si al intentar añadir la paleta ya alcancó el límite, o al añadir en lote la cantidad final excedería el límite, el sistema SHALL bloquear la operación completa, SHALL no añadir ningún color y SHALL advertir con un toast con el texto "Máximo 64 colores por paleta" (las paletas guardadas con más colores por archivos externos SHALL conservarse intactas: el límite aplica solo a añadir nuevos colores, sin bloquear lectura, edición de otros aspectos ni exportación). El conteo SHALL ser siempre correcto: al recargar, al cambiar la paleta marcada o al seleccionar colores, el sistema SHALL evaluar el límite sobre el número real de colores de la paleta.

#### Scenario: Añadir al alcanzar el límite
- **WHEN** la paleta activa tiene 64 colores y el usuario pulsa "+ Paleta" (o `Ctrl+S`)
- **THEN** no se añade ningún color, la paleta sigue con 64 colores, su fecha de modificación no cambia y se muestra el toast "Máximo 64 colores por paleta"

#### Scenario: Lote que no cabe no añade nada
- **WHEN** la paleta activa tiene 60 colores y el usuario pulsa "Añadir todo" sobre 6 colores generados
- **THEN** no se añade ningún color (60 + 6 excedería 64), la paleta queda con 60 y se muestra el toast "Máximo 64 colores por paleta"

#### Scenario: Lote con hueco disponible
- **WHEN** la paleta activa tiene 3 colores y el usuario pulsa "Añadir todo" sobre 10 colores generados
- **THEN** se añaden los 10 colores al final en orden y la paleta queda con 13

#### Scenario: Paleta externa de más de 64 colores se conserva
- **WHEN** el archivo `palettes.json` contiene una paleta con 70 colores (creada fuera del límite) y el usuario la abre
- **THEN** la paleta se muestra y se exporta completa (70 colores), se puede renombrar y borrar/reordenar colores, pero añadir está bloqueado con el aviso hasta que quede bajo el límite

### Requirement: Recargar la paleta marcada en la rueda
El panel Biblioteca ("Paletas") SHALL presentar un botón "Recargar" al lado de "Nueva" que cargue la paleta marcada (activa) en la rueda cromática en modo Libre. El botón SHALL deshabilitarse si no existe paleta marcada con colores o si la rueda ya muestra esa paleta sin cambios que editar. Al pulsarlo el sistema SHALL advertir antes de cargar —toast con el texto "Se cargará la paleta en la rueda cromática y se perderán las selecciones actuales." y un botón de confirmación "Recargar"— y sin confirmar SHALL no cambiar nada. Al confirmar: el primer color de la paleta SHALL pasar a ser el color actual (cambio confirmado) y el principal del conjunto; cada color restante SHALL añadirse como punto secundario en su posición hue/saturación relativa al principal con su brillo propio de modo que cada tarjeta SHALL reproducir el color exacto de la paleta; el conjunto libre previo SHALL reemplazarse; la paleta marcada SHALL evaluarse al confirmar (si entre el aviso y la confirmación se marca otra paleta, se carga esa); la operación SHALL respetar los límites (una paleta de 64 colores carga principal + 63 secundarios, dentro del límite de la rueda) y el panel Biblioteca SHALL cerrarse para mostrar la rueda. Si la paleta tiene un solo color SHALL cargar solo el principal. No SHALL añadir colores a la paleta ni modificarla.

#### Scenario: Aviso antes de cargar
- **WHEN** hay una paleta marcada con 8 colores y el usuario pulsa "Recargar"
- **THEN** aparece el toast "Se cargará la paleta en la rueda cromática y se perderán las selecciones actuales." con botón "Recargar", y hasta confirmar no cambian la rueda, el color actual ni las selecciones

#### Scenario: Carga completa en Libre
- **WHEN** el usuario confirma "Recargar" sobre la paleta "PuchiApp" con colores `#5246BC`, `#E24347`, `#F7F9FB`
- **THEN** la pestaña pasa a Libre, el color actual pasa a ser `#5246BC` como cambio confirmado, aparecen los puntos 1 (`#E24347`) y 2 (`#F7F9FB`) en las posiciones hue/saturación de la rueda correspondientes con su brillo propio y el panel Biblioteca se cierra

#### Scenario: Conteo correcto al recargar una paleta llena
- **WHEN** la paleta marcada tiene 64 colores y el usuario confirma "Recargar"
- **THEN** el conjunto carga principal + 63 secundarios (65 colores en total) sin aviso de límite de rueda y la rueda queda editable

#### Scenario: Sin paleta con colores
- **WHEN** no existe ninguna paleta, o la única paleta existente no tiene colores, y el usuario abre el panel "Paletas"
- **THEN** el botón "Recargar" está deshabilitado y no muestra el aviso

#### Scenario: Solo principal
- **WHEN** la paleta marcada tiene un único color y el usuario confirma "Recargar"
- **THEN** la pestaña pasa a Libre con solo la tarjeta base (ese color como color actual), sin puntos secundarios, y se cierra el panel Biblioteca