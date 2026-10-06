#!/bin/bash
set -e

echo "Starting Database Initializations..."

echo "Waiting for SQL Server to be ready..."
sleep 15

export PATH="$PATH:/root/.dotnet/tools"
dotnet tool restore

echo "Running UserService Migrations..."
dotnet ef database update --project UserService/UserService.Infrastructure --startup-project UserService/UserService.API --connection "Server=sqlserver,1433;Database=UserServiceDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"

echo "Running ProductService Migrations..."
dotnet ef database update --project ProductService/ProductService.Infrastructure --startup-project ProductService/ProductService.API --connection "Server=sqlserver,1433;Database=ProductServiceDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"

echo "Running OrderService Migrations..."
dotnet ef database update --project OrderService/OrderService.Infrastructure --startup-project OrderService/OrderService.API --connection "Server=sqlserver,1433;Database=OrderServiceDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"

echo "Running PaymentService Migrations..."
dotnet ef database update --project PaymentService/PaymentService.Infrastructure --startup-project PaymentService/PaymentService.API --connection "Server=sqlserver,1433;Database=PaymentServiceDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True"

echo "All migrations completed successfully!"
