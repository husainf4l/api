#!/bin/bash

# Auth Service Deployment Script
# This script helps deploy the Auth Service to various environments

set -e

# Configuration
APP_NAME="authservice"
DOCKER_IMAGE="authservice:latest"
DOCKER_REGISTRY="${DOCKER_REGISTRY:-localhost:5000}"
ENVIRONMENT="${ENVIRONMENT:-development}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_dependencies() {
    log_info "Checking dependencies..."

    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker first."
        exit 1
    fi

    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is not installed. Please install .NET 10.0 SDK first."
        exit 1
    fi

    log_info "Dependencies check passed."
}

build_application() {
    log_info "Building application..."

    # Restore dependencies
    dotnet restore

    # Build application
    dotnet build --configuration Release --no-restore

    # Run tests if they exist
    if [ -d "tests" ]; then
        log_info "Running tests..."
        dotnet test --configuration Release --no-build --verbosity normal
    fi

    log_info "Application build completed."
}

build_docker_image() {
    log_info "Building Docker image..."

    # Build Docker image
    docker build -t "$DOCKER_IMAGE" .

    # Tag for registry if specified
    if [ "$DOCKER_REGISTRY" != "localhost:5000" ]; then
        docker tag "$DOCKER_IMAGE" "$DOCKER_REGISTRY/$APP_NAME:$ENVIRONMENT"
        docker tag "$DOCKER_IMAGE" "$DOCKER_REGISTRY/$APP_NAME:latest"
    fi

    log_info "Docker image built successfully."
}

push_docker_image() {
    if [ "$DOCKER_REGISTRY" != "localhost:5000" ]; then
        log_info "Pushing Docker image to registry..."

        docker push "$DOCKER_REGISTRY/$APP_NAME:$ENVIRONMENT"
        docker push "$DOCKER_REGISTRY/$APP_NAME:latest"

        log_info "Docker image pushed successfully."
    else
        log_warn "Skipping push to registry (using local registry)."
    fi
}

deploy_local() {
    log_info "Deploying locally with Docker Compose..."

    # Stop existing containers
    docker-compose down || true

    # Start services
    docker-compose up -d

    # Wait for services to be ready
    log_info "Waiting for services to be ready..."
    sleep 30

    # Check health
    if curl -f http://localhost:8080/health &>/dev/null; then
        log_info "Deployment successful! Service is healthy."
        log_info "Access the application at: http://localhost:8080"
        log_info "Swagger UI: http://localhost:8080/swagger"
    else
        log_error "Health check failed. Please check the logs."
        docker-compose logs
        exit 1
    fi
}

show_usage() {
    cat << EOF
Auth Service Deployment Script

Usage: $0 [OPTIONS] COMMAND

Commands:
  build          Build the application and Docker image
  deploy-local   Deploy locally using Docker Compose
  deploy-prod    Deploy to production (requires DOCKER_REGISTRY env var)
  test           Run tests only
  clean          Clean build artifacts

Options:
  -e, --env ENV     Set environment (development, staging, production) [default: development]
  -r, --registry REG Set Docker registry URL [default: localhost:5000]
  -h, --help        Show this help message

Environment Variables:
  ENVIRONMENT         Deployment environment (development, staging, production)
  DOCKER_REGISTRY     Docker registry URL for production deployments

Examples:
  $0 build
  $0 -e production deploy-prod
  $0 -r myregistry.com deploy-prod
  ENVIRONMENT=staging $0 deploy-prod

EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--env)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -r|--registry)
            DOCKER_REGISTRY="$2"
            shift 2
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            break
            ;;
    esac
done

COMMAND=${1:-build}

# Main execution
case $COMMAND in
    build)
        check_dependencies
        build_application
        build_docker_image
        ;;
    deploy-local)
        check_dependencies
        build_application
        build_docker_image
        deploy_local
        ;;
    deploy-prod)
        if [ "$DOCKER_REGISTRY" = "localhost:5000" ]; then
            log_error "DOCKER_REGISTRY environment variable must be set for production deployment."
            exit 1
        fi
        check_dependencies
        build_application
        build_docker_image
        push_docker_image
        log_info "Production image pushed. Use your deployment tool (Kubernetes, Docker Swarm, etc.) to deploy."
        ;;
    test)
        log_info "Running tests..."
        dotnet test --configuration Release --verbosity normal
        ;;
    clean)
        log_info "Cleaning build artifacts..."
        dotnet clean
        docker rmi "$DOCKER_IMAGE" 2>/dev/null || true
        docker system prune -f
        ;;
    *)
        log_error "Unknown command: $COMMAND"
        show_usage
        exit 1
        ;;
esac

log_info "Deployment script completed successfully!"
