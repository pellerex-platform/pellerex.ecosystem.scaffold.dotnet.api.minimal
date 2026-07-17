#!/bin/bash

echo "🐳 Running API in Docker container with volume-mounted secrets..."

# Configuration
CONTAINER_NAME="minimal-app"
IMAGE_NAME="minimal"
HOST_PORT="<port-number>"
CONTAINER_PORT="<port-number>"
SECRETS_DIR=~/.microsoft/usersecrets/<marketplace-product-id>

# Change to the project root directory
cd "$(dirname "$0")/.." || exit 1

# Check if secrets exist
SECRETS_FILE=$SECRETS_DIR/secrets.json
if [ ! -f "$SECRETS_FILE" ]; then
    echo "❌ Secrets not found. Run setup first:"
    echo "   ./start/setup-secrets.sh"
    exit 1
fi

# Build Docker image
echo "🔨 Building Docker image..."
docker build -t $IMAGE_NAME . -q

if [ $? -ne 0 ]; then
    echo "❌ Docker build failed"
    exit 1
fi

# Stop and remove existing container
echo "🧹 Cleaning up existing container..."
docker stop $CONTAINER_NAME 2>/dev/null
docker rm $CONTAINER_NAME 2>/dev/null

echo "🚀 Starting container with volume-mounted secrets..."
echo "📦 Container: $CONTAINER_NAME"
echo "🌐 Port: $HOST_PORT:$CONTAINER_PORT"
echo "🔐 Secrets volume: $SECRETS_DIR (read-only)"

# Run container with volume-mounted user secrets
docker run -d \
    --name $CONTAINER_NAME \
    -p $HOST_PORT:$CONTAINER_PORT \
    -e ASPNETCORE_ENVIRONMENT=Development \
    -v $SECRETS_DIR:/root/.microsoft/usersecrets/<marketplace-product-id>:ro \
    $IMAGE_NAME

if [ $? -eq 0 ]; then
    echo "✅ Container started successfully!"
    
    # Wait a moment for startup
    echo "⏳ Waiting for startup..."
    sleep 5
    
    # Test health endpoint
    if curl -f -s http://localhost:$HOST_PORT/health/live >/dev/null 2>&1; then
        echo "💚 API is healthy and running!"
    else
        echo "⚠️  API is starting up... try again in a few seconds"
    fi
    
    echo ""
    echo "🎯 Available endpoints:"
    echo "  🌐 API: http://localhost:$HOST_PORT"
    echo "  📊 Swagger: http://localhost:$HOST_PORT/swagger"
    echo "  💓 Health: http://localhost:$HOST_PORT/health/live"
    echo ""
    echo "📋 Useful commands:"
    echo "  🔍 View logs: docker logs $CONTAINER_NAME"
    echo "  📄 Follow logs: docker logs -f $CONTAINER_NAME"
    echo "  🛑 Stop: docker stop $CONTAINER_NAME"
    echo "  🗑️  Remove: docker rm $CONTAINER_NAME"
    echo "  🔄 Restart: docker restart $CONTAINER_NAME"
else
    echo "❌ Failed to start container"
    exit 1
fi
