# EmailService GraphQL API Documentation

## Overview
The EmailService provides a GraphQL API for sending emails via AWS SES in the UAE region (me-central-1).

**GraphQL Endpoint**: `http://localhost:5189/graphql`

**GraphQL Playground**: `http://localhost:5189/ui/playground` (in development)

---

## Authentication
All mutations require API key authentication via the `X-API-Key` header.

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{...}'
```

---

## Schema

### Queries

#### Health Check
```graphql
query {
  health {
    status
    service
    timestamp
  }
}
```

**Response**:
```json
{
  "data": {
    "health": {
      "status": "healthy",
      "service": "EmailService",
      "timestamp": "2025-11-17T12:00:00.0000000Z"
    }
  }
}
```

### Mutations

#### Send Email
```graphql
mutation SendEmail($input: SendEmailInput!) {
  sendEmail(input: $input) {
    success
    message
    messageId
  }
}
```

**Variables**:
```json
{
  "input": {
    "to": "recipient@example.com",
    "cc": "optional@example.com",
    "bcc": "optional@example.com",
    "subject": "Email Subject",
    "body": "Email body or HTML content",
    "isHtml": false
  }
}
```

**Parameters**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `to` | String! | Yes | Recipient email address |
| `cc` | String | No | Carbon copy email address |
| `bcc` | String | No | Blind carbon copy email address |
| `subject` | String! | Yes | Email subject line |
| `body` | String! | Yes | Email body (text or HTML) |
| `isHtml` | Boolean | No | Set to `true` for HTML content (default: `false`) |

**Success Response**:
```json
{
  "data": {
    "sendEmail": {
      "success": true,
      "message": "Email sent successfully",
      "messageId": "011d019a91fc0b6b-368d8322-5038-4004-9d57-998ba9c9533c-000000"
    }
  }
}
```

#### Generate HTML Email Template
```graphql
mutation GenerateHtml($input: GenerateHtmlInput!) {
  generateHtml(input: $input) {
    success
    html
    message
  }
}
```

**Variables**:
```json
{
  "input": {
    "title": "Welcome Email",
    "heading": "Welcome to Our Service!",
    "message": "Thank you for joining us.",
    "buttonText": "Get Started",
    "buttonUrl": "https://example.com",
    "additionalInfo": "If you have any questions, please contact us.",
    "footerText": "Best regards, Our Team"
  }
}
```

**Parameters**:

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `title` | String! | Yes | Email title (appears in subject/browser tab) |
| `heading` | String! | Yes | Main heading of the email |
| `message` | String! | Yes | Main message content |
| `buttonText` | String | No | Call-to-action button text |
| `buttonUrl` | String | No | Call-to-action button URL |
| `additionalInfo` | String | No | Additional information section |
| `footerText` | String | No | Footer text (default: "Thank you for using our service") |

---

## Usage Examples

### Example 1: Send Plain Text Email using GraphQL

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{
    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
    "variables": {
      "input": {
        "to": "recipient@example.com",
        "subject": "Hello World",
        "body": "This is a test email",
        "isHtml": false
      }
    }
  }'
```

### Example 2: Send HTML Email using GraphQL

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{
    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
    "variables": {
      "input": {
        "to": "recipient@example.com",
        "subject": "Welcome",
        "body": "<h1>Welcome!</h1><p>This is an HTML email</p>",
        "isHtml": true
      }
    }
  }'
```

### Example 3: Send Email with CC and BCC

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{
    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
    "variables": {
      "input": {
        "to": "recipient@example.com",
        "cc": "cc@example.com",
        "bcc": "bcc@example.com",
        "subject": "Important Update",
        "body": "Email body content",
        "isHtml": false
      }
    }
  }'
```

### Example 4: Generate HTML Email Template

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key-here" \
  -d '{
    "query": "mutation GenerateHtml($input: GenerateHtmlInput!) { generateHtml(input: $input) { success html message } }",
    "variables": {
      "input": {
        "title": "Welcome Email",
        "heading": "Welcome to Our Service!",
        "message": "Thank you for joining us.",
        "buttonText": "Get Started",
        "buttonUrl": "https://example.com",
        "additionalInfo": "If you have any questions, please contact us.",
        "footerText": "Best regards, Our Team"
      }
    }
  }'
```

### Example 5: Health Check

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "{ health { status service timestamp } }"
  }'
```

### Example 6: C# / .NET Application

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

var client = new HttpClient();
var url = "http://localhost:5189/graphql";

var query = new
{
    query = @"mutation SendEmail($input: SendEmailInput!) {
        sendEmail(input: $input) {
            success
            message
            messageId
        }
    }",
    variables = new
    {
        input = new
        {
            to = "recipient@example.com",
            subject = "Test Email",
            body = "Hello from C# application",
            isHtml = false
        }
    }
};

var json = JsonSerializer.Serialize(query);
var content = new StringContent(json, Encoding.UTF8, "application/json");
content.Headers.Add("X-API-Key", "your-api-key-here");

var response = await client.PostAsync(url, content);
var result = await response.Content.ReadAsStringAsync();
Console.WriteLine(result);
```

### Example 7: JavaScript / Node.js with GraphQL Client

```javascript
const { GraphQLClient, gql } = require('graphql-request');

const client = new GraphQLClient('http://localhost:5189/graphql', {
    headers: {
        'X-API-Key': 'your-api-key-here'
    }
});

const mutation = gql`
    mutation SendEmail($input: SendEmailInput!) {
        sendEmail(input: $input) {
            success
            message
            messageId
        }
    }
`;

const variables = {
    input: {
        to: 'recipient@example.com',
        subject: 'Test Email from Node.js',
        body: 'Hello from Node.js application',
        isHtml: false
    }
};

client.request(mutation, variables)
    .then(data => console.log(data))
    .catch(error => console.error(error));
```

---

## Configuration

### AWS Credentials
The service uses AWS credentials from:
1. Environment variables: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`
2. Or from `.env` file in the EmailService directory

### API Key
Set the API key in `appsettings.json`:
```json
{
  "ApiSettings": {
    "ApiKey": "your-secure-api-key-here"
  }
}
```

### Current Configuration
- **Sender Email**: `admin@rolevate.com`
- **AWS Region**: `me-central-1` (UAE)
- **GraphQL URL**: `http://localhost:5189/graphql`
- **Playground URL**: `http://localhost:5189/ui/playground` (development only)

---

## Important Notes

### Verified Email Addresses (Current)
Due to AWS SES Sandbox limitations, emails can only be sent to these verified addresses:
- `husain.f4l@gmail.com`
- `admin@rolevate.com`
- `info@aqlaan.com`
- `test@aqlaan.com`
- `test@rolevate.com`

### To Send to Any Email Address
Request **AWS SES Production Access** - Submit a support ticket to AWS with:
- Service: AWS Support
- Category: Service Limit Increase
- Limit Type: SES Sending Limits
- Region: me-central-1
- Request: Production Access removal from Sandbox

---

## Error Handling

GraphQL errors are returned in the `errors` field:

```json
{
  "errors": [
    {
      "message": "Access denied",
      "extensions": {
        "code": "AUTH_NOT_AUTHORIZED"
      }
    }
  ],
  "data": null
}
```

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "Access denied" | Missing or invalid API key | Provide valid X-API-Key header |
| "Email address is not verified" | Recipient email not verified in SES | Verify email or request production access |
| "Failed to send email" | AWS SES error | Check AWS credentials and region |
| "Validation error" | Invalid input data | Check field requirements and formats |

---

## Rate Limits (Current - Sandbox)
- **Daily Limit**: 200 emails per 24 hours
- **Per-Second Rate**: 1 email per second

---

## Development Tools

### GraphQL Playground
In development mode, visit `http://localhost:5189/ui/playground` to:
- Explore the schema
- Test queries and mutations
- View documentation
- Debug requests

### Schema Introspection
Query the schema programmatically:

```graphql
query {
  __schema {
    types {
      name
      kind
      description
    }
  }
}
```

---

## Support

For issues or questions, check:
1. Service logs
2. GraphQL error responses
3. AWS SES verification status
4. Network connectivity to localhost:5189/graphql
