-- Ejecutar en Data/opiniones_warehouse.db con DB Browser for SQLite
-- o desde la propia aplicacion (se muestran los conteos en sus logs).
SELECT 'DimProducto' AS Tabla, COUNT(*) AS Filas FROM DimProducto
UNION ALL SELECT 'DimCliente', COUNT(*) FROM DimCliente
UNION ALL SELECT 'DimFecha', COUNT(*) FROM DimFecha
UNION ALL SELECT 'DimFuente', COUNT(*) FROM DimFuente
UNION ALL SELECT 'DimSentimiento', COUNT(*) FROM DimSentimiento;

SELECT * FROM DimProducto ORDER BY ProductoId;
SELECT * FROM DimCliente ORDER BY ClienteId;
SELECT * FROM DimFecha ORDER BY FechaKey;
SELECT * FROM DimFuente ORDER BY Codigo;
SELECT * FROM DimSentimiento ORDER BY Codigo;
