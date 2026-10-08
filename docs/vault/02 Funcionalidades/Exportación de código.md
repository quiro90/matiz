---
tags: [funcionalidad, export]
---
# Exportación de código
Paletas, escalas y armonías se copian como:
- **CSS variables** `:root { --primary: #5246BC; }`
- **JSON** `{ "Primary": "#5246BC" }`
- **Dart/Flutter** clase con `static const Color primary = Color(0xFF5246BC);`
- **C# (WPF)** clase con `Color.FromArgb(255, 82, 70, 188)`
- **Tailwind v4** `@theme { --color-primary-500: … }`
- **Lista HEX**

Nombres → identificadores válidos (kebab / camel / Pascal, sin tildes, colisiones con sufijo). Sin nombre → `color1`…
Extender → [[Cómo añadir un formato]] · [[Formatos extensibles]]
