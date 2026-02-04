# DevComunity

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/PhamMinhKha0710/DevComunity)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple)](https://dotnet.microsoft.com/)
[![Next.js 15](https://img.shields.io/badge/Next.js-15-black)](https://nextjs.org/)

**DevComunity** is a comprehensive social platform tailored for developers. It combines the structured knowledge sharing of Q&A sites with the real-time engagement of social networks. Built with **Clean Architecture** principles, it ensures scalability, maintainability, and high performance.

![DevCommunity Logo](https://github.com/user-attachments/assets/4d958968-526b-446a-9379-4e0231cd9ee8)

---

## 🚀 Features

### 🧠 Knowledge Sharing (Q&A)
- **Ask & Answer**: Create markdown-rich questions and answers.
- **Voting System**: Upvote/downvote content to bubble up quality (Stack Overflow style).
- **Accepted Answers**: Mark the best solution.
- **Comments**: Discuss specifics on questions or answers.
- **Tags**: Categorize content for easy discovery.
- **Saved Items**: Bookmark useful questions for later.

### 🤝 Social & Networking
- **Real-time Chat**: Private messaging and group chats using SignalR.
- **Friendships**: Send friend requests, accept/decline.
- **Following**: Follow users to see their activity.
- **Groups**: Create and join developer communities/groups.
- **Newsfeed**: Personalized feed based on friends, followed users, and groups.
- **User Profiles**: Showcase reputation, badges, and activity history.

### 🏆 Gamification
- **Reputation System**: Earn points for contributions (votes, accepted answers).
- **Badges**: Unlock achievements for milestones.

### 🛠️ Developer Tools
- **Repositories**: Integration for sharing code (File/Folder management).
- **Gitea Integration**: (In Development) Connect with self-hosted Git.

---

## 🏗️ Technology Stack

### Backend
| Component | Technology | Description |
|-----------|------------|-------------|
| **Framework** | ASP.NET Core 9.0 | High-performance Web API |
| **Architecture** | Clean Architecture | CQRS (MediatR), Domain-Driven Design |
| **Database** | SQL Server + EF Core 9 | Robust relational data & ORM |
| **Real-time** | SignalR | Websockets for chat and notifications |
| **Auth** | JWT & BCrypt | Secure stateless authentication |

### Frontend
| Component | Technology | Description |
|-----------|------------|-------------|
| **Framework** | Next.js 15 | React framework with App Router |
| **Language** | TypeScript | Type safety and better DX |
| **UI** | Bootstrap 5 / Custom CSS | Responsive design (migrating to polished UI) |
| **State** | React Context + Hooks | Efficient state management |
| **Real-time** | @microsoft/signalr | Client-side socket management |

---

## 📂 Project Structure

```bash
socialtechsy-social-network/
├── backend/                    # ASP.NET Core Solution
│   ├── src/
│   │   ├── SocialTechsy.SocialNetwork.Api/           # Entry point, Controllers, Hubs
│   │   ├── SocialTechsy.SocialNetwork.Application/   # Business Logic (CQRS)
│   │   ├── SocialTechsy.SocialNetwork.Domain/        # Entities, Enums, Interfaces
│   │   ├── SocialTechsy.SocialNetwork.Infrastructure/# DB Context, Repositories, External Services
│   │   └── SocialTechsy.SocialNetwork.Shared/        # DTOs, Common Utils
│   └── tests/                  # Unit & Integration Tests
├── frontend/                   # Next.js Application
│   ├── src/
│   │   ├── app/                # Pages & Layouts
│   │   ├── components/         # Reusable UI Components
│   │   ├── contexts/           # Global State (Auth, Socket)
│   │   └── services/           # API Handling
├── .gitignore
└── README.md
```

---

## ⚡ Getting Started

### Prerequisites
- **.NET 9.0 SDK**
- **Node.js 18+** & **npm**
- **SQL Server** (Local or Container)

### Backend Setup
1.  Navigate to the API folder:
    ```bash
    cd backend/src/DevComunity.Api
    ```
2.  Configure `appsettings.json` with your SQL connection string.
3.  Run migrations:
    ```bash
    dotnet ef database update --project ../DevComunity.Infrastructure
    ```
4.  Start the server:
    ```bash
    dotnet run
    ```
    API will run at `https://localhost:7001`. Swagger docs at: `https://localhost:7001/swagger`.

### Frontend Setup
1.  Navigate to the frontend folder:
    ```bash
    cd frontend
    ```
2.  Install dependencies:
    ```bash
    npm install
    ```
3.  Start the development server:
    ```bash
    npm run dev
    ```
    App will run at `http://localhost:3000`.

---

## 🤝 Contributing

We welcome contributions!
1.  Fork the project.
2.  Create your feature branch (`git checkout -b feature/AmazingFeature`).
3.  Commit your changes (`git commit -m 'Add some AmazingFeature'`).
4.  Push to the branch (`git push origin feature/AmazingFeature`).
5.  Open a Pull Request.

---

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

<p align="center">
  Made with ❤️ by the DevCommunity Team
</p>
