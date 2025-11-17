# SMS Service - GraphQL API

A production-ready GraphQL microservice for sending SMS messages via JOSMS Gateway with API key authentication and comprehensive message tracking.

## 🚀 Features

- **GraphQL API**: Modern, flexible API with introspection
- **JOSMS Gateway Integration**: Send SMS using Jordan SMS (JOSMS) platform
- **API Key Authentication**: Secure fields with X-API-Key header validation
- **Multiple SMS Types**:
  - OTP Messages (One-Time Passwords)
  - General Messages (Announcements, notifications)
  - Bulk Messages (Up to 120 recipients)
- **Message Tracking**: PostgreSQL database with full audit trail
- **Application Tracking**: Track which app sent each message
- **Balance Checking**: Query remaining SMS credits
- **Phone Number Validation**: Automatic normalization for Jordanian numbers (962)

## 📋 Prerequisites

- .NET 10.0 SDK
- JOSMS Account credentials
- PostgreSQL Database (for message history)
- HotChocolate GraphQL (v15.1.11)

## 🏗️ Project Structure

```
SmsService/
├── GraphQL/
│   ├── Queries/
│   │   └── SmsQueries.cs         # GraphQL queries
│   ├── Mutations/
│   │   └── SmsMutations.cs       # GraphQL mutations
│   └── Middleware/
│       └── ApiKeyAuthorizationMiddleware.cs
├── Services/
│   └── JosmsSmsService.cs        # JOSMS integration
├── Repositories/
│   └── SmsMessageRepository.cs   # Database operations
├── Models/
│   ├── SmsMessage.cs             # Message entity
│   └── ServiceResponses.cs       # Response models
├── appsettings.json              # Configuration
└── Program.cs                    # Application entry point
```

## 🚀 Quick Start

### 1. Configure Database

```bash
# Database connection in appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=149.200.251.12;Port=5432;Database=aqlaansms;Username=husain;Password=tt55oo77"
  }
}
```

### 2. Configure JOSMS

```json
{
  "JosmsSettings": {
    "BaseUrl": "https://www.josms.net",
    "AccName": "margogroup",
    "AccPassword": "your-password",
    "DefaultSenderId": "MargoGroup"
  },
  "ApiSettings": {
    "ApiKey": "your-secure-api-key"
  }
}
```

### 3. Run the Service

```bash
dotnet run --urls "http://localhost:5103"
```

## 📡 GraphQL Endpoint

**URL:** `http://localhost:5103/graphql`

**Method:** POST

**Headers:**
- `Content-Type: application/json`
- `X-API-Key: your-api-key` (required for protected fields)
- `X-App-Name: YourApp` (optional, for tracking)
- `X-App-Version: 1.0.0` (optional, for tracking)

## 📚 GraphQL Schema

### Queries

#### Health Check (Public)
```graphql
{
  health
}
```

**Response:**
```json
{
  "data": {
    "health": "SMS service is healthy"
  }
}
```

#### Available Senders (Public)
```graphql
{
  senders
}
```

**Response:**
```json
{
  "data": {
    "senders": ["MargoGroup"]
  }
}
```

#### Balance (Requires Auth)
```graphql
{
  balance {
    success
    message
    balance
  }
}
```

**Response:**
```json
{
  "data": {
    "balance": {
      "success": true,
      "message": "Balance retrieved successfully",
      "balance": 6577.0
    }
  }
}
```

#### Message History (Requires Auth)
```graphql
{
  history(limit: 10) {
    success
    count
    messages {
      id
      recipient
      message
      senderId
      messageId
      status
      createdAt
      ipAddress
      userAgent
      appName
      appVersion
    }
  }
}
```

#### History by Recipient (Requires Auth)
```graphql
{
  historyByRecipient(recipient: "962771122003") {
    success
    count
    recipient
    messages {
      id
      message
      status
      createdAt
      appName
    }
  }
}
```

### Mutations

#### Send OTP SMS (Requires Auth)
```graphql
mutation {
  sendOtp(input: {
    to: "962771122003"
    message: "Your verification code: 123456"
    senderId: "MargoGroup"
  }) {
    success
    message
    rawResponse
  }
}
```

#### Send General SMS (Requires Auth)
```graphql
mutation {
  sendGeneral(input: {
    to: "962771122003"
    message: "Your appointment is tomorrow at 10 AM"
  }) {
    success
    message
    rawResponse
  }
}
```

#### Send Bulk SMS (Requires Auth)
```graphql
mutation {
  sendBulk(input: {
    to: ["962771122003", "962796026659", "962795790819"]
    message: "Important announcement for all"
    senderId: "MargoGroup"
  }) {
    success
    message
  }
}
```

## 🔧 Complete Examples

### Using curl

```bash
# Health check (no auth)
curl -X POST http://localhost:5103/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ health }"}'

# Send SMS with app tracking
curl -X POST http://localhost:5103/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-App-Name: CustomerPortal" \
  -H "X-App-Version: 2.1.5" \
  -d '{
    "query": "mutation { sendGeneral(input: { to: \"962771122003\", message: \"Hello from GraphQL\" }) { success message } }"
  }'

# Get message history
curl -X POST http://localhost:5103/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "query": "{ history(limit: 5) { count messages { recipient message appName createdAt } } }"
  }'
```

### Using JavaScript/TypeScript

```typescript
const query = `
  mutation SendSMS($input: SendSmsInput!) {
    sendGeneral(input: $input) {
      success
      message
      rawResponse
    }
  }
`;

const variables = {
  input: {
    to: "962771122003",
    message: "Your order has been shipped"
  }
};

const response = await fetch('http://localhost:5103/graphql', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-API-Key': 'your-api-key',
    'X-App-Name': 'ECommerceApp',
    'X-App-Version': '3.2.1'
  },
  body: JSON.stringify({ query, variables })
});

const result = await response.json();
console.log(result.data.sendGeneral);
```

### Using Python

```python
import requests

url = 'http://localhost:5103/graphql'
headers = {
    'Content-Type': 'application/json',
    'X-API-Key': 'your-api-key',
    'X-App-Name': 'PythonBackend',
    'X-App-Version': '1.5.0'
}

query = '''
mutation {
  sendGeneral(input: {
    to: "962771122003"
    message: "Alert from Python service"
  }) {
    success
    message
  }
}
'''

response = requests.post(url, json={'query': query}, headers=headers)
result = response.json()
print(result['data']['sendGeneral'])
```

### Using C# / .NET

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");
client.DefaultRequestHeaders.Add("X-App-Name", "DotNetService");
client.DefaultRequestHeaders.Add("X-App-Version", "2.0.0");

var query = new
{
    query = @"
        mutation {
          sendGeneral(input: {
            to: ""962771122003""
            message: ""Test from .NET""
          }) {
            success
            message
          }
        }"
};

var content = new StringContent(
    JsonSerializer.Serialize(query),
    Encoding.UTF8,
    "application/json"
);

var response = await client.PostAsync(
    "http://localhost:5103/graphql",
    content
);

var result = await response.Content.ReadAsStringAsync();
Console.WriteLine(result);
```

## 🔐 Authentication

### Public Fields (No Auth Required)
- `health` - Service health check
- `senders` - List available sender IDs

### Protected Fields (Requires X-API-Key)
- `balance` - Get SMS balance
- `history` - Get message history
- `historyByRecipient` - Get messages for specific recipient
- `sendOtp` - Send OTP message
- `sendGeneral` - Send general message
- `sendBulk` - Send bulk messages

### Error Responses

**Missing API Key:**
```json
{
  "errors": [{
    "message": "API Key is missing",
    "extensions": {
      "code": "AUTH_NOT_AUTHENTICATED"
    }
  }]
}
```

**Invalid API Key:**
```json
{
  "errors": [{
    "message": "Invalid API Key",
    "extensions": {
      "code": "AUTH_NOT_AUTHORIZED"
    }
  }]
}
```

## 📊 Application Tracking

Track which application sent each message using custom headers:

```bash
curl -X POST http://localhost:5103/graphql \
  -H "X-App-Name: MarketingCampaigns" \
  -H "X-App-Version: 2.5.8" \
  ...
```

Every message is saved with:
- App Name
- App Version
- IP Address
- User Agent
- API Key (masked)
- Timestamp

## 🧪 GraphQL Playground

For development, you can use:
- **Banana Cake Pop** (HotChocolate's built-in UI): Navigate to `/graphql` in browser
- **GraphQL Playground**
- **Insomnia**
- **Postman**

## 📈 Phone Number Formats

The service automatically normalizes Jordanian phone numbers:

**Supported formats:**
- `0771122003` → `962771122003`
- `962771122003` → `962771122003`
- `+962771122003` → `962771122003`
- `00962771122003` → `962771122003`

## ⚠️ Limitations

- Bulk SMS limited to 120 recipients per request
- Only Jordanian numbers (962) supported
- Phone numbers must be 12 digits after normalization

## 🔄 Migration from REST

If you're migrating from the REST API:

| REST Endpoint | GraphQL Query/Mutation |
|--------------|------------------------|
| `GET /api/sms/health` | `{ health }` |
| `GET /api/sms/senders` | `{ senders }` |
| `GET /api/sms/balance` | `{ balance { ... } }` |
| `GET /api/sms/history` | `{ history(limit: N) { ... } }` |
| `POST /api/sms/send/otp` | `mutation { sendOtp(input: {...}) { ... } }` |
| `POST /api/sms/send/general` | `mutation { sendGeneral(input: {...}) { ... } }` |
| `POST /api/sms/send/bulk` | `mutation { sendBulk(input: {...}) { ... } }` |

## 🛠️ Development

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Watch mode
dotnet watch run
```

## 📝 GraphQL Advantages

- **Single Endpoint**: All operations through `/graphql`
- **Type Safety**: Strong typing with schema validation
- **Introspection**: Self-documenting API
- **Flexible Queries**: Request exactly what you need
- **Real-time**: Ready for subscriptions (future feature)
- **Developer Experience**: Better tooling and IDE support

## 📄 License

Private - Internal Use Only

---

**Built with**: ASP.NET Core 10.0 | HotChocolate GraphQL 15.1 | JOSMS Gateway | PostgreSQL
