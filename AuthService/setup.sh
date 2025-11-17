#!/bin/bash

# AuthService Setup Script

echo "=== AuthService Database Setup ==="
echo ""

# Step 1: Check .env file
if [ ! -f ".env" ]; then
    echo "❌ .env file not found!"
    echo "Please create a .env file with the following content:"
    echo ""
    echo "DATABASE_HOST=your-db-hostname"
    echo "DATABASE_PORT=5432"
    echo "DATABASE_USER=your-db-user"
    echo "DATABASE_PASSWORD=your-db-password"
    echo "DATABASE_NAME=authservice"
    exit 1
fi

echo "✅ .env file found"
echo ""

# Step 2: Restore packages
echo "=== Restoring NuGet packages ==="
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ Failed to restore packages"
    exit 1
fi
echo "✅ Packages restored successfully"
echo ""

# Step 3: Build project
echo "=== Building project ==="
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi
echo "✅ Build successful"
echo ""

# Step 4: Create migration (if not exists)
if [ ! -d "Migrations" ]; then
    echo "=== Creating initial database migration ==="
    echo "Note: If dotnet-ef is not installed, run:"
    echo "  dotnet tool install --global dotnet-ef --version 9.0.0"
    echo ""
    
    # Try to run migration
    dotnet ef migrations add InitialCreate
    if [ $? -ne 0 ]; then
        echo "⚠️  Migration creation failed. You may need to install dotnet-ef tool manually."
        echo "   Run: dotnet tool install --global dotnet-ef --version 9.0.0"
        echo ""
    else
        echo "✅ Migration created successfully"
        echo ""
    fi
fi

# Step 5: Apply migrations
echo "=== Applying database migrations ==="
echo "This will create/update the database schema"
echo ""

dotnet ef database update
if [ $? -ne 0 ]; then
    echo "⚠️  Database update failed."
    echo "   Please ensure:"
    echo "   1. PostgreSQL is running"
    echo "   2. Database credentials in .env are correct"
    echo "   3. Database 'authservice' exists"
    echo ""
    echo "   To manually create the migrations and update:"
    echo "   dotnet ef migrations add InitialCreate"
    echo "   dotnet ef database update"
    exit 1
fi

echo "✅ Database updated successfully"
echo ""
echo "=== Setup Complete! ==="
echo ""
echo "To run the service:"
echo "  dotnet run"
echo ""
echo "The service will be available at: https://localhost:5001"
echo "Swagger UI: https://localhost:5001/swagger"
echo ""
