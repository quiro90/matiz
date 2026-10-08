## 1. Ejecutable autónomo

- [x] 1.1 Perfil `win-x64-native` (self-contained, single-file, ReadyToRun compuesto, sin símbolos); verificar que la salida es solo `Matiz.exe` (≈143 MB)
- [x] 1.2 Tarea `.\build.ps1 native` e inclusión en `all`; verificar que el script termina en OK
- [x] 1.3 Medir arranque frente a `win-x64` y documentar (≈0,7 s en caliente ambos; con compresión ≈65 MB y ≈0,9 s)
