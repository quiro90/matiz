# palette-file Specification

## Purpose
Formato de archivo de paleta (`.mpalette`): permitir guardar una paleta completa en el disco e importarla después, siempre como paleta nueva, en formato JSON versionado y legible, incluida la apertura por doble click del sistema.

## Requirements

### Requirement: Formato de archivo de paleta
El sistema SHALL definir un formato de archivo único con extensión `.mpalette`: un documento JSON legible con campo `schemaVersion` y el contenido completo de la paleta: nombre, descripción (opcional), fecha de creación, fecha de última modificación y la lista ordenada de colores; cada color SHALL incluir su nombre individual (opcional), su valor hex y su alfa cuando no es opaco. La escritura al disco SHALL ser atómica (archivo temporal + reemplazo). Archivos con `schemaVersion` desconocida (mayor a la vigente) SHALL rechazarse con error y sin modificar la biblioteca.

#### Scenario: Contenido del archivo exportado
- **WHEN** se exporta la paleta "PuchiApp" con los colores `#5246BC`, `#E24347` y `#F7F9FB`
- **THEN** el archivo `.mpalette` es JSON válido con `schemaVersion`, `"name": "PuchiApp"`, descripción, fechas de creación/modificación y 3 colores en orden con sus valores hex (y el alfa de algún color no opaco, si lo hubiera, conservado)

#### Scenario: Archivo con versión futura
- **WHEN** se abre un `.mpalette` con `schemaVersion` mayor a la vigente
- **THEN** la operación falla con un aviso de error y la biblioteca queda sin cambios

### Requirement: Exportar la paleta marcada a archivo
El sistema SHALL exportar la paleta marcada (activa) mediante el botón "Exportar" del panel Biblioteca: SHALL abrir un diálogo de guardado con filtro "Paleta Matiz (*.mpalette)" y nombre de archivo sugerido derivado del nombre de la paleta, SHALL escribir el archivo con el formato `.mpalette` completo y SHALL confirmar con un toast. La acción SHALL estar deshabilitada si la paleta marcada no tiene colores. Si el usuario cancela el diálogo SHALL no escribirse nada ni modificarse la biblioteca.

#### Scenario: Exportar la biblioteca existente
- **WHEN** la paleta marcada "PuchiApp" tiene 3 colores y el usuario pulsa "Exportar", elige carpeta y confirma el nombre sugerido "PuchiApp.mpalette"
- **THEN** se crea el archivo con los datos completos de la paleta y se muestra el toast de exportación con el nombre de la paleta

#### Scenario: Exportar con color no opaco
- **WHEN** la paleta marcada incluye un color con alfa 128 y el usuario exporta
- **THEN** el archivo conserva el color con su alfa (no se opaca al 255)

#### Scenario: Sin paleta con colores
- **WHEN** la paleta marcada no tiene colores y el usuario abre el panel Biblioteca
- **THEN** el botón "Exportar" está deshabilitado

### Requirement: Importar siempre como paleta nueva
El sistema SHALL importar un archivo `.mpalette` mediante el botón "Importar" del panel Biblioteca (diálogo de abrir con filtro "Paleta Matiz (*.mpalette)"): SHALL crear **siempre una paleta nueva** —nunca modificar, reemplazar ni eliminar una paleta existente— conservando nombre, descripción, colores (con sus nombres individuales), orden y fechas del archivo, y generando identificadores nuevos. Si ya existe una paleta con ese nombre SHALL agregarse el sufijo numérico automático ("Nombre 2"). La paleta importada SHALL quedar como marcada y el sistema SHALL confirmar con un toast que incluya el nombre de la paleta importada. Si la paleta importada excede 64 colores SHALL conservarse completa (el límite solo bloquea añadir colores nuevos). Si el archivo es inválido o no parsea, SHALL advertirse con un toast de error y la biblioteca SHALL quedar sin cambios.

#### Scenario: Importar con biblioteca vacía
- **WHEN** no hay paletas y el usuario importa el archivo de "PuchiApp" (3 colores)
- **THEN** se crea la paleta "PuchiApp" con esos 3 colores en orden y fechas del archivo, queda como marcada y se muestra el toast de importación con el nombre de la paleta

#### Scenario: Importar con nombre duplicado
- **WHEN** ya existe una paleta "PuchiApp" con otros colores y el usuario importa el archivo de "PuchiApp"
- **THEN** se crea una paleta nueva "PuchiApp 2" con los colores del archivo, la "PuchiApp" existente queda exactamente como estaba, la nueva queda marcada y se muestra el toast con "PuchiApp 2"

#### Scenario: Paleta externa de más de 64 colores
- **WHEN** se importa un archivo con una paleta de 70 colores
- **THEN** la paleta nueva se crea con los 70 colores completos; añadir nuevos colores queda bloqueado con el aviso de límite hasta que quede bajo el máximo

#### Scenario: Archivo inválido
- **WHEN** el usuario importa un archivo cuyo contenido no es JSON válido
- **THEN** se muestra toast de error y no se agrega ninguna paleta

### Requirement: Apertura por doble click del sistema
El sistema SHALL declarar la asociación de archivos para `.mpalette` al instalar (manifest MSIX) de modo que el doble click del SO abra Matiz e importe la paleta como nueva. Si la app no está corriendo SHALL abrirse, importarse y quedar la paleta marcada; si ya está corriendo, la nueva instancia SHALL delegar la importación a la instancia activa (que se muestra e importa la paleta) y SHALL cerrarse sin abrir una segunda ventana.

#### Scenario: Doble click sin la app corriendo
- **WHEN** Matiz está cerrado y el usuario hace doble click en "PuchiApp.mpalette"
- **THEN** Matiz abre, se importa la paleta como nueva marcada y se muestra el toast de importación

#### Scenario: Doble click con la app corriendo
- **WHEN** Matiz ya está corriendo y el usuario hace doble click en otro `.mpalette`
- **THEN** no se abre una segunda ventana: la instancia activa importa la paleta y su ventana queda en primer plano

#### Scenario: Doble click con archivo inválido
- **WHEN** el usuario hace doble click en un `.mpalette` corrupto
- **THEN** se muestra el toast de error y no se agrega ninguna paleta