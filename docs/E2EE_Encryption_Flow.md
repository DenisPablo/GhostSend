# 🔐 Cifrado de Extremo a Extremo (E2EE) en GhostSend

Este documento explica de forma detallada cómo se implementará el cifrado **Zero-Knowledge** en el Frontend de GhostSend para asegurar que el servidor (Backend) jamás tenga acceso o conocimiento de los archivos originales.

## 🎯 El Problema
Si un usuario sube un archivo plano al servidor y el servidor se encarga de cifrarlo, durante ese trayecto y proceso, el servidor tiene la capacidad potencial de leer la información. Además, debe almacenar la contraseña o llave para descifrarlo en el futuro. Esto rompe la privacidad total.

## 💡 La Solución: Encriptación en el Navegador (WebCrypto API)
La encriptación se delega al **100% al navegador del usuario** que sube el archivo. El servidor de Node/.NET solo actúa como "transportista de paquetes cerrados con candado" sin saber qué llevan dentro, y **la llave nunca viaja al servidor**.

---

## 🔄 Flujo Exacto del Cifrado y la Llave

### Etapa 1: Subida del Archivo (El Emisor)

1. **Selección**: El usuario selecciona un archivo (`mivideo.mp4`) en la página web.
2. **Generación de Llave**: El Frontend usa la API nativa del navegador (`window.crypto.subtle`) para generar una clave criptográfica simétrica segura, aleatoria y única de tipo **AES-GCM (256-bit)**. 
3. **Cifrado Local**: El Frontend toma los bytes del archivo original y la llave recién generada para cifrarlos directamente en la memoria RAM del navegador.
   - *Resultado:* Un objeto Blob (paquete de datos) totalmente ininteligible.
4. **Envío al Servidor**: El Frontend sube este "Blob cifrado" usando tu API (`POST /api/v1/files/upload`). 
   - *Importante:* Al servidor **solo se envía el Blob**. La llave generada en el paso 2 se queda atrapada en el navegador.
5. **Respuesta del Servidor**: El servidor guarda el Blob en disco y responde con un Identificador Único (`File-ID`, por ejemplo: `550e8400...`).

### Etapa 2: Construcción de la URL y "El Truco" del Hash (`#`)

Ahora el Frontend tiene 2 cosas en su memoria:
1. El `File-ID` (retornado por el Backend).
2. La `Llave-AES` (retenida en el navegador).

El Frontend junta ambas cosas para crear el **Enlace para Compartir**:
`https://tudominio.com/download/550e8400...#A1b2C3d4E5f6...`

#### 🕵️‍♂️ ¿Por qué usamos el numeral (`#`)?
El símbolo `#` en una URL se conoce como **Identifier Fragment**. Existe una regla de oro, inquebrantable, en todos los navegadores de internet (Chrome, Firefox, Safari): **Todo lo que está después del `#` NUNCA se envía al servidor cuando se hace una petición HTTP**.

*   Si abres `https://tudominio.com/download/550e8400#MILLAVESECRETA`
*   El navegador solo le pide a tu backend esto: `GET /download/550e8400`.
*   El servidor de GhostSend jamás se entera de la existencia de `MILLAVESECRETA`.

### Etapa 3: Descarga y Descifrado (El Receptor)

1. **Acceso al Enlace**: El receptor abre el enlace que le pasaron por WhatsApp o correo.
2. **Petición del Archivo**: El Frontend del receptor lee el `File-ID` de la URL y hace un `GET` a la API (`api/v1/files/{File-ID}`).
3. **Recepción del Bulto**: El servidor entrega el archivo cifrado. Para el receptor esto es solo un archivo lleno de ruído ilegible.
4. **Rescate de la Llave**: El Frontend del receptor accede a la memoria del navegador leyendo el fragmento (`const key = window.location.hash.substring(1)`).
5. **Descifrado Local**: Usando la llave extraída y WebCrypto API, se descifra el Blob directamente en la RAM del navegador del receptor.
6. **Descarga**: Se gatilla la descarga automática o se muestra el contenido al usuario (ahora sí, como `mivideo.mp4`).

---

## 🛡️ Resumen de Seguridad
* **Si hackean y roban tu disco duro**: Solo encontrarán archivos cifrados ilegibles. 
* **Si hackean y roban tu base de datos SQL**: Solo encontrarán metadatos (fechas, tamaños) y File-IDs, pero ni rastro de llaves.
* **Si interceptan la conexión (Man in the Middle)**: Verán solo paquetes binarios rotos.
* **Solo el emisor y aquellos a los que les haya compartido el link exacto (con el #) pueden abrir el archivo.**
