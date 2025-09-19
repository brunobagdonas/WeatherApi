# Weather API

API que consome a **Weather API ([https://www.weatherapi.com/](https://www.weatherapi.com/))** para fornecer clima atual, previsão e histórico de buscas, com caching no banco de dados.

---

## Configuração

Caso haja necessidade de criar uma conta própria na plataforma, basta, após o cadastro estar completo, pegar a API Key disponibilizada no seu Dashboard. (Essa API KEY tem a gratuidade ate 02/10/2025):

<img width="1175" height="279" alt="image" src="https://github.com/user-attachments/assets/4c42e3d1-38ab-4a95-b802-afe64b003a67" />


Altere a API Key e o Banco de Dados aqui:

* **API Key**: `appSettings.json -> OpenWeather -> ApiKey`
* **Banco de Dados**: `appSettings.json -> ConnectionStrings -> DefaultConnection`

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
```

---

## Endpoints

OBS: No endpoint 1 e 2, a ideia foi criar uma Colunma chamada Key a fim de controle para saber o que ja foi chamado. Poderia chamar Paris, tanto no dia de hoje como nos proximos dias. No Endpoint 2, liberei a possibilidade de escolher a quantidade de dias (Até no maximo 5), e fazer as separações Por forecast_qtdDias, onde qtdDias é a quantidade que eu quero ver, assim podendo chamar nos proximos 3 dias, e se eu quiser ver os proximos dias, eu chamo a API novamente para evitar falta de dados. Sendo assim, para um unico Lugar posso chamar a API 6 vezes. Para uma v2 seria interessante criar uma forma de verificação que se eu tiver mais dias do que eu pedindo, inves de chamar a api de novo, tratar o dado para mostrar apenas a quantidade de dias selecionadas. (Exemplo: Selecionei 5 dias, agora so quero ver 4, inves de chamar a API, "esconder" o ultimo dia da lista e assim para outros dias).

### 1. Clima Atual

```
GET /api/weather/current?city={city}
```

Retorna o clima atual da cidade informada.

**Query Parameters:**

* `city` (obrigatório): Nome da cidade para consulta.

**Exemplo de resposta:**

```json
{
  "city": "London",
  "temperatureC": 15.5,
  "humidity": 70,
  "description": "Partly cloudy",
  "windKph": 12.3
}
```

---

### 2. Previsão do Tempo

```
GET /api/weather/forecast?city={city}&daysQuantity={1-5}
```

Retorna a previsão do tempo para os próximos dias.

**Query Parameters:**

* `city` (obrigatório): Nome da cidade.
* `daysQuantity` (opcional): Quantidade de dias de previsão (1 a 5). No maximo: 5.

**Exemplo de resposta:**

```json
{
  "city": "London",
  "days": [
    {
      "date": "2025-09-20",
      "maxTempC": 18.2,
      "minTempC": 10.1,
      "avgTempC": 14.5,
      "avgHumidity": 65,
      "maxWindKph": 15.0,
      "condition": "Sunny"
    },
    {
      "date": "2025-09-21",
      "maxTempC": 19.0,
      "minTempC": 11.0,
      "avgTempC": 15.0,
      "avgHumidity": 60,
      "maxWindKph": 12.5,
      "condition": "Partly cloudy"
    }
  ]
}
```

---

### 3. Histórico de Buscas

```
GET /api/weather/history
```

Retorna as últimas cidades consultadas, incluindo o tipo de consulta (`current` ou `forecast_{qtdDias}`) e a data/hora da busca (Horário de Brasilia).

**Exemplo de resposta:**

```json
[
  { "city": "London", "type": "current", "retrievedAtUtc": "2025-09-19T16:23:00Z" },
  { "city": "Paris", "type": "forecast_2", "retrievedAtUtc": "2025-09-19T16:15:00Z" }
]
```

---

## Observações

* Os dados são armazenados em **cache** no banco de dados para reduzir chamadas à API externa.
* O tempo de expiração do cache é configurável via `appSettings.json -> Cache -> ExpirationMinutes`.

---

## Quickstart 

1. Altere a API Key e a ConnectionString no `appSettings.json`.
2. Crie o banco e a tabela de cache conforme mostrado acima.
3. Execute a aplicação com `dotnet run` ou via Visual Studio.

O `index.html` ficará em `coverage-report/index.html`.
