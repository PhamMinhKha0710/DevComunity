# DevCommunity

## Developer Community Platform - Q&A and Code Sharing

![DevCommunity Logo](https://github.com/user-attachments/assets/4d958968-526b-446a-9379-4e0231cd9ee8)

DevCommunity is a platform connecting developers through code sharing and technical Q&A. Built with Clean Architecture principles, it combines features from GitHub and Stack Overflow to create an effective learning and knowledge-sharing environment.

---

## Project Structure

```
DevComunity/
├── backend/                    # ASP.NET Core Clean Architecture API
│   ├── src/
│   │   ├── DevComunity.Api/           # API Controllers, Hubs, Entry Point
│   │   ├── DevComunity.Application/   # CQRS Commands, Queries, Handlers
│   │   ├── DevComunity.Domain/        # Domain Entities, Enums
│   │   ├── DevComunity.Infrastructure/# EF Core, Repositories, Services
│   │   └── DevComunity.Shared/        # Shared utilities
│   └── tests/                         # Unit and Integration tests
├── frontend/                   # Next.js React Frontend
│   ├── src/
│   │   ├── app/               # Next.js App Router pages
│   │   ├── components/        # Reusable React components
│   │   ├── contexts/          # React Context providers
│   │   ├── hooks/             # Custom React hooks
│   │   ├── api/               # API client
│   │   └── types/             # TypeScript types
│   └── public/                # Static assets
├── frontend-vite-backup/       # Legacy Vite frontend (backup)
├── _archived/                  # Archived legacy code (GitIntegration)
├── .gitignore
├── .gitattributes
└── README.md
```

---

## Features

### User Management
- JWT-based authentication with Register/Login
- OAuth integration (Google, GitHub) - planned
- Role-based authorization (Admin, Moderator, User)
- User profiles and activity tracking

### Q&A System
- Create, edit, delete questions with Markdown support
- Answer questions with accept functionality
- Upvote/Downvote system for questions and answers
- Tag-based categorization
- Real-time updates via SignalR

### Code Repositories
- Create public/private repositories
- File and folder management
- Gitea integration for source control (in development)

### Real-time Features
- SignalR hubs for:
  - Chat messaging
  - Notifications
  - Question updates
  - User presence
  - Activity feed

### Additional Features
- Badge and reputation system
- Saved items/bookmarks
- Full-text search
- Tag preferences (watch/ignore)

---

## Technology Stack

### Backend
- **ASP.NET Core 9.0** - Web API framework
- **Entity Framework Core 9.0** - ORM
- **SQL Server** - Database
- **SignalR** - Real-time communication
- **JWT** - Authentication tokens
- **Clean Architecture** - CQRS pattern with Commands/Queries
- **BCrypt** - Password hashing

### Frontend
- **Next.js 15** - React framework with App Router
- **TypeScript** - Type safety
- **Bootstrap 5** - UI framework
- **@microsoft/signalr** - Real-time client

---

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Node.js 18+ and npm
- SQL Server 2022+
- Visual Studio 2022 or VS Code

### Backend Setup

```bash
# Navigate to backend
cd backend/src/DevComunity.Api

# Restore packages
dotnet restore

# Update database
dotnet ef database update --project ../DevComunity.Infrastructure

# Run the API
dotnet run
```

The API will be available at `https://localhost:7001` with Swagger at `/swagger`.

### Frontend Setup

```bash
# Navigate to frontend
cd frontend

# Install dependencies
npm install

# Run development server
npm run dev
```

The frontend will be available at `http://localhost:3000`.

### Configuration

#### Backend (`backend/src/DevComunity.Api/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DevComunity;..."
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "DevComunity",
    "Audience": "DevComunity",
    "ExpirationMinutes": 60
  }
}
```

---

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token

### Questions
- `GET /api/questions` - List questions (paginated)
- `GET /api/questions/{id}` - Get question details
- `POST /api/questions` - Create question
- `PUT /api/questions/{id}` - Update question
- `DELETE /api/questions/{id}` - Delete question

### Answers
- `GET /api/answers/question/{questionId}` - Get answers for question
- `POST /api/answers` - Create answer
- `PUT /api/answers/{id}` - Update answer
- `DELETE /api/answers/{id}` - Delete answer
- `POST /api/answers/{id}/accept` - Accept answer

### Users
- `GET /api/users/me` - Get current user
- `GET /api/users/{id}` - Get user by ID

### SignalR Hubs
- `/hubs/chat` - Chat messaging
- `/hubs/notification` - Notifications
- `/hubs/question` - Question updates
- `/hubs/presence` - User presence
- `/hubs/activity` - Activity feed

---

## Development Status

### Fully Implemented
- Questions CRUD with CQRS handlers
- Answers CRUD with Accept functionality
- User authentication (JWT)
- User queries
- SignalR hub structure

### In Development (TODO)
- Votes system handlers
- Comments system handlers
- Tags system handlers
- Badges system handlers
- Saved items handlers
- Notifications handlers
- Repository/Gitea integration

### Archived
- Legacy MVC code preserved in `_archived/GitIntegration/` for reference

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit changes (`git commit -m 'Add some feature'`)
4. Push to branch (`git push origin feature/your-feature`)
5. Create a Pull Request

---

## License

This project is licensed under the MIT License.

---

<p align="center">Made with love by the DevCommunity Team</p>
