# PureTCO Web App

A .NET 10 Web API application with Entity Framework Core and PostgreSQL integration, ready for Kubernetes deployment.

## Features

- .NET 10 Web API
- Entity Framework Core 10.0.1
- PostgreSQL Database Provider (Npgsql.EntityFrameworkCore.PostgreSQL)
- Minimal APIs
- OpenAPI/Swagger Documentation
- CRUD Operations for Products
- **Kubernetes/k3s Ready** with Kustomize deployment

## Quick Start

### 🚀 Kubernetes Deployment (Recommended)

Deploy to k3s cluster with one command:

```bash
# Deploy development environment
./deploy.sh

# Deploy production environment
./deploy.sh production

# Monitor deployment
./monitor.sh status

# Access application (if LoadBalancer not available)
./monitor.sh forward app 8080
```

### 🔧 Local Development

### Prerequisites

- .NET 10 SDK
- PostgreSQL Server (local or remote)

### Setup

1. **Clone and navigate to the project**
   ```bash
   cd PureTCOWebApp
   ```

2. **Update the database connection string** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=puretcowebappdb;Username=postgres;Password=postgres;"
     }
   }
   ```
   
   Update the Host, Username, and Password according to your PostgreSQL setup.

3. **Apply database migrations**:
   ```bash
   dotnet ef database update
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

5. **Access the API**:
   - OpenAPI/Swagger: `https://localhost:7XXX/openapi/v1.json`
   - Weather forecast: `GET /weatherforecast`
   - Products API:
     - `GET /products` - Get all products
     - `GET /products/{id}` - Get product by ID
     - `POST /products` - Create new product
     - `PUT /products/{id}` - Update product
     - `DELETE /products/{id}` - Delete product

## Database Schema

### Products Table (PostgreSQL)

- `id` (integer, Primary Key, auto-increment)
- `name` (varchar(100), Required)
- `description` (varchar(500), Optional)
- `price` (numeric(18,2), Required)
- `created_at` (timestamp, Required)
- `updated_at` (timestamp, Optional)

**Note:** The application uses PostgreSQL naming conventions with snake_case column names.

## PostgreSQL Setup

### Install PostgreSQL

**macOS (using Homebrew):**
```bash
brew install postgresql
brew services start postgresql
```

**Ubuntu/Debian:**
```bash
sudo apt update
sudo apt install postgresql postgresql-contrib
sudo systemctl start postgresql
```

**Windows:**
Download and install from [PostgreSQL official website](https://www.postgresql.org/download/windows/)

### Create Database and User

```sql
-- Connect to PostgreSQL as superuser
sudo -u postgres psql

-- Create database
CREATE DATABASE puretcowebappdb;

-- Create user (optional, if not using default postgres user)
CREATE USER puretcouser WITH PASSWORD 'yourpassword';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE puretcowebappdb TO puretcouser;

-- Exit
\q
```

## Entity Framework Commands

- **Add Migration**: `dotnet ef migrations add <MigrationName>`
- **Update Database**: `dotnet ef database update`
- **Remove Last Migration**: `dotnet ef migrations remove`
- **List Migrations**: `dotnet ef migrations list`

## Sample API Requests

### Create Product
```http
POST /products
Content-Type: application/json

{
  "name": "New Product",
  "description": "Product description",
  "price": 49.99
}
```

### Update Product
```http
PUT /products/1
Content-Type: application/json

{
  "name": "Updated Product",
  "description": "Updated description",
  "price": 59.99
}
```

## Connection String Examples

### Local PostgreSQL
```
Host=localhost;Database=puretcowebappdb;Username=postgres;Password=yourpassword;
```

### PostgreSQL with SSL
```
Host=localhost;Database=puretcowebappdb;Username=postgres;Password=yourpassword;SSL Mode=Require;
```

### Cloud PostgreSQL (Azure, AWS RDS, etc.)
```
Host=your-server.postgres.database.azure.com;Database=puretcowebappdb;Username=yourusername;Password=yourpassword;SSL Mode=Require;
```