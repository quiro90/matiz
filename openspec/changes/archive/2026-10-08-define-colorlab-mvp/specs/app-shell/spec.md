## Purpose

Define la ventana principal, su organización, temas, atajos y ajustes, para que Matiz se sienta como una utilidad pequeña, rápida y siempre disponible.

## ADDED Requirements

### Requirement: Ventana única y layout
El sistema SHALL usar una única ventana principal (tamaño inicial ≈ 1000×680 DIP, mínimo ≈ 780×560) organizada en: barra superior compacta (captura, imagen, tema, siempre visible, ajustes); zona del selector (rueda, brillo, enfoque, entradas numéricas, grises); panel del color actual (anterior/actual, formatos, copiar); fila de recientes; panel de paletas generadas con pestañas; y barra inferior con la paleta activa. La gestión de paletas guardadas y los ajustes SHALL abrirse como paneles laterales deslizables dentro de la misma ventana, no como ventanas nuevas (salvo diálogos del sistema para abrir/guardar archivos y la vista previa de exportación).

#### Scenario: Inicio
- **WHEN** el usuario abre la aplicación
- **THEN** ve inmediatamente el selector con el último color actual de la sesión anterior y sus valores, sin pantallas intermedias

### Requirement: Arranque rápido e instancia única
La ventana principal SHALL ser interactiva en menos de 1,5 s en arranque en frío en hardware típico. Solo SHALL ejecutarse una instancia: abrir la aplicación de nuevo SHALL activar la ventana existente.

#### Scenario: Segunda instancia
- **WHEN** Matiz ya está abierto y el usuario lo lanza otra vez
- **THEN** no se abre otra ventana y la existente se restaura y pasa al frente

### Requirement: Temas claro y oscuro neutros
El sistema SHALL ofrecer tema claro, oscuro y "según el sistema" (por defecto), con superficies en grises neutros (croma prácticamente nula) para no interferir con la percepción de los colores analizados. El cambio de tema SHALL aplicarse sin reiniciar.

#### Scenario: Cambiar tema
- **WHEN** el usuario cambia a tema oscuro
- **THEN** toda la UI cambia inmediatamente y la preferencia persiste

### Requirement: Siempre visible
El sistema SHALL ofrecer un conmutador "Siempre visible" que mantenga la ventana por encima de las demás, persistente entre sesiones.

#### Scenario: Activar
- **WHEN** el usuario activa "Siempre visible"
- **THEN** la ventana permanece por encima de otras aplicaciones hasta desactivarlo

### Requirement: Atajos de teclado
El sistema SHALL ofrecer estos atajos por defecto: `Alt+C` capturar (global), `Esc` cancelar captura/cerrar panel, `Ctrl+C` copiar en formato por defecto, `Ctrl+Shift+C` copiar HEX, `Ctrl+V` pegar color o imagen, `Ctrl+Z`/`Ctrl+Y` deshacer/rehacer color, `Ctrl+S` agregar color actual a la paleta activa, `Ctrl+N` nueva paleta, `Ctrl+O` abrir imagen, `Ctrl+E` exportar paleta activa como imagen. Los atajos de ventana no SHALL interceptar la edición dentro de campos de texto. El atajo global SHALL ser configurable desde ajustes en el MVP; el resto SHALL estar centralizado para hacerse configurable más adelante.

#### Scenario: Ctrl+C dentro de un campo
- **WHEN** el foco está en el campo HEX con texto seleccionado y el usuario pulsa `Ctrl+C`
- **THEN** se copia el texto seleccionado, no el color en formato por defecto

### Requirement: Ajustes persistentes
El sistema SHALL persistir en `%APPDATA%\Matiz\settings.json`: tema, siempre visible, atajo global, formato por defecto, opciones de HEX, copiar al capturar, mostrar tras capturar, enfoque de la rueda, anclaje de la Design Scale, último color actual, paleta activa y tamaño/posición de la ventana. Si la posición guardada queda fuera de los monitores actuales, la ventana SHALL abrirse en el monitor principal.

#### Scenario: Monitor desconectado
- **WHEN** la ventana se cerró en un monitor secundario que ya no está conectado
- **THEN** al abrir, la ventana aparece completamente visible en el monitor principal

### Requirement: Accesibilidad básica
Todos los controles SHALL ser accesibles por teclado (orden de tabulación lógico, foco visible) y tener nombres accesibles para lectores de pantalla; los botones solo con icono SHALL tener tooltip.

#### Scenario: Navegación por teclado
- **WHEN** el usuario recorre la ventana con `Tab`
- **THEN** cada control interactivo recibe foco visible en un orden lógico

### Requirement: Barra de título integrada
La ventana principal SHALL prescindir de la barra de título del sistema: la barra superior de la aplicación SHALL actuar como zona de arrastre e incluir solo los botones Minimizar y Cerrar. El usuario SHALL poder elegir en Ajustes si esos botones van a la derecha (por defecto) o a la izquierda. La ventana SHALL conservar redimensionado por los bordes, el acoplamiento de Windows (snap) y el maximizado con doble click en la barra.

#### Scenario: Botones a la izquierda
- **WHEN** el usuario elige "Botones de ventana: Izquierda" en Ajustes
- **THEN** Minimizar y Cerrar aparecen al inicio de la barra superior y la preferencia persiste

#### Scenario: Arrastrar la ventana
- **WHEN** el usuario arrastra una zona vacía de la barra superior
- **THEN** la ventana se mueve como con una barra de título normal
