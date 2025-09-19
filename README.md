# Weather API

API que consome a **Weather API(https://www.weatherapi.com/)** para fornecer clima atual, previsão e histórico de buscas, com caching no banco de dados.

---

## Configuração

Caso haja necessidade de criar uma conta propria na plataforma, basta após o cadastro estiver completo pegar a API Key disponibilizada no seu Dashboard 

<img width="1799" height="266" alt="image" src="https://github.com/user-attachments/assets/f8a8c43b-cb5b-408c-897a-bf94cf286bc2" />

Altere a API key e o Banco de Dados aqui:
- **API Key**: `appSettings.json -> OpenWeather -> ApiKey`
- **Banco de Dados**: `appSettings.json -> ConnectionStrings -> DefaultConnection`

Antes de rodar, crie o banco e a tabela de cache:

```sql
CREATE DATABASE WeatherCacheDb;
GO
USE WeatherCacheDb;
GO
CREATE TABLE CachedWeathers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    City NVARCHAR(200) NOT NULL,
    Type NVARCHAR(50) NOT NULL,
    PayloadJson NVARCHAR(MAX) NOT NULL,
    RetrievedAtUtc DATETIME2 NOT NULL
);
GO
CREATE INDEX IX_CachedWeathers_City_Type
    ON CachedWeathers (City, Type);
GO


Basta Rodar e ser feliz :)
