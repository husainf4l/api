# 🔐 EmailService - Secure GraphQL API with API Key Authentication# 🔐 EmailService - Secure API with API Key Authentication



## ✅ Security Implementation Complete## ✅ Security Implementation Complete



The EmailService GraphQL API requires API key authentication for all mutations (sendEmail, generateHtml)!The EmailService now requires API key authentication for all email sending requests!



------



## 🔑 API Key Configuration## 🔑 API Key Configuration



**Development API Key**: `dev-email-service-key-2024`**Development API Key**: `dev-email-service-key-2024`



**Location**: **Location**: 

- `appsettings.Development.json`- `appsettings.Development.json`

- `.env` file- `.env` file



**⚠️ Important**: Change the API key in production!**⚠️ Important**: Change the API key in production!



------



## 📨 How to Use GraphQL with API Key## 📨 How to Send Emails with API Key



### Required Header for Mutations### Required Header

All mutation requests to `/graphql` must include:All requests to `/api/email/send` must include:



``````

X-API-Key: dev-email-service-key-2024X-API-Key: dev-email-service-key-2024

``````



**Note**: Queries like `health` do NOT require authentication.---



---## 🧪 Usage Examples



## 🧪 Usage Examples### Example 1: cURL with API Key



### Example 1: cURL with API Key```bash

curl -X POST http://localhost:5189/api/email/send \

```bash  -H "Content-Type: application/json" \

curl -X POST http://localhost:5189/graphql \  -H "X-API-Key: dev-email-service-key-2024" \

  -H "Content-Type: application/json" \  -d '{

  -H "X-API-Key: dev-email-service-key-2024" \    "to": "recipient@example.com",

  -d '{    "subject": "Hello World",

    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",    "body": "Your message here",

    "variables": {    "isHtml": false

      "input": {  }'

        "to": "recipient@example.com",```

        "subject": "Hello World",

        "body": "Your message here",### Example 2: JavaScript / Node.js

        "isHtml": false

      }```javascript

    }const response = await fetch('http://localhost:5189/api/email/send', {

  }'  method: 'POST',

```  headers: {

    'Content-Type': 'application/json',

### Example 2: JavaScript / Node.js    'X-API-Key': 'dev-email-service-key-2024'

  },

```javascript  body: JSON.stringify({

const response = await fetch('http://localhost:5189/graphql', {    to: 'recipient@example.com',

  method: 'POST',    subject: 'Hello',

  headers: {    body: 'Your message',

    'Content-Type': 'application/json',    isHtml: false

    'X-API-Key': 'dev-email-service-key-2024'  })

  },});

  body: JSON.stringify({```

    query: `mutation SendEmail($input: SendEmailInput!) {

      sendEmail(input: $input) {### Example 3: Python

        success

        message```python

        messageIdimport requests

      }

    }`,headers = {

    variables: {    'Content-Type': 'application/json',

      input: {    'X-API-Key': 'dev-email-service-key-2024'

        to: 'recipient@example.com',}

        subject: 'Hello',

        body: 'Your message',data = {

        isHtml: false    'to': 'recipient@example.com',

      }    'subject': 'Hello',

    }    'body': 'Your message',

  })    'isHtml': False

});}



const result = await response.json();response = requests.post(

console.log(result);    'http://localhost:5189/api/email/send',

```    headers=headers,

    json=data

### Example 3: Python)

print(response.json())

```python```

import requests

### Example 4: C# / .NET

headers = {

    'Content-Type': 'application/json',```csharp

    'X-API-Key': 'dev-email-service-key-2024'var client = new HttpClient();

}client.DefaultRequestHeaders.Add("X-API-Key", "dev-email-service-key-2024");



data = {var json = JsonSerializer.Serialize(new {

    'query': '''    to = "recipient@example.com",

        mutation SendEmail($input: SendEmailInput!) {    subject = "Hello",

            sendEmail(input: $input) {    body = "Your message",

                success    isHtml = false

                message});

                messageId

            }var content = new StringContent(json, Encoding.UTF8, "application/json");

        }var response = await client.PostAsync("http://localhost:5189/api/email/send", content);

    ''',```

    'variables': {

        'input': {### Example 5: PHP

            'to': 'recipient@example.com',

            'subject': 'Hello',```php

            'body': 'Your message',$ch = curl_init('http://localhost:5189/api/email/send');

            'isHtml': False

        }$data = [

    }    'to' => 'recipient@example.com',

}    'subject' => 'Hello',

    'body' => 'Your message',

response = requests.post(    'isHtml' => false

    'http://localhost:5189/graphql',];

    headers=headers,

    json=data$headers = [

)    'Content-Type: application/json',

print(response.json())    'X-API-Key: dev-email-service-key-2024'

```];



### Example 4: C# / .NETcurl_setopt($ch, CURLOPT_HTTPHEADER, $headers);

curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));

```csharpcurl_setopt($ch, CURLOPT_RETURNTRANSFER, true);

var client = new HttpClient();

client.DefaultRequestHeaders.Add("X-API-Key", "dev-email-service-key-2024");$response = curl_exec($ch);

curl_close($ch);

var query = new {```

    query = @"mutation SendEmail($input: SendEmailInput!) {

        sendEmail(input: $input) {---

            success

            message## 🛡️ Security Responses

            messageId

        }### ✅ Success (with valid API key)

    }",```json

    variables = new {{

        input = new {  "success": true,

            to = "recipient@example.com",  "message": "Email sent successfully",

            subject = "Hello",  "messageId": "011d019a9219b66a-9730c1a3-7731-4ccc-a5c7-26909c94330d-000000"

            body = "Your message",}

            isHtml = false```

        }

    }### ❌ Missing API Key (401 Unauthorized)

};```json

{

var json = JsonSerializer.Serialize(query);  "error": "API Key is missing"

var content = new StringContent(json, Encoding.UTF8, "application/json");}

var response = await client.PostAsync("http://localhost:5189/graphql", content);```

var result = await response.Content.ReadAsStringAsync();

Console.WriteLine(result);### ❌ Invalid API Key (401 Unauthorized)

``````json

{

### Example 5: PHP  "error": "Invalid API Key"

}

```php```

$ch = curl_init('http://localhost:5189/graphql');

---

$data = [

    'query' => 'mutation SendEmail($input: SendEmailInput!) { ## 🚫 Endpoints WITHOUT API Key Requirement

        sendEmail(input: $input) { 

            success message messageId The following endpoints are public and don't require API key:

        } 

    }',- `GET /api/email/health` - Health check endpoint

    'variables' => [

        'input' => [```bash

            'to' => 'recipient@example.com',curl http://localhost:5189/api/email/health

            'subject' => 'Hello',# Response: {"status":"Email service is healthy"}

            'body' => 'Your message',```

            'isHtml' => false

        ]---

    ]

];## 🔐 Changing the API Key



$headers = [### For Development

    'Content-Type: application/json',Edit `appsettings.Development.json`:

    'X-API-Key: dev-email-service-key-2024'```json

];{

  "ApiSettings": {

curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);    "ApiKey": "your-new-api-key-here"

curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));  }

curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);}

```

$response = curl_exec($ch);

curl_close($ch);### For Production

```Edit `appsettings.json`:

```json

---{

  "ApiSettings": {

## 🛡️ Security Responses    "ApiKey": "your-secure-production-key"

  }

### ✅ Success (with valid API key)}

```json```

{

  "data": {### Using Environment Variables

    "sendEmail": {Set the environment variable:

      "success": true,```bash

      "message": "Email sent successfully",export ApiSettings__ApiKey="your-api-key"

      "messageId": "011d019a9219b66a-9730c1a3-7731-4ccc-a5c7-26909c94330d-000000"```

    }

  }---

}

```## 💡 Best Practices



### ❌ Missing or Invalid API Key (401 Unauthorized)1. **Use Strong API Keys**: Generate random, complex API keys for production

```json2. **Keep Keys Secret**: Never commit API keys to version control

{3. **Rotate Keys Regularly**: Change API keys periodically

  "errors": [4. **Use Environment Variables**: Store production keys in environment variables or secure vaults

    {5. **Monitor Usage**: Log all API key usage for security auditing

      "message": "Access denied",

      "extensions": {---

        "code": "AUTH_NOT_AUTHORIZED"

      }## 🔒 Example: Generate Secure API Key

    }

  ],```bash

  "data": null# Generate a secure random API key (Linux/Mac)

}openssl rand -base64 32

```

# Or using Python

---python3 -c "import secrets; print(secrets.token_urlsafe(32))"

```

## 🚫 Endpoints WITHOUT API Key Requirement

---

The following GraphQL queries are public and don't require an API key:

## ✅ Current Configuration

- **Health Check Query**

- **Service URL**: `http://localhost:5189`

```bash- **API Key Header**: `X-API-Key`

curl -X POST http://localhost:5189/graphql \- **Development Key**: `dev-email-service-key-2024`

  -H "Content-Type: application/json" \- **Protected Endpoints**: `/api/email/send`

  -d '{"query": "{ health { status service timestamp } }"}'- **Public Endpoints**: `/api/email/health`

```

---

Response:

```json## 📋 Quick Test

{

  "data": {```bash

    "health": {# Test without API key (should fail)

      "status": "healthy",curl -X POST http://localhost:5189/api/email/send \

      "service": "EmailService",  -H "Content-Type: application/json" \

      "timestamp": "2025-11-17T12:00:00.0000000Z"  -d '{"to":"test@example.com","subject":"Test","body":"Test","isHtml":false}'

    }

  }# Test with valid API key (should succeed)

}curl -X POST http://localhost:5189/api/email/send \

```  -H "Content-Type: application/json" \

  -H "X-API-Key: dev-email-service-key-2024" \

---  -d '{"to":"husain.f4l@gmail.com","subject":"Secure Test","body":"This works!","isHtml":false}'

```

## 🔐 Changing the API Key

---

### For Development

Edit `appsettings.Development.json`:**Your EmailService is now secured with API key authentication!** 🔐

```json
{
  "ApiSettings": {
    "ApiKey": "your-new-api-key-here"
  }
}
```

### For Production
Edit `appsettings.json`:
```json
{
  "ApiSettings": {
    "ApiKey": "your-secure-production-key"
  }
}
```

### Using Environment Variables
Set the environment variable:
```bash
export ApiSettings__ApiKey="your-api-key"
```

---

## 💡 Best Practices

1. **Use Strong API Keys**: Generate random, complex API keys for production
2. **Keep Keys Secret**: Never commit API keys to version control
3. **Rotate Keys Regularly**: Change API keys periodically
4. **Use Environment Variables**: Store production keys in environment variables or secure vaults
5. **Monitor Usage**: Log all API key usage for security auditing
6. **Use HTTPS**: Always use HTTPS in production to protect API keys in transit

---

## 🔒 Example: Generate Secure API Key

```bash
# Generate a secure random API key (Linux/Mac)
openssl rand -base64 32

# Or using Python
python3 -c "import secrets; print(secrets.token_urlsafe(32))"

# Or using .NET
dotnet run -c "Console.WriteLine(Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)))"
```

---

## ✅ Current Configuration

- **GraphQL Endpoint**: `http://localhost:5189/graphql`
- **GraphQL Playground**: `http://localhost:5189/ui/playground` (development only)
- **API Key Header**: `X-API-Key`
- **Development Key**: `dev-email-service-key-2024`
- **Protected Operations**: All mutations (sendEmail, generateHtml)
- **Public Operations**: Queries (health)

---

## 📋 Quick Test

### Test Health Query (No API Key Required)
```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -d '{"query": "{ health { status service timestamp } }"}'
```

### Test Mutation Without API Key (Should Fail)
```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message } }",
    "variables": {
      "input": {
        "to": "test@example.com",
        "subject": "Test",
        "body": "Test",
        "isHtml": false
      }
    }
  }'
```

### Test Mutation With Valid API Key (Should Succeed)
```bash
curl -X POST http://localhost:5189/graphql \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-email-service-key-2024" \
  -d '{
    "query": "mutation SendEmail($input: SendEmailInput!) { sendEmail(input: $input) { success message messageId } }",
    "variables": {
      "input": {
        "to": "husain.f4l@gmail.com",
        "subject": "Secure Test",
        "body": "This works!",
        "isHtml": false
      }
    }
  }'
```

---

## 🔍 Authentication Flow

1. Client sends GraphQL request to `/graphql`
2. If request contains a mutation, `GraphQLAuthInterceptor` checks for `X-API-Key` header
3. Header value is validated against configured API key
4. If valid, request proceeds; if invalid or missing, returns authorization error
5. Queries (like health check) bypass authentication

---

**Your EmailService GraphQL API is now secured with API key authentication!** 🔐
