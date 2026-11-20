# Sistema de Análisis de Opiniones de Clientes
## Actividad 1 - Arquitectura y extracción (E de ETL) con .NET 8

Esta entrega implementa **únicamente la E de ETL** para tres fuentes: encuestas CSV, reseñas en SQLite (BD relacional) y comentarios de una API REST. Guarda los resultados en archivos `*.jsonl` dentro de `Staging/`. El almacén analítico y dashboard aparecen en el diagrama como **fases futuras**, no implementadas.

> **Datos de ejemplo:** el CSV, los 3 registros SQLite y la API local contienen opiniones inventadas para demostrar el funcionamiento. No son datos reales de clientes.

### Requisitos
- Visual Studio 2022 actualizado con compatibilidad para .NET 8, o SDK de .NET 8.
- Acceso a NuGet al restaurar los paquetes por primera vez.

### Ejecutar en Visual Studio
1. Descomprime el ZIP y abre `Opiniones_ETL.sln` con Visual Studio.
2. Espera a que Visual Studio restaure los paquetes NuGet.
3. Clic derecho en la **solución** > `Configurar proyectos de inicio...` (o `Configure Startup Projects...`).
4. Selecciona `Varios proyectos de inicio` y marca **Inicio/Start** para `Opiniones.DemoApi` y `Opiniones.ETL.Worker`, dejando la API primero. Alternativamente ejecuta primero la API y luego el Worker con clic derecho > Depurar > Iniciar nueva instancia.
5. Para comprobar la API, abre `http://127.0.0.1:5187/api/comentarios` en el navegador. Debe devolver tres elementos JSON.
6. Ejecuta `Opiniones.ETL.Worker`. La consola mostrará los conteos y tiempos por fuente.
7. Busca los archivos `.jsonl` en `src/Opiniones.ETL.Worker/bin/Debug/net8.0/Staging` (o en el directorio de salida que utilice Visual Studio). Cada línea corresponde a una opinión extraída.

### Ejecutar con terminal
Abre dos terminales desde la carpeta principal:

**Terminal 1 (API):**
```bash
dotnet run --project src/Opiniones.DemoApi/Opiniones.DemoApi.csproj
```
**Terminal 2 (Worker):**
```bash
dotnet run --project src/Opiniones.ETL.Worker/Opiniones.ETL.Worker.csproj
```

### Resultado esperado con los ejemplos
- CSV: **5** encuestas.
- Base de datos relacional: **3** reseñas.
- API REST: **3** comentarios.
- Total esperado: **11** registros y tres archivos `.jsonl`.

Estos son conteos **previstos por los datos incluidos**, no resultados certificados de una ejecución real. Captura la consola y los archivos generados después de ejecutar para usar como evidencia empírica.

### Configuración
`src/Opiniones.ETL.Worker/appsettings.json` habilita o deshabilita fuentes, cambia rutas y configura la URL base de la API. Si la API no está iniciada, la extracción de esa fuente fallará y se registrará un error. En ese caso inicia la API o establece `Sources:Api:Enabled` en `false` para comprobar las otras dos fuentes.

Para una API real, no subas tokens al repositorio. Configura la clave con una variable de entorno (Windows PowerShell):
```powershell
$env:Sources__Api__ApiKey="REEMPLAZAR_CON_TOKEN_REAL"
```
La aplicación envía la clave en `X-Api-Key` cuando tiene un valor. Para despliegues reales usa HTTPS y un almacén de secretos.

### Estructura
- `Domain/OpinionCruda.cs`: contrato de datos común.
- `Application/IExtractor.cs`: interfaz extensible para nuevas fuentes.
- `Infrastructure/CsvExtractor.cs`: lectura CSV con CsvHelper.
- `Infrastructure/DatabaseExtractor.cs`: lectura asíncrona SQLite mediante ADO.NET.
- `Infrastructure/ApiExtractor.cs`: consumo REST usando `IHttpClientFactory`.
- `Infrastructure/JsonlStagingWriter.cs`: guarda JSON Lines, con archivo temporal y movimiento al completar.
- `Infrastructure/DatabaseDemoInitializer.cs`: crea la BD de ejemplo solo cuando `SeedDemo=true`.
- `ExtractionWorker.cs`: orquesta en paralelo, registra tiempos y eventos con `ILogger`.
- `Opiniones.DemoApi`: API REST local de demostración.
- `diagramas/`: diagramas de arquitectura y flujo en PNG y formato Draw.io editable.
- `documentacion/`: informe técnico editable y PDF.

### Límites / alcance académico
Este ejemplo no implementa transformación, clasificación de sentimientos, carga de dimensiones/hechos, ni dashboard. Los datos aún no han sido deduplicados ni enriquecidos. Al conectar fuentes reales, hay que reemplazar la BD y API de ejemplo, gestionar paginación, reintentos, permisos y conexiones de producción.

### Subir a GitHub
Desde la carpeta que contiene la solución:
```bash
git init
git add .
git commit -m "Actividad 1: arquitectura y extraccion ETL .NET 8"
git branch -M main
git remote add origin https://github.com/TU_USUARIO/ETL-Opiniones-Clientes.git
git push -u origin main
```
Primero crea el repositorio vacío en GitHub y sustituye `TU_USUARIO`. No incluyas claves ni archivos `*.db` o archivos staging; `.gitignore` los excluye.

### Validación
- Comprueba que la API devuelva 3 elementos en el navegador.
- Comprueba tres logs de fuente y un log global en la consola.
- Comprueba que los archivos staging contengan 5, 3 y 3 líneas JSON, respectivamente.
- Repite la ejecución: cada corrida crea nuevos archivos con nombres únicos.
- Establece `Sources:Api:Enabled=false` para simular ausencia de una fuente sin modificar los extractores.
