# WhatsApp Business API - ASP.NET Core with GraphQL

A complete ASP.NET Core implementation of WhatsApp Business API with GraphQL interface.

## Features

- ✅ **GraphQL API** - Full GraphQL schema for all WhatsApp operations
- ✅ **Token Management** - Automatic token caching and refresh
- ✅ **Template Messages** - Send WhatsApp template messages
- ✅ **Text Messages** - Send plain text messages
- ✅ **Webhook Support** - Handle incoming messages and status updates
- ✅ **Type Safety** - Strongly typed C# models

## Project Structure

```
whatsapp/
├── Constants/
│   └── TimeConstants.cs          # Time-related constants
├── Controllers/
│   └── WebhookController.cs      # REST webhook endpoint (required by Meta)
├── GraphQL/
│   └── WhatsAppSchema.cs         # GraphQL queries and mutations
├── Models/
│   └── WhatsAppModels.cs         # Data models for API responses
├── Services/
│   ├── TokenManagerService.cs    # Token management with caching
│   └── WhatsAppService.cs        # Core WhatsApp API integration
├── Program.cs                     # App configuration
├── appsettings.json              # Configuration (production)
└── appsettings.Development.json  # Configuration (development)
```

## Configuration

Add your WhatsApp credentials to `appsettings.json` or `appsettings.Development.json`:

```json
{
  "WHATSAPP_VERIFY_TOKEN": "your_verify_token",
  "WHATSAPP_PHONE_NUMBER_ID": "your_phone_number_id",
  "WHATSAPP_BUSINESS_ACCOUNT_ID": "your_business_account_id",
  "WHATSAPP_API_VERSION": "v18.0",
  
  "FACEBOOK_APP_ID": "your_app_id",
  "FACEBOOK_APP_SECRET": "your_app_secret",
  "FACEBOOK_SYSTEM_USER_ID": "your_system_user_id",
  "FACEBOOK_SYSTEM_USER_TOKEN": "your_system_user_token"
}
```

## GraphQL Schema

### Queries

**List Templates**
```graphql
query {
  listTemplates {
    data {
      name
      language
      status
      category
      id
    }
  }
}
```

**Get Token Info** (for debugging)
```graphql
query {
  getTokenInfo
}
```

### Mutations

**Send Template Message**
```graphql
mutation {
  sendTemplateMessage(
    to: "1234567890"
    templateName: "hello_world"
    language: "en_US"
    parameters: ["John"]
  ) {
    messages {
      id
    }
    contacts {
      waId
    }
  }
}
```

**Send Text Message**
```graphql
mutation {
  sendTextMessage(
    to: "1234567890"
    text: "Hello from GraphQL!"
  ) {
    messages {
      id
    }
  }
}
```

**Clear Token Cache**
```graphql
mutation {
  clearTokenCache
}
```

## REST Endpoints

### Webhook (Required by Meta)

**GET** `/webhook` - Webhook verification
- Query params: `hub.mode`, `hub.verify_token`, `hub.challenge`

**POST** `/webhook` - Receive webhook events
- Body: WhatsApp webhook payload

## Running the Application

1. **Restore dependencies:**
```bash
dotnet restore
```

2. **Run the application:**
```bash
dotnet run
```

3. **Access GraphQL Playground:**
   - Development: `https://localhost:5001/graphql`
   - The Banana Cake Pop IDE will be available

## Usage Examples

### Using GraphQL (Recommended)

Access the GraphQL endpoint at `/graphql` and use the interactive IDE to explore the schema.

### Using HTTP Client

```bash
curl -X POST https://localhost:5001/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "mutation { sendTextMessage(to: \"1234567890\", text: \"Hello!\") { messages { id } } }"
  }'
```

### Webhook Setup

1. Configure your webhook URL in Meta Developer Console: `https://yourdomain.com/webhook`
2. Use your `WHATSAPP_VERIFY_TOKEN` for verification
3. The controller will automatically handle verification and incoming events

## Key Differences from NestJS

1. **Dependency Injection**: Uses ASP.NET Core's built-in DI
2. **Configuration**: Uses `IConfiguration` instead of `ConfigService`
3. **Logging**: Uses `ILogger<T>` instead of NestJS Logger
4. **HTTP Client**: Uses `IHttpClientFactory` instead of `fetch`
5. **Caching**: Uses `IMemoryCache` instead of Map
6. **GraphQL**: Uses HotChocolate instead of NestJS GraphQL

## Token Management

The service supports three token strategies (in order of priority):

1. **Static Token**: Set `WHATSAPP_ACCESS_TOKEN` in config
2. **Environment System User Token**: Set `FACEBOOK_SYSTEM_USER_TOKEN`
3. **Dynamic Token**: Automatically generated using Facebook App credentials

Tokens are cached with automatic expiration handling.

## Development Tips

- Use the GraphQL IDE (Banana Cake Pop) for testing queries/mutations
- Check logs for detailed request/response information
- Token cache can be cleared via GraphQL mutation for debugging
- Webhook events are logged with full payload details

## Dependencies

- **HotChocolate.AspNetCore** (14.1.0) - GraphQL server
- **Microsoft.Extensions.Http** (9.0.0) - HTTP client factory
- **Microsoft.Extensions.Caching.Memory** (9.0.0) - In-memory caching

## License

MIT
