Gasto Hormiga - Gestor de Finanzas Personales

Bienvenido al repositorio del proyecto Gasto Hormiga, una aplicación web diseñada para ayudar a los usuarios a identificar, clasificar y gestionar esos pequeños gastos cotidianos que impactan la economía personal. (Este proyecto aún está en proceso de análisis y mejora).

------------------------------------------------------------------------------------------------------------------------------------------------------------------------

🚀 Funcionalidad de la Web

La plataforma ofrece una serie de herramientas enfocadas en la gestión financiera:

Panel de Control (Dashboard): Visualización general de los gastos, resúmenes e indicadores financieros.

Gestión de Gastos: Permite a los usuarios registrar, visualizar y organizar sus "gastos hormiga" diarios.

Suscripciones: Módulo dedicado para controlar pagos recurrentes y determinar si representan fugas de dinero innecesarias.

Clasificador Inteligente/Híbrido: Categorización automática o asistida de los gastos para entender mejor en qué se va el dinero.

Alertas y Patrones: Identificación de patrones de consumo y alertas para mejorar los hábitos financieros.

Autenticación de Usuarios: Sistema de registro, inicio de sesión y recuperación para proteger los datos personales.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------

🏗️ Arquitectura del Sistema

El sistema está construido bajo una arquitectura Cliente-Servidor moderna, dividida en tres capas principales que garantizan la escalabilidad y el mantenimiento:

Frontend (GastoHormiga.Web): Una aplicación de página única (SPA) desarrollada con Blazor WebAssembly. Se encarga de toda la interfaz de usuario, interactuando de forma reactiva en el navegador del cliente.

Backend (GastoHormiga.API): Una API RESTful construida con ASP.NET Core que maneja la lógica de negocio, el procesamiento del clasificador, la seguridad y la comunicación con la base de datos.

Librería Compartida (GastoHormiga.Shared): Una biblioteca de clases que contiene los Modelos de dominio (Usuarios, Suscripciones, Reglas) y los DTOs (Data Transfer Objects). Al compartirla entre el Backend y el Frontend, se asegura la consistencia de los datos y se evita la duplicidad de código.

Base de Datos: Gestionada a través de Entity Framework Core utilizando un enfoque Code-First mediante migraciones controladas.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------

💻 Lenguajes y Tecnologías Ocupadas

C#: El lenguaje principal utilizado tanto en el backend (API) como en el frontend (Blazor).

.NET Core / ASP.NET Core: Framework principal de la solución.

HTML5 y CSS3: Para la estructura visual y el diseño, complementado con el framework Bootstrap para asegurar la responsividad (diseño móvil y de escritorio).

SQL: (Vía Entity Framework Core) Para el almacenamiento relacional de datos.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------

🔒 Seguridad

La protección de la información del usuario es una prioridad en el proyecto. Se implementaron los siguientes mecanismos:

Autenticación por Token (JWT): Las sesiones se manejan a través de JSON Web Tokens, asegurando que solo usuarios autenticados puedan acceder a los endpoints protegidos.

Proveedores de Estado Personalizados (Custom Auth State Provider): En el frontend de Blazor, se maneja el estado de la sesión para mostrar u ocultar componentes según los permisos del usuario.

Verificación de Identidad: Integración de envío de códigos OTP (One-Time Password) a través de servicios de SMS (incluyendo implementaciones Mock para desarrollo).

Protección de Datos: Las contraseñas se almacenan mediante algoritmos de hashing seguro, y la estructura de la base de datos aísla la información de cada usuario.

------------------------------------------------------------------------------------------------------------------------------------------------------------------------

⚙️ Cómo se puede acceder al proyecto

Para correr este proyecto en un entorno local, sigue estos pasos:

Clonar el repositorio: Descarga el código fuente mediante Git.

Configurar Base de Datos: Abre el archivo appsettings.json o appsettings.Development.json en el proyecto GastoHormiga.API y asegúrate de que la cadena de conexión (Connection String) apunte a tu servidor SQL local.

Aplicar Migraciones: Abre la consola del administrador de paquetes (o tu terminal) en el proyecto de la API y ejecuta Update-Database (o dotnet ef database update) para generar las tablas en tu base de datos.

Ejecutar la Solución:

**Inicia el proyecto GastoHormiga.API para levantar el servidor backend.**

Inicia el proyecto GastoHormiga.Web (Blazor WebAssembly) para levantar el cliente.

Uso: Navega a la URL local (por defecto suele ser https://localhost:xxxx) y regístrate como un nuevo usuario para probar el sistema.

*************************************************************************************************************************************************************************

**Proyecto creado y desarrollado por Álvaro Brizuela Rivera.**
