#!/bin/bash

echo "🔐 Setting up development secrets..."

# Change to the project root directory
cd "$(dirname "$0")/.." || exit 1

# Create user secrets directory
SECRETS_DIR=~/.microsoft/usersecrets/<marketplace-product-id>
if [ ! -d "$SECRETS_DIR" ]; then
    echo "📁 Creating user secrets directory..."
    mkdir -p "$SECRETS_DIR"
fi

# Create default secrets.json if it doesn't exist
SECRETS_FILE=$SECRETS_DIR/secrets.json
if [ ! -f "$SECRETS_FILE" ]; then
    echo "📝 Creating default secrets.json..."
    cat > "$SECRETS_FILE" << 'EOF'
{
  "DbConnectionString": "Server=.;Database=MinimalDev;TTrustServerCertificate=True; Trusted_Connection=False; MultipleActiveResultSets=True; User Id=sa; Password=pa55w0rd!"
}
EOF
    echo "✅ Created $SECRETS_FILE"
    echo "💡 Edit this file with your actual database connection string"
else
    echo "✅ Secrets file already exists: $SECRETS_FILE"
fi

echo ""
echo "🎯 Next steps:"
echo "  📝 Edit secrets: $SECRETS_FILE"
echo "  💡 For Docker: Change 'Server=.' to 'Server=host.docker.internal'"
echo "  🏃 Run locally: ./start/run-local.sh"
echo "  🐳 Run Docker: ./start/run-docker.sh"
