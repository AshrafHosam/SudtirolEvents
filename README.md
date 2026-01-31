# Open Data Hub Weather & Activity Assistant

A full-stack web application that fetches and displays weather data, events, and points of interest from the Open Data Hub API for South Tyrol. It uses OpenAI to generate natural language recommendations and explanations.

## ??? Architecture

```
OpenDataHubAssistant/
??? src/
?   ??? OpenDataHubAssistant.Api/          # ASP.NET Core Web API
?   ?   ??? Controllers/                    # API endpoints
?   ?   ??? Program.cs                      # Application entry point
?   ??? OpenDataHubAssistant.Core/         # Domain layer
?   ?   ??? Models/                         # Entity models
?   ?   ??? DTOs/                           # Data transfer objects
?   ?   ??? Interfaces/                     # Service interfaces
?   ?   ??? Services/                       # Business logic
?   ?   ??? Enums/                          # Enumerations
?   ??? OpenDataHubAssistant.Infrastructure/ # Data & external services
?       ??? Data/                           # EF Core DbContext
?       ??? Repositories/                   # Repository implementations
?       ??? ExternalClients/                # Open Data Hub API clients
?       ??? Services/                       # OpenAI LLM service
?       ??? Configuration/                  # Settings classes
??? frontend/                               # Angular application
?   ??? src/
?       ??? app/
?           ??? components/                 # Chat & Direct query pages
?           ??? services/                   # API service
?           ??? models/                     # TypeScript interfaces
??? README.md
```

## ? Features

- **Weather Data**: Fetch current and forecasted weather for South Tyrol locations
- **Events**: Get upcoming events from Open Data Hub
- **Points of Interest**: Discover attractions, museums, and activities
- **Weather Classification**: Automatic classification (Good/Bad/Rainy/Cold/Hot/Windy)
- **Activity Recommendations**: Smart suggestions based on weather conditions
- **AI-Powered Chat**: Natural language interface using OpenAI
- **Two UI Modes**:
  - **Chat Interface**: Conversational Q&A about weather and activities
  - **Direct Query**: Form-based data lookup with detailed results

## ?? Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- [Angular CLI](https://angular.io/cli) (optional, can use npx)

### Configuration

#### 1. Open Data Hub API (No configuration needed)

The application uses the public Open Data Hub Tourism API endpoints by default:
- Weather: `https://tourism.opendatahub.com/v1/Weather`
- Events: `https://tourism.opendatahub.com/v1/Event`
- POI: `https://tourism.opendatahub.com/v1/ODHActivityPoi`

These can be customized in `src/OpenDataHubAssistant.Api/appsettings.json`:

```json
{
  "OpenDataHub": {
    "WeatherApiBaseUrl": "https://tourism.opendatahub.com/v1/Weather",
    "EventsApiBaseUrl": "https://tourism.opendatahub.com/v1/Event",
    "PoiApiBaseUrl": "https://tourism.opendatahub.com/v1/ODHActivityPoi",
    "CacheTtlMinutes": 15,
    "TimeoutSeconds": 30,
    "RetryCount": 3
  }
}
```

#### 2. OpenAI Configuration

To enable AI-powered responses, configure OpenAI:

**Option A: Environment Variable (Recommended)**
```bash
# Windows PowerShell
$env:OPENAI_API_KEY = "your-api-key-here"

# Linux/macOS
export OPENAI_API_KEY="your-api-key-here"
```

**Option B: appsettings.json** (Not recommended for production)
```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "Model": "gpt-3.5-turbo",
    "BaseUrl": "https://api.openai.com/v1",
    "MaxTokens": 500,
    "Temperature": 0.7
  }
}
```

> **Note**: Without an OpenAI API key, the application will use rule-based fallback responses.

#### 3. Database Configuration

The application uses SQLite by default. The database file (`opendatahub.db`) is created automatically in the API project directory.

To use a different database provider, update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=opendatahub.db"
  }
}
```

### Running the Application

#### Backend (API)

```bash
# Navigate to the solution directory
cd "D:\Masters\UNIBZ\Semester 1\Architecture\Project\impl trial"

# Restore packages and build
dotnet build

# Run the API
cd src/OpenDataHubAssistant.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5213`
- HTTPS: `https://localhost:7088`
- Swagger UI: `https://localhost:7088/swagger`

#### Frontend (Angular)

```bash
# Navigate to the frontend directory
cd frontend

# Install dependencies
npm install

# Run the development server
npm start
```

The Angular app will be available at `http://localhost:4200`

### Development Commands

#### Backend
```bash
# Build
dotnet build

# Run tests (if available)
dotnet test

# Create new migration
dotnet ef migrations add MigrationName --project src/OpenDataHubAssistant.Infrastructure --startup-project src/OpenDataHubAssistant.Api

# Apply migrations
dotnet ef database update --project src/OpenDataHubAssistant.Infrastructure --startup-project src/OpenDataHubAssistant.Api
```

#### Frontend
```bash
# Development server
npm start

# Build for production
npm run build

# Lint
npm run lint
```

## ?? API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/locations` | GET | List all available locations |
| `/api/locations/{id}` | GET | Get a specific location |
| `/api/weather?lat={lat}&lon={lon}&date={date}` | GET | Get weather by coordinates |
| `/api/weather/city/{city}?date={date}` | GET | Get weather by city name |
| `/api/recommendations?locationId={id}&date={date}` | GET | Get AI-powered recommendations |
| `/api/recommendations/city/{city}?date={date}` | GET | Get recommendations by city |
| `/api/chat` | POST | Send chat message |
| `/api/chat/status` | GET | Check LLM availability |
| `/health` | GET | Health check endpoint |

## ??? Database Schema

### Location
- `Id` (int, PK)
- `Name` (string)
- `Latitude` (double)
- `Longitude` (double)

### WeatherSnapshot
- `Id` (int, PK)
- `LocationId` (int, FK)
- `Timestamp` (DateTime)
- `TemperatureC` (double)
- `PrecipitationMm` (double)
- `WindKph` (double)
- `ConditionText` (string)
- `RawJson` (string, nullable)

### Event
- `Id` (int, PK)
- `Name` (string)
- `Description` (string)
- `StartDate` (DateTime)
- `EndDate` (DateTime)
- `Location` (string)
- `IsIndoor` (bool)
- `ExternalId` (string)

### PointOfInterest
- `Id` (int, PK)
- `Name` (string)
- `Type` (string)
- `Description` (string)
- `Address` (string)
- `IsIndoor` (bool)
- `ExternalId` (string)
- `Latitude` (double, nullable)
- `Longitude` (double, nullable)

### RecommendationLog
- `Id` (int, PK)
- `LocationId` (int, FK, nullable)
- `Timestamp` (DateTime)
- `QueryText` (string)
- `RecommendationText` (string)
- `SourceDataSummary` (string)

## ??? Weather Classification Rules

| Classification | Condition |
|---------------|-----------|
| Good | No bad conditions present |
| Bad | Any of: Rainy, Cold, or Windy |
| Cold | Temperature < 10°C |
| Hot | Temperature > 30°C |
| Rainy | Precipitation > 0.5mm |
| Windy | Wind > 30 km/h |

## ?? Pre-seeded Locations

The database includes these South Tyrol locations:

| Name | Latitude | Longitude |
|------|----------|-----------|
| Bolzano | 46.4983 | 11.3548 |
| Merano | 46.6713 | 11.1594 |
| Bressanone | 46.7176 | 11.6565 |
| Brunico | 46.7964 | 11.9365 |
| Vipiteno | 46.8958 | 11.4328 |

## ?? Technology Stack

### Backend
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- SQLite (default, configurable)
- Polly (retry policies)

### Frontend
- Angular 19+
- TypeScript
- RxJS
- CSS (no external framework)

### External Services
- Open Data Hub Tourism API
- OpenAI API (GPT-3.5/GPT-4)

## ?? TODO

- [ ] Add unit tests for services
- [ ] Add integration tests for API endpoints
- [ ] Implement user authentication (if needed)
- [ ] Add support for multiple languages
- [ ] Enhance weather parsing for more detailed data
- [ ] Add caching layer for database queries

## ?? License

This project is for educational purposes.

## ?? Acknowledgments

- [Open Data Hub](https://opendatahub.com/) for providing the South Tyrol tourism data API
- [OpenAI](https://openai.com/) for the language model API
