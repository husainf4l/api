#!/bin/bash

# Test AuthService API
BASE_URL="http://localhost:5067"

echo "🔍 Testing AuthService API..."
echo ""

# Test Health Check
echo "1️⃣  Testing Health Check..."
curl -s $BASE_URL/health | jq .
echo ""
echo ""

# Test Register
echo "2️⃣  Testing User Registration..."
REGISTER_RESPONSE=$(curl -s -X POST $BASE_URL/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@example.com",
    "password": "SecurePass123!@#",
    "firstName": "John",
    "lastName": "Doe"
  }')
echo $REGISTER_RESPONSE | jq .
echo ""
echo ""

# Extract tokens
ACCESS_TOKEN=$(echo $REGISTER_RESPONSE | jq -r '.accessToken // empty')
REFRESH_TOKEN=$(echo $REGISTER_RESPONSE | jq -r '.refreshToken // empty')

if [ -z "$ACCESS_TOKEN" ]; then
  echo "❌ Registration failed or user already exists. Trying login..."
  echo ""
  
  # Test Login
  echo "3️⃣  Testing User Login..."
  LOGIN_RESPONSE=$(curl -s -X POST $BASE_URL/api/auth/login \
    -H "Content-Type: application/json" \
    -d '{
      "email": "testuser@example.com",
      "password": "SecurePass123!@#"
    }')
  echo $LOGIN_RESPONSE | jq .
  echo ""
  echo ""
  
  ACCESS_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.accessToken // empty')
  REFRESH_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.refreshToken // empty')
fi

if [ -n "$ACCESS_TOKEN" ]; then
  # Test Get Current User
  echo "4️⃣  Testing Get Current User (with token)..."
  curl -s -X GET $BASE_URL/api/auth/me \
    -H "Authorization: Bearer $ACCESS_TOKEN" | jq .
  echo ""
  echo ""
  
  # Test Validate Token
  echo "5️⃣  Testing Token Validation..."
  curl -s -X POST $BASE_URL/api/auth/validate \
    -H "Content-Type: application/json" \
    -d "{\"accessToken\": \"$ACCESS_TOKEN\"}" | jq .
  echo ""
  echo ""
  
  if [ -n "$REFRESH_TOKEN" ]; then
    # Test Refresh Token
    echo "6️⃣  Testing Token Refresh..."
    curl -s -X POST $BASE_URL/api/auth/refresh \
      -H "Content-Type: application/json" \
      -d "{\"refreshToken\": \"$REFRESH_TOKEN\"}" | jq .
    echo ""
    echo ""
  fi
else
  echo "❌ Could not obtain access token"
fi

echo "✅ API Testing Complete!"
