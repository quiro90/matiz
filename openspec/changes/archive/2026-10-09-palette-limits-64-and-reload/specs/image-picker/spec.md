## MODIFIED Requirements

### Requirement: Extraer colores principales
El sistema SHALL ofrecer "Extraer colores" que detecte entre 1 y 64 colores predominantes (por defecto 6) y los presente como paleta generada con las mismas acciones que el resto de tarjetas. El campo de cantidad SHALL etiquetarse "Cantidad de colores:" a la izquierda del control numérico y SHALL aceptar valores entre 1 y 64 (valores fuera de rango SHALL rechazarse como entrada inválida). Al cargar una imagen (diálogo "Abrir imagen", arrastrar y soltar, o portapapeles), el sistema SHALL ejecutar la extracción una vez de forma automática con la cantidad vigente, sin bloquear la UI, y la extracción manual SHALL seguir disponible para re-ejecutarla (por ejemplo tras cambiar la cantidad). El resultado SHALL ser determinista para una misma imagen y número de colores, y SHALL completarse sin bloquear la UI.

#### Scenario: Extracción automática al cargar
- **WHEN** el usuario pega una captura de pantalla con `Ctrl+V` con la cantidad en su valor por defecto (6)
- **THEN** la pestaña "Extraídas" queda activa con la paleta extraída y el aviso confirma la cantidad, sin que el usuario haya pulsado "Extraer colores"

#### Scenario: La cantidad ajustada se respeta
- **WHEN** el usuario ajusta la cantidad a 9 y carga otra imagen por drag&drop
- **THEN** la extracción automática produce 9 colores

#### Scenario: Cantidad mínima de 1
- **WHEN** el usuario ajusta la cantidad a 1 y extra de una imagen
- **THEN** la extracción produce un único color, el predominante de la imagen

#### Scenario: Cantidad máxima de 64
- **WHEN** el usuario intenta introducir una cantidad mayor que 64 (por ejemplo 100) en el campo numérico
- **THEN** la entrada se rechaza como inválida, el valor vigente no cambia y no se ejecuta ninguna extracción con una cantidad fuera de rango

#### Scenario: Determinismo
- **WHEN** el usuario ejecuta "Extraer colores" dos veces sobre la misma imagen con 6 colores
- **THEN** ambos resultados son idénticos

#### Scenario: Imagen grande
- **WHEN** la imagen es de 6000×4000 píxeles
- **THEN** la UI sigue respondiendo durante la extracción y el resultado aparece en menos de 1 segundo en hardware típico