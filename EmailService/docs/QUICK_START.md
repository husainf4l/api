# EmailService - Quick Integration Guide

## 🚀 Quick Start

Your EmailService is running with a modern GraphQL API and ready to accept email requests from any application!

**GraphQL Endpoint**: `http://localhost:5189/graphql`
**GraphQL Playground**: `http://localhost:5189/ui/playground` (development only)
**Authentication**: Required via `X-API-Key` header for mutations

---

## 📨 How to Send an Email

### Using cURL (Command Line)

```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-email-service-key-2024" \
  -d '{
    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
    "variables": {
      "input": {
        "to": "recipient@example.com",
        "subject": "Your Subject",
        "body": "Your message here",
        "isHtml": false
      }
    }
  }'
```

### Using Postman
1. Create a new POST request
2. URL: `http://localhost:5189/graphql`
3. Headers: 
   - `Content-Type: application/json`
   - `X-API-Key: dev-email-service-key-2024`
4. Body (raw JSON):
```json
{
  "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
  "variables": {
    "input": {
      "to": "recipient@example.com",
      "subject": "Your Subject",
      "body": "Your message here",
      "isHtml": false
    }
  }
}
```

### Using GraphQL Playground
1. Open `http://localhost:5189/ui/playground` in your browser
2. Add header: `{ "X-API-Key": "dev-email-service-key-2024" }`
3. Run this mutation:
```graphql
mutation SendEmail($input: SendEmailInput!) {
  sendEmail(input: $input) {
    success
    message
    messageId
  }
}
```
4. Variables panel:
```json
{
  "input": {
    "to": "recipient@example.com",
    "subject": "Your Subject",
    "body": "Your message here",
    "isHtml": false
  }
}
```

---

## 🔧 Integration Examples

### JavaScript / Node.js
```javascript
const response = await fetch('http://localhost:5189/graphql', {
  method: 'POST',
  headers: { 
    'Content-Type': 'application/json',
    'X-API-Key': 'dev-email-service-key-2024'
  },
  body: JSON.stringify({
    query: `mutation SendEmail($input: SendEmailInput!) {
      sendEmail(input: $input) {
        success
        message
        messageId
      }
    }`,
    variables: {
      input: {
        to: 'recipient@example.com',
        subject: 'Hello',
        body: 'Your message',
        isHtml: false
      }
    }
  })
});
const result = await response.json();
console.log(result);
```

### Python
```python
import requests

response = requests.post('http://localhost:5189/graphql', 
    headers={'X-API-Key': 'dev-email-service-key-2024'},
    json={
        'query': '''
            mutation SendEmail($input: SendEmailInput!) {
                sendEmail(input: $input) {
                    success
                    message
                    messageId
                }
            }
        ''',
        'variables': {
            'input': {
                'to': 'recipient@example.com',
                'subject': 'Hello',
                'body': 'Your message',
                'isHtml': False
            }
        }
    })
print(response.json())
```

### C# / .NET
```csharp
using System.Net.Http;
using System.Text.Json;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "dev-email-service-key-2024");

var query = new {
    query = @"mutation SendEmail($input: SendEmailInput!) {
        sendEmail(input: $input) {
            success
            message
            messageId
        }
    }",
    variables = new {
        input = new {
            to = "recipient@example.com",
            subject = "Hello",
            body = "Your message",
            isHtml = false
        }
    }
};

var json = JsonSerializer.Serialize(query);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var response = await client.PostAsync("http://localhost:5189/graphql", content);
var result = await response.Content.ReadAsStringAsync();
Console.WriteLine(result);
```

### Java
```java
import org.apache.http.client.methods.HttpPost;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClients;

CloseableHttpClient client = HttpClients.createDefault();
HttpPost httpPost = new HttpPost("http://localhost:5189/graphql");

String json = """
{
  "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
  "variables": {
    "input": {
      "to": "recipient@example.com",
      "subject": "Hello",
      "body": "Message",
      "isHtml": false
    }
  }
}
""";

httpPost.setEntity(new StringEntity(json));
httpPost.setHeader("Content-Type", "application/json");
httpPost.setHeader("X-API-Key", "dev-email-service-key-2024");
client.execute(httpPost);
```

### PHP
```php
$data = [
    'query' => 'mutation SendEmail($input: SendEmailInput!) { 
        sendEmail(input: $input) { 
            success message messageId 
        } 
    }',
    'variables' => [
        'input' => [
            'to' => 'recipient@example.com',
            'subject' => 'Hello',
            'body' => 'Your message',
            'isHtml' => false
        ]
    ]
];

$ch = curl_init('http://localhost:5189/graphql');
curl_setopt($ch, CURLOPT_HTTPHEADER, [
    'Content-Type: application/json',
    'X-API-Key: dev-email-service-key-2024'
]);
curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
$result = curl_exec($ch);
```

---

## ✅ Current Status

- **Service**: ✅ Running on `http://localhost:5189/graphql`
- **AWS Region**: me-central-1 (UAE)
- **Sender Email**: admin@rolevate.com
- **Status**: Ready for email delivery

---

## 📋 GraphQL Schema

### Queries

#### Health Check (No Auth Required)
```graphql
query {
  health {
    status
    service
    timestamp
  }
}
```

### Mutations

#### Send Email (Auth Required)
```graphql
mutation SendEmail($input: SendEmailInput!) {
  sendEmail(input: $input) {
    success
    message
    messageId
  }
}
```

**Input Fields:**
| Field | Type | Required | Example |
|-------|------|----------|---------|
| `to` | String! | Yes | "recipient@example.com" |
| `cc` | String | No | "cc@example.com" |
| `bcc` | String | No | "bcc@example.com" |
| `subject` | String! | Yes | "Hello" |
| `body` | String! | Yes | "Your message" |
| `isHtml` | Boolean | No | true/false |

#### Generate HTML Template (Auth Required)
```graphql
mutation GenerateHtml($input: GenerateHtmlInput!) {
  generateHtml(input: $input) {
    success
    html
    message
  }
}
```

**Input Fields:**
| Field | Type | Required | Example |
|-------|------|----------|---------|
| `title` | String! | Yes | "Welcome Email" |
| `heading` | String! | Yes | "Welcome!" |
| `message` | String! | Yes | "Thank you for joining" |
| `buttonText` | String | No | "Get Started" |
| `buttonUrl` | String | No | "https://example.com" |
| `additionalInfo` | String | No | "Contact us..." |
| `footerText` | String | No | "Best regards" |

---

## 🎯 Response Examples

### Success
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

### Error
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

---

## ⚠️ Important: Email Verification

**Current Limitation**: Due to AWS SES Sandbox mode, emails can only be sent to these verified addresses:
- `husain.f4l@gmail.com`
- `admin@rolevate.com`
- `info@aqlaan.com`
- Any email under `@aqlaan.com` or `@rolevate.com` domains

**To send to any email address**, request AWS SES Production Access via AWS Support Console.

---

## 🧪 Test It Now

### Health Check
```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ health { status service timestamp } }"}'
```

### Send Test Email
```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-email-service-key-2024" \
  -d '{
    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
    "variables": {
      "input": {
        "to": "husain.f4l@gmail.com",
        "subject": "Test",
        "body": "Hello World",
        "isHtml": false
      }
    }
  }'
```

---

## 📚 Full Documentation

See `EMAIL_API_DOCUMENTATION.md` for complete GraphQL API reference and more examples.
