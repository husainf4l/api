# Application Tracking Examples

## Overview
The SMS service tracks which application sent each message using optional HTTP headers `X-App-Name` and `X-App-Version`. This enables detailed analytics and usage monitoring per application.

## How to Use

### Include Headers in Your Requests

```bash
curl -X POST http://localhost:5103/api/sms/send/general \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-App-Name: CustomerPortal" \
  -H "X-App-Version: 2.1.5" \
  -d '{
    "to": "962771122003",
    "message": "Your order has been confirmed"
  }'
```

### Headers

| Header | Required | Description | Example |
|--------|----------|-------------|---------|
| `X-App-Name` | Optional | Name of the application | `CustomerCRM`, `MarketingApp`, `MobileApp` |
| `X-App-Version` | Optional | Version of the application | `1.0.0`, `2.5.3`, `3.2.1-beta` |

## Application Examples

### Customer CRM System
```bash
curl -X POST http://localhost:5103/api/sms/send/general \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-App-Name: CustomerCRM" \
  -H "X-App-Version: 3.2.1" \
  -d '{
    "to": "962771122003",
    "message": "Your appointment is scheduled for tomorrow"
  }'
```

### Marketing Campaign App
```bash
curl -X POST http://localhost:5103/api/sms/send/bulk \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-App-Name: MarketingApp" \
  -H "X-App-Version: 2.5.8" \
  -d '{
    "to": ["962771122003", "962796026659"],
    "message": "Special offer: 20% off this weekend!"
  }'
```

### Mobile Application
```bash
curl -X POST http://localhost:5103/api/sms/send/otp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-App-Name: MobileApp" \
  -H "X-App-Version: 1.8.2" \
  -d '{
    "to": "962771122003",
    "message": "Your verification code: 123456"
  }'
```

## View Messages by Application

### Get Recent Messages with App Info
```bash
curl -s -X GET "http://localhost:5103/api/sms/history?limit=10" \
  -H "X-API-Key: your-api-key" | jq '.messages[] | {
    id, 
    recipient, 
    message, 
    appName, 
    appVersion, 
    createdAt
  }'
```

**Example Output:**
```json
{
  "id": 5,
  "recipient": "962796026659",
  "message": "Test from Marketing app",
  "appName": "MarketingApp",
  "appVersion": "2.5.8",
  "createdAt": "2025-11-17T16:57:05.522701Z"
}
{
  "id": 4,
  "recipient": "962771122003",
  "message": "Test from CRM app",
  "appName": "CustomerCRM",
  "appVersion": "3.2.1",
  "createdAt": "2025-11-17T16:56:49.68121Z"
}
```

## Database Queries

### Messages by Application
```sql
SELECT 
    COALESCE(app_name, 'Unknown') as application,
    app_version,
    COUNT(*) as message_count,
    COUNT(DISTINCT recipient) as unique_recipients,
    MAX(created_at) as last_sent
FROM sms_messages
GROUP BY app_name, app_version
ORDER BY message_count DESC;
```

**Example Output:**
```
 application  | app_version | message_count | unique_recipients |           last_sent           
--------------+-------------+---------------+-------------------+-------------------------------
 MarketingApp | 2.5.8       |            45 |                32 | 2025-11-17 19:57:05.522701+03
 CustomerCRM  | 3.2.1       |            28 |                18 | 2025-11-17 19:56:49.68121+03
 MobileApp    | 1.8.2       |            12 |                 9 | 2025-11-17 18:30:15.123456+03
 Unknown      |             |             5 |                 4 | 2025-11-17 16:48:28.586638+03
```

### Daily Usage by App
```sql
SELECT 
    DATE(created_at) as date,
    app_name,
    COUNT(*) as messages_sent
FROM sms_messages
WHERE created_at > NOW() - INTERVAL '7 days'
GROUP BY DATE(created_at), app_name
ORDER BY date DESC, messages_sent DESC;
```

### App Version Distribution
```sql
SELECT 
    app_name,
    app_version,
    COUNT(*) as usage_count,
    ROUND(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER (PARTITION BY app_name), 2) as percentage
FROM sms_messages
WHERE app_name IS NOT NULL
GROUP BY app_name, app_version
ORDER BY app_name, usage_count DESC;
```

### Most Active Applications (Last 24 Hours)
```sql
SELECT 
    app_name,
    COUNT(*) as messages_today,
    COUNT(DISTINCT recipient) as unique_recipients,
    MIN(created_at) as first_message,
    MAX(created_at) as last_message
FROM sms_messages
WHERE created_at > NOW() - INTERVAL '24 hours'
  AND app_name IS NOT NULL
GROUP BY app_name
ORDER BY messages_today DESC;
```

## Use Cases

### 1. Monitor Application Usage
Track which applications are sending the most messages:
```bash
curl -s "http://localhost:5103/api/sms/history?limit=1000" \
  -H "X-API-Key: your-api-key" | \
  jq '[.messages[] | .appName] | group_by(.) | map({app: .[0], count: length}) | sort_by(.count) | reverse'
```

### 2. Identify App Versions
See which versions of your apps are actively sending messages:
```bash
curl -s "http://localhost:5103/api/sms/history?limit=1000" \
  -H "X-API-Key: your-api-key" | \
  jq '[.messages[] | select(.appName != null) | {app: .appName, version: .appVersion}] | group_by(.app) | map({app: .[0].app, versions: [.[].version] | unique})'
```

### 3. Billing/Chargeback per Application
Calculate costs per application for internal billing:
```sql
SELECT 
    app_name,
    COUNT(*) as total_messages,
    COUNT(*) * 0.05 as estimated_cost_usd  -- Assuming $0.05 per SMS
FROM sms_messages
WHERE created_at BETWEEN '2025-11-01' AND '2025-11-30'
  AND status = 'sent'
GROUP BY app_name
ORDER BY total_messages DESC;
```

### 4. Deprecation Tracking
Identify apps using old versions that need updates:
```sql
SELECT 
    app_name,
    app_version,
    COUNT(*) as message_count,
    MAX(created_at) as last_used
FROM sms_messages
WHERE app_version < '2.0.0'  -- Replace with your deprecation threshold
GROUP BY app_name, app_version
ORDER BY last_used DESC;
```

## Best Practices

### 1. Always Include App Headers
```javascript
// Node.js example
const axios = require('axios');

const sendSMS = async (to, message) => {
  return axios.post('http://localhost:5103/api/sms/send/general', {
    to,
    message
  }, {
    headers: {
      'Content-Type': 'application/json',
      'X-API-Key': process.env.SMS_API_KEY,
      'X-App-Name': 'MyCustomerPortal',
      'X-App-Version': '2.1.5'  // Read from package.json or config
    }
  });
};
```

### 2. Use Semantic Versioning
```
X-App-Version: 1.2.3
               │ │ │
               │ │ └─ Patch (bug fixes)
               │ └─── Minor (new features, backward compatible)
               └───── Major (breaking changes)
```

### 3. Consistent App Names
Use consistent naming across your organization:
- ✅ `CustomerCRM`, `MarketingCampaigns`, `MobileApp`
- ❌ `crm`, `CRM System`, `customer-crm` (inconsistent)

### 4. Update Tracking on Deployments
When deploying a new version, update the version header:
```bash
# In your deployment script
export APP_VERSION=$(cat package.json | jq -r '.version')
# Your application reads APP_VERSION and includes it in headers
```

## Integration Examples

### Python
```python
import requests
import os

def send_sms(to, message):
    response = requests.post(
        'http://localhost:5103/api/sms/send/general',
        json={'to': to, 'message': message},
        headers={
            'Content-Type': 'application/json',
            'X-API-Key': os.getenv('SMS_API_KEY'),
            'X-App-Name': 'PythonBackend',
            'X-App-Version': '1.0.0'
        }
    )
    return response.json()
```

### C# / .NET
```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

public class SmsClient
{
    private readonly HttpClient _client;
    private readonly string _apiKey;
    
    public SmsClient(string apiKey)
    {
        _client = new HttpClient();
        _apiKey = apiKey;
        _client.DefaultRequestHeaders.Add("X-App-Name", "DotNetService");
        _client.DefaultRequestHeaders.Add("X-App-Version", "2.3.1");
    }
    
    public async Task<HttpResponseMessage> SendSmsAsync(string to, string message)
    {
        var payload = new { to, message };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );
        
        _client.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
        return await _client.PostAsync(
            "http://localhost:5103/api/sms/send/general",
            content
        );
    }
}
```

### PHP
```php
<?php
function sendSMS($to, $message) {
    $ch = curl_init('http://localhost:5103/api/sms/send/general');
    
    curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
    curl_setopt($ch, CURLOPT_POST, true);
    curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode([
        'to' => $to,
        'message' => $message
    ]));
    curl_setopt($ch, CURLOPT_HTTPHEADER, [
        'Content-Type: application/json',
        'X-API-Key: ' . getenv('SMS_API_KEY'),
        'X-App-Name: PHPWebsite',
        'X-App-Version: 1.5.0'
    ]);
    
    $response = curl_exec($ch);
    curl_close($ch);
    
    return json_decode($response, true);
}
?>
```

---

**Last Updated:** November 17, 2025
