# MovieApp — WinUI 3 Desktop Application

A comprehensive movie catalog, review, battle, and discussion platform built with **WinUI 3**, **.NET 8**, and **Entity Framework Core 8**.

## 📦 Project Structure

```
MovieApp.sln
├── MovieApp.Core        → Models, Services, Interfaces, DbContext
├── MovieApp.UI          → WinUI 3 XAML Views + MVVM ViewModels
└── MovieApp.Tests       → xUnit unit tests with Moq
```

## 🛠️ Prerequisites

- **Visual Studio 2022 (17.8+)** with:
  - .NET 8 SDK
  - Windows App SDK workload
  - SQL Server Express LocalDB
- **SQL Server LocalDB** (included with Visual Studio)

## 🚀 How to Run

### 1. Clone / Open the Solution
Open `MovieApp.sln` in Visual Studio 2022.

### 2. Restore NuGet Packages
```bash
dotnet restore
```

### 3. Create the Database
The database is created automatically on first run via `EnsureCreatedAsync()`. The connection string uses LocalDB:
```
Server=(localdb)\mssqllocaldb;Database=MovieAppDb;Trusted_Connection=True;
```

### 4. Run the Application
Set **MovieApp.UI** as the startup project, then press **F5** or:
```bash
dotnet run --project MovieApp.UI
```

### 5. Run Tests
```bash
dotnet test MovieApp.Tests
```

## 📋 Features

### 🎬 Movie Catalog
- Browse all movies with poster, title, year, genre, and rating
- Real-time search by title
- Filter by genre and minimum rating

### ✍️ Reviews
- Add star ratings (0–5, 0.5 increments)
- Submit extended reviews with 5 categories (Cinematography, Acting, CGI, Plot, Sound)
- One review per user per movie enforced

### ⚔️ Battle Arena
- Weekly movie battles between similarly-rated films
- Place bets with earned points
- Winners determined by rating improvement over the week

### 💬 Forum
- Threaded comment discussions per movie
- Reply to existing comments
- Max 10,000 character comments

### 🏆 Points & Badges
- Earn points for reviewing movies
- Six achievement badges (The Snob, The Super Serious, The Joker, Godfather I/II/III)

### 🎭 External Reviews
- Mock critic reviews (NYT, Guardian, OMDb)
- Aggregate critic/audience scores
- Lexicon analysis and polarization detection

## 🧪 Test Coverage

| Service | Test Cases |
|---------|-----------|
| ReviewService | Add review, duplicate check, invalid rating, average update |
| PointService | +2/+1/+5 scoring rules, freeze points validation |
| BattleService | Rating diff validation, duplicate bet, winner determination |
| BadgeService | Badge awarding, duplicate prevention |

## 🏗️ Architecture

- **MVVM Pattern** — Views, ViewModels, Models cleanly separated
- **Dependency Injection** — All services registered in `App.xaml.cs`
- **Async/Await** — All database operations are async
- **Entity Framework Core 8** — Code-first with SQL Server LocalDB
- **Hard-coded User** — UserId = 1 (no authentication required)

## 📝 Database Schema

9 entities: Movie, User, Review, Comment, Battle, Bet, UserStats, Badge, UserBadge

Composite primary keys:
- `Bet` → (UserId, BattleId)
- `UserBadge` → (UserId, BadgeId)
