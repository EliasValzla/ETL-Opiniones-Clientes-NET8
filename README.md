# Sistema de Análisis de Opiniones de Clientes

## Descripción

Este proyecto consiste en el desarrollo de un proceso de extracción de datos (ETL) utilizando C# y .NET 8.

El sistema obtiene información de tres fuentes diferentes: archivos CSV con encuestas de satisfacción, una base de datos SQLite con reseñas de clientes y una API REST con comentarios.

## Tecnologías utilizadas

- C# y .NET 8
- Worker Service
- SQLite
- API REST
- Visual Studio 2022

## Funcionamiento

El Worker Service se encarga de consultar las diferentes fuentes de información y almacenar los resultados en archivos temporales (staging).

También registra los eventos y errores del proceso para facilitar su seguimiento.

## Resultados

Durante la prueba se extrajeron correctamente 11 registros:

- 5 encuestas CSV.
- 3 reseñas de la base de datos.
- 3 comentarios de la API REST.

El proceso finalizó en 591 milisegundos.

## Alcance

Esta primera etapa se concentra en el diseño de la arquitectura y la implementación de la extracción de datos, como base para las siguientes fases del proceso ETL.
