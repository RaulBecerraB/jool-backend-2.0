# Jool Backend

A modern REST API developed in .NET 8.0 for a question and answer platform, similar to Stack Overflow, with JWT authentication and Microsoft OAuth functionalities.

## 📋 Description

Jool Backend is a web API application that allows users to ask questions, answer them, and organize them using hashtags. The platform includes a robust authentication system, automatic validations, and a scalable architecture using Entity Framework Core with MySQL.

## 🏗️ Architecture

The project follows a layered architecture with the following components:

```
├── Controllers/          # REST API controllers
├── Services/            # Business logic
├── Repository/          # Data access and EF Core context
├── Models/              # Domain entities
├── DTOs/                # Data transfer objects
├── Validations/         # Validators using FluentValidation
├── Utils/               # Utilities (Security, Logging, Mapping)
└── Migrations/          # Database migrations
```

## 🚀 Technologies Used

- **.NET 8.0** - Main framework
- **Entity Framework Core 8.0** - ORM for data access
- **MySQL 8.0** - Database
- **JWT Bearer Authentication** - Authentication
- **Microsoft Graph API** - OAuth authentication with Microsoft
- **FluentValidation** - Input validations
- **Swagger/OpenAPI** - API documentation
- **Docker & Docker Compose** - Containerization

### Main Dependencies

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.13" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.3" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="Microsoft.Identity.Web" Version="3.9.2" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
```

## 📊 Data Model

### Main Entities

#### **User**
```csharp
public class User
{
    public int user_id { get; set; }
    public string first_name { get; set; }
    public string last_name { get; set; }
    public string email { get; set; }
    public string password { get; set; }
    public bool is_active { get; set; }
    public byte[]? image { get; set; }
    public string? phone { get; set; }
}
```

#### **Question**
```csharp
public class Question
{
    public int question_id { get; set; }
    public string title { get; set; }
    public string content { get; set; }
    public int user_id { get; set; }
    public int views { get; set; }
    public int stars { get; set; }
    public DateTime date { get; set; }
}
```

#### **Response**
```csharp
public class Response
{
    public int response_id { get; set; }
    public string content { get; set; }
    public int user_id { get; set; }
    public int question_id { get; set; }
    public int likes { get; set; }
    public DateTime date { get; set; }
}
```

#### **Hashtag**
```csharp
public class Hashtag
{
    public int hashtag_id { get; set; }
    public string name { get; set; }
    public int used_count { get; set; }
}
```

### Relationships
- **User ↔ Question**: 1:N relationship (A user can ask many questions)
- **User ↔ Response**: 1:N relationship (A user can respond many times)
- **Question ↔ Response**: 1:N relationship (A question can have many responses)
- **Question ↔ Hashtag**: N:M relationship (A question can have multiple hashtags)

## 🔐 Authentication and Security

### JWT Authentication
- **Algorithm**: HS256
- **Duration**: 525,600 minutes (1 year)
- **Claims included**: sub (user_id), email, first_name, last_name

### Microsoft OAuth 2.0
- Integration with Microsoft Graph API
- Support for personal and corporate accounts
- Scopes: `user.read`

### Password Security
- SHA256 hash for secure storage
- Verification through hash comparison

## 📡 API Endpoints

### Authentication (`/auth`)
```http
POST   /auth/register           # User registration
POST   /auth/login              # User login
GET    /auth/profile            # Authenticated user profile
GET    /auth/login-microsoft    # Start Microsoft authentication
GET    /auth/microsoft-callback # Microsoft OAuth callback
```

### Questions (`/questions`)
```http
GET    /questions               # Get all questions
GET    /questions/{id}          # Get question by ID
GET    /questions/user/{userId} # Questions from specific user
POST   /questions               # Create new question
PUT    /questions/{id}          # Update question
DELETE /questions/{id}          # Delete question
```

### Responses (`/responses`)
```http
GET    /responses               # Get all responses
GET    /responses/{id}          # Get response by ID
GET    /responses/question/{id} # Responses to a question
POST   /responses               # Create new response
PUT    /responses/{id}          # Update response
DELETE /responses/{id}          # Delete response
```

### Hashtags (`/hashtags`)
```http
GET    /hashtags                # Get all hashtags
GET    /hashtags/{id}           # Get hashtag by ID
POST   /hashtags                # Create new hashtag
PUT    /hashtags/{id}           # Update hashtag
DELETE /hashtags/{id}           # Delete hashtag
```

## 🛠️ Configuration

### Environment Variables

Create a `.env` file in the project root:

```env
# Database
DB_HOST=localhost
DB_PORT=3306
DB_NAME=jool
DB_USER=root
DB_PASSWORD=admin1
DB_CONNECTION_STRING=Server=localhost;Database=jool;User=root;Password=admin1;Port=3306;

# Microsoft OAuth
MS_CLIENT_ID=your_client_id_here
MS_CLIENT_SECRET=your_client_secret_here
```

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=jool;User=root;Password=admin1;Port=3306;"
  },
  "JWT": {
    "SecretKey": "joolSuperSecretKey2024@ASecureKeyWithAtLeast32Chars",
    "Issuer": "jool-backend",
    "Audience": "jool-clients",
    "DurationInMinutes": 525600
  },
  "Authentication": {
    "Microsoft": {
      "TenantId": "common",
      "FrontendCallbackUrl": "http://localhost:3000/auth/callback",
      "FrontendErrorUrl": "http://localhost:3000/auth/error",
      "Scope": "https://graph.microsoft.com/user.read"
    }
  }
}
```

## 🐳 Running with Docker

### Using Docker Compose (Recommended)

```bash
# Clone repository
git clone https://github.com/RaulBecerraB/jool-backend-2.0.git
cd jool-backend-2.0

# Configure environment variables
cp .env.example .env
# Edit .env with your values

# Run with Docker Compose
docker-compose up -d

# View logs
docker-compose logs -f api
```

The service will be available at:
- **API**: http://localhost:8080
- **Swagger**: http://localhost:8080/swagger
- **MySQL**: localhost:3307

### Manual Docker

```bash
# Build image
docker build -t jool-backend .

# Run container
docker run -p 8080:80 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=jool;User=root;Password=admin1;Port=3306" \
  jool-backend
```

## 💻 Local Development

### Prerequisites

- .NET 8.0 SDK
- MySQL 8.0
- Visual Studio 2022 / VS Code

### Setup

```bash
# 1. Clone repository
git clone https://github.com/RaulBecerraB/jool-backend-2.0.git
cd jool-backend-2.0

# 2. Restore dependencies
dotnet restore

# 3. Configure database
# Create 'jool' database in MySQL

# 4. Run migrations
dotnet ef database update

# 5. Run application
dotnet run

# API will be available at https://localhost:7000
```

### Migrations

```bash
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Revert migration
dotnet ef database update PreviousMigration
```

## 🧪 Validations

The project uses **FluentValidation** for robust validations:

### Validation Examples

```csharp
// User registration
RuleFor(u => u.email)
    .NotEmpty().WithMessage("Email is required")
    .EmailAddress().WithMessage("Invalid email format")
    .MustAsync(BeUniqueEmail).WithMessage("Email is already registered");

// Question creation
RuleFor(q => q.title)
    .NotEmpty().WithMessage("Title is required")
    .MaximumLength(255).WithMessage("Title cannot exceed 255 characters");
```

### Asynchronous Validations

```csharp
// Validate user exists
.MustAsync(UserExistsAsync).WithMessage("The specified user does not exist");

// Validate unique email
.MustAsync(BeUniqueEmail).WithMessage("Email is already registered");
```

## 📝 Logging

Centralized logging system in `LoggingUtils`:

```csharp
LoggingUtils.LogInfo($"User registered successfully: {user.email}", nameof(AuthService));
LoggingUtils.LogError($"Authentication error: {ex.Message}", nameof(AuthController));
```

## 🔧 Utilities

### SecurityUtils
- `HashPassword(string password)`: Generates SHA256 hash
- `VerifyPassword(string password, string hash)`: Verifies passwords

### MappingUtils
- `MapToUserDto(User user)`: Entity to DTO mapping
- `MapToQuestionDto(Question question)`: Complete conversion with relationships

### UrlUtils
- Utilities for URL handling in OAuth

## 📋 Project Status

### ✅ Completed
- JWT authentication system
- Complete CRUD for all entities
- Automatic validations
- Microsoft OAuth integration
- Swagger documentation
- Complete dockerization
- Database migrations

### 🚧 In Development
- Role and permission system
- Pagination in listings
- Advanced search with filters
- Notification system
- Image upload

### 📈 Future Improvements
- Redis caching
- Rate limiting
- Structured logging with Serilog
- Unit and integration tests
- CI/CD pipeline

## 🤝 Contributing

1. Fork the project
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is under the MIT License. See the `LICENSE` file for more details.

## 👥 Team

- **Lead Developer**: [RaulBecerraB](https://github.com/RaulBecerraB)
- **Organization**: AAAIMX Software Division

## 📞 Contact

For questions or support:
- **GitHub**: [RaulBecerraB](https://github.com/RaulBecerraB)
- **Repository**: [jool-backend-2.0](https://github.com/RaulBecerraB/jool-backend-2.0)

---

**Jool Backend** - Building the future of Q&A platforms 🚀