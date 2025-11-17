#!/bin/bash

# Database Migration Helper Script
# This script helps manage Entity Framework Core migrations

case "$1" in
    "create")
        if [ -z "$2" ]; then
            echo "Usage: ./migration.sh create <MigrationName>"
            echo "Example: ./migration.sh create AddUserRoles"
            exit 1
        fi
        echo "Creating migration: $2"
        dotnet ef migrations add "$2"
        ;;
    
    "apply")
        echo "Applying all pending migrations..."
        dotnet ef database update
        ;;
    
    "rollback")
        if [ -z "$2" ]; then
            echo "Rolling back to previous migration..."
            dotnet ef database update 0
        else
            echo "Rolling back to migration: $2"
            dotnet ef database update "$2"
        fi
        ;;
    
    "remove")
        echo "Removing last migration..."
        dotnet ef migrations remove
        ;;
    
    "list")
        echo "Listing all migrations..."
        dotnet ef migrations list
        ;;
    
    "script")
        if [ -z "$2" ]; then
            echo "Generating SQL script for all migrations..."
            dotnet ef migrations script -o migration.sql
        else
            echo "Generating SQL script from $2 to $3..."
            dotnet ef migrations script "$2" "$3" -o migration.sql
        fi
        echo "SQL script saved to migration.sql"
        ;;
    
    "drop")
        echo "⚠️  WARNING: This will drop the entire database!"
        read -p "Are you sure? (yes/no): " confirm
        if [ "$confirm" = "yes" ]; then
            dotnet ef database drop
        else
            echo "Cancelled"
        fi
        ;;
    
    *)
        echo "Database Migration Helper"
        echo ""
        echo "Usage: ./migration.sh <command> [options]"
        echo ""
        echo "Commands:"
        echo "  create <name>     Create a new migration"
        echo "  apply             Apply all pending migrations"
        echo "  rollback [name]   Rollback to previous or specified migration"
        echo "  remove            Remove the last migration"
        echo "  list              List all migrations"
        echo "  script [from to]  Generate SQL script"
        echo "  drop              Drop the database (WARNING)"
        echo ""
        echo "Examples:"
        echo "  ./migration.sh create InitialCreate"
        echo "  ./migration.sh apply"
        echo "  ./migration.sh rollback"
        echo "  ./migration.sh list"
        echo "  ./migration.sh script"
        ;;
esac
