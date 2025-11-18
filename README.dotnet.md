# Food Diary .NET - Clean Architecture

Полноценный перенос NestJS приложения "Дневник питания" на .NET 9 с применением **Clean Architecture**.

## 🏗 Архитектура

```
FoodDiary/
├── FoodDiary.Domain/          # Доменный слой (сущности, enums)
│   ├── Entities/
│   ├── Enums/
│   └── Common/
├── FoodDiary.Contracts/       # Контракты (DTOs для API)
│   ├── Authentication/
│   ├── Users/
│   └── Food/
├── FoodDiary.Application/     # Бизнес-логика
│   ├── Services/
│   └── Common/Interfaces/
├── FoodDiary.Infrastructure/  # Инфраструктура (EF Core, JWT, BCrypt)
│   ├── Persistence/
│   ├── Authentication/
│   └── Services/
└── FoodDiary.Web.Api/         # API презентационный слой
    └── Controllers/
```

## 🎯 Принципы Clean Architecture

1. **Domain** - независимый от всех, чистый бизнес-слой
2. **Application** - use cases, зависит только от Domain
3. **Infrastructure** - реализации (БД, JWT), зависит от Application
4. **Web.Api** - точка входа, зависит от всех

## 🚀 Запуск

```bash
# Перейти в папку Web.Api
cd FoodDiary.Web.Api

# Настроить БД в appsettings.json

# Применить миграции (из корня solution)
cd ..
dotnet ef migrations add InitialCreate --project FoodDiary.Infrastructure --startup-project FoodDiary.Web.Api
dotnet ef database update --project FoodDiary.Infrastructure --startup-project FoodDiary.Web.Api

# Запустить API
cd FoodDiary.Web.Api
dotnet run
```

API доступно на:
- **http://localhost:5000**
- **https://localhost:5001**
- **Swagger**: https://localhost:5001/swagger

## 📦 Технологии

- **.NET 9** - фреймворк
- **EF Core 9** + **PostgreSQL** - ORM и БД
- **JWT Authentication** - аутентификация
- **BCrypt** - хеширование паролей
- **Swagger/OpenAPI** - документация
- **Clean Architecture** - архитектурный паттерн

## 📁 Структура слоев

### Domain Layer
- **Entities**: User, Food, Consumption, Recipe, etc.
- **Enums**: Unit, Visibility
- **No dependencies** - чистый C#

### Contracts Layer
- **Request/Response DTOs** используя records
- Shared между слоями

### Application Layer
- **Services**: AuthenticationService, UserService, FoodService
- **Interfaces**: IUserRepository, IJwtTokenGenerator, etc.
- Бизнес-логика без привязки к инфраструктуре

### Infrastructure Layer
- **DbContext**: FoodDiaryDbContext (EF Core)
- **Repositories**: UserRepository, FoodRepository
- **Authentication**: JwtTokenGenerator
- **Services**: PasswordHasher (BCrypt)

### Web.Api Layer
- **Controllers**: AuthController, UsersController, FoodController
- **DI Configuration**: Program.cs
- JWT + Swagger конфигурация

## 🔧 Dependency Injection

Каждый слой имеет свой extension method:

```csharp
builder.Services.AddApplication();      // Application layer
builder.Services.AddInfrastructure();   // Infrastructure layer
```

## 🎨 Особенности реализации

✅ **Repository Pattern** - абстракция над EF Core
✅ **JWT tokens** - access + refresh tokens
✅ **Password hashing** - BCrypt
✅ **CORS** - настроен
✅ **Swagger** - с JWT авторизацией
✅ **Records** для DTOs - immutable contracts

## 🔄 Сравнение с NestJS версией

| NestJS | .NET Clean Architecture |
|--------|------------------------|
| Modules | Projects/Layers |
| Prisma ORM | EF Core |
| Providers/Services | Services + Repositories |
| Guards | JWT Middleware |
| DTOs (class-validator) | Contracts (Records) |
| Dependency Injection | Built-in DI |

## 📝 API Endpoints

### Auth
- `POST /api/auth/register` - Регистрация
- `POST /api/auth/login` - Вход
- `POST /api/auth/refresh` - Обновление токена

### Users
- `GET /api/users/info` - Информация о пользователе
- `PATCH /api/users` - Обновление профиля

### Food
- `GET /api/food` - Список продуктов
- `POST /api/food` - Создать продукт
- `GET /api/food/{id}` - Продукт по ID
- `PUT /api/food/{id}` - Обновить продукт
- `DELETE /api/food/{id}` - Удалить продукт

## 🔮 Дальнейшее развитие

- [ ] Добавить Consumption endpoints
- [ ] Добавить Recipe endpoints
- [ ] Добавить Statistics endpoints
- [ ] Unit тесты (xUnit)
- [ ] Integration тесты
- [ ] FluentValidation для валидации
- [ ] MediatR для CQRS
- [ ] Serilog для логирования
- [ ] Redis для кеширования
- [ ] Docker контейнеризация

## 📖 Ресурсы

- [Clean Architecture by Robert Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [.NET Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)

---

Создано с использованием Clean Architecture принципов 🏛️
