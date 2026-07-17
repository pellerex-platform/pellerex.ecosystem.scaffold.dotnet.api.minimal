#!/bin/bash

echo "🚀 Running API locally with dotnet..."

# Change to the project root directory
cd "$(dirname "$0")/.." || exit 1

# Check if secrets exist
SECRETS_FILE=~/.microsoft/usersecrets/<marketplace-product-id>/secrets.json
if [ ! -f "$SECRETS_FILE" ]; then
    echo "❌ Secrets not found. Run setup first:"
    echo "   ./start/setup-secrets.sh"
    exit 1
fi

echo "✅ Using secrets from: $SECRETS_FILE"
echo "🔄 Starting API with hot reload..."
echo "🌐 API will be available at: http://localhost:<port-number>"
echo ""

# Run with dotnet watch for hot reload
dotnet watch --project api/RepoUniqueIdentifier.csproj run
