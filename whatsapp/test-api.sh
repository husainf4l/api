#!/bin/bash

# WhatsApp GraphQL API Test Script
# Make sure the server is running first: dotnet run

echo "🧪 Testing WhatsApp GraphQL API"
echo "================================"
echo ""

# Test 1: List Templates
echo "📋 Test 1: List Templates"
curl -X POST http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "query { listTemplates { data { name language status } } }"
  }' | jq '.'

echo ""
echo ""

# Test 2: Get Token Info
echo "🔑 Test 2: Get Token Info"
curl -X POST http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "query { getTokenInfo }"
  }' | jq '.'

echo ""
echo ""

# Test 3: Send Text Message (CHANGE THE PHONE NUMBER!)
echo "💬 Test 3: Send Text Message"
echo "⚠️  This will send a real message. Uncomment to test."
# curl -X POST http://localhost:5000/graphql \
#   -H "Content-Type: application/json" \
#   -d '{
#     "query": "mutation { sendTextMessage(to: \"YOUR_PHONE_NUMBER\", text: \"Test from GraphQL!\") { messages { id } } }"
#   }' | jq '.'

echo ""
echo ""

# Test 4: Webhook Verification
echo "🔗 Test 4: Webhook Verification"
curl -X GET "http://localhost:5000/webhook?hub.mode=subscribe&hub.verify_token=tt55oo77&hub.challenge=test123"

echo ""
echo ""

# Test 5: GraphQL Schema Introspection
echo "📚 Test 5: GraphQL Schema Introspection"
curl -X POST http://localhost:5000/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "{ __schema { queryType { name fields { name description } } mutationType { name fields { name description } } } }"
  }' | jq '.'

echo ""
echo ""
echo "✅ Tests Complete!"
echo ""
echo "📖 To test interactively, open: http://localhost:5000/graphql"
