## 1. Armonías precisas

- [x] 1.1 Generar armonías desde `ColorState` girando el hue HSV y conservando S, con equilibrio de luminosidad por búsqueda binaria del brillo; verificar con tests de geometría, estabilidad al bajar el brillo, ΔL < 0.01 y modo sin equilibrar
- [x] 1.2 Guardar en `GeneratedColor` las coordenadas de rueda y usarlas para los puntos; verificar renderizando la rueda con brillo 100% y 15%
- [x] 1.3 Ajuste "Luminosidad equilibrada" (persistente, activo por defecto) en el panel de armonías; verificar que alterna el resultado y persiste
