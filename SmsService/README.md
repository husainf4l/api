# SmsService

A production-ready microservice for sending SMS messages via JOSMS Gateway with API key authentication and support for OTP, general, and bulk messaging.

## 🚀 Features

- **JOSMS Gateway Integration**: Send SMS using Jordan SMS (JOSMS) platform
- **API Key Authentication**: Secure endpoints with X-API-Key header validation
- **Multiple SMS Types**:
  - OTP Messages (One-Time Passwords)
  - General Messages (Announcements, notifications)
  - Bulk Messages (Up to 120 recipients)
- **Balance Checking**: Query remaining SMS credits
- **Phone Number Validation**: Automatic normalization for Jordanian numbers (962)
- **Health Monitoring**: Health check endpoint

## 📋 Prerequisites

- .NET 10.0 SDK
- JOSMS Account credentials

## 🏗️ Project Structure

```
SmsService/
├── Controllers/
│   └── SmsController.cs      # API endpoints
├── Services/
│   └── JosmsSmsService.cs    # JOSMS gateway integration
├── DTOs/
│   └── SmsDTOs.cs            # Request/Response models
├── Middleware/
│   └── ApiKeyAuthMiddleware.cs # Authentication
├── .env                       # Environment variables (not in git)
├── appsettings.json          # Configuration
├── Program.cs                # Application entry point
└── README.md                 # This file
```

## 🚀 Quick Start

### 1. Configure Environment Variables

Create `.env` file:

```env
JOSMS_ACCNAME=margogroup
JOSMS_ACCPASSWORD=nR@9g@Z7yV0@sS9bX1y
JOSMS_SENDER_ID=MargoGroup
API_KEY=your-secure-api-key
```

### 2. Run the Service

```bash
cd SmsService
dotnet run
```

The service will be available at `http://localhost:5000`

### 3. Test the API

```bash
# Health check
curl http://localhost:5000/api/sms/health

# Get balance
curl -H "X-API-Key: your-api-key" \
  http://localhost:5000/api/sms/balance
```

## 📡 API Endpoints

### Health Check
```http
GET /api/sms/health
```
No authentication required.

### Get Balance
```http
GET /api/sms/balance
X-API-Key: your-api-key
```

**Response:**
```json
{
  "success": true,
  "message": "Balance retrieved successfully",
  "balance": 1500.50,
  "rawResponse": "1500.50"
}
```

### Send OTP SMS
```http
POST /api/sms/send/otp
Content-Type: application/json
X-API-Key: your-api-key
```

**Request Body:**
```json
{
  "to": "0775444418",
  "message": "Your OTP code is: 123456",
  "senderId": "MargoGroup"
}
```

**Response:**
```json
{
  "success": true,
  "message": "OTP SMS sent successfully",
  "rawResponse": "..."
}
```

### Send General SMS
```http
POST /api/sms/send/general
Content-Type: application/json
X-API-Key: your-api-key
```

**Request Body:**
```json
{
  "to": "962775444418",
  "message": "Your appointment is tomorrow at 10:00 AM",
  "senderId": "MargoGroup"
}
```

### Send Bulk SMS
```http
POST /api/sms/send/bulk
Content-Type: application/json
X-API-Key: your-api-key
```

**Request Body:**
```json
{
  "to": [
    "0775444418",
    "0786543210",
    "962795551234"
  ],
  "message": "Special offer: 20% off all items!",
  "senderId": "MargoGroup"
}
```

**Note:** Maximum 120 numbers per bulk request.

## 📱 Phone Number Formats

The service automatically normalizes Jordanian phone numbers. Supported formats:

- `0775444418` → `962775444418`
- `962775444418` → `962775444418`
- `+962775444418` → `962775444418`
- `00962775444418` → `962775444418`

**Requirements:**
- Must be Jordanian number (starts with 962)
- Operator codes: 77, 78, or 79
- Total length: 12 digits (962 + 9 digits)

## 🔒 Security

- **API Key Authentication**: All SMS endpoints require valid X-API-Key header
- **Environment Variables**: Sensitive credentials stored in .env
- **Input Validation**: Phone numbers and message content validated
- **Logging**: All operations logged for audit

## 🛠️ Configuration

### appsettings.json

```json
{
  "JosmsSettings": {
    "BaseUrl": "https://www.josms.net",
    "AccName": "margogroup",
    "AccPassword": "nR@9g@Z7yV0@sS9bX1y",
    "DefaultSenderId": "MargoGroup"
  },
  "ApiSettings": {
    "ApiKey": "dev-sms-service-key-2024"
  }
}
```

## 🧪 Testing

### Using curl

```bash
# OTP SMS
curl -X POST http://localhost:5000/api/sms/send/otp \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-sms-service-key-2024" \
  -d '{
    "to": "0775444418",
    "message": "Your verification code is: 123456"
  }'

# Bulk SMS
curl -X POST http://localhost:5000/api/sms/send/bulk \
  -H "Content-Type: application/json" \
  -H "X-API-Key: dev-sms-service-key-2024" \
  -d '{
    "to": ["0775444418", "0786543210"],
    "message": "Important announcement!"
  }'
```

### Using Postman

1. Create POST request
2. URL: `http://localhost:5000/api/sms/send/otp`
3. Headers:
   - `Content-Type: application/json`
   - `X-API-Key: dev-sms-service-key-2024`
4. Body (raw JSON):
```json
{
  "to": "0775444418",
  "message": "Test message"
}
```

## 🚨 Error Handling

**Common Error Responses:**

```json
// Missing API Key
{
  "error": "API Key is missing"
}

// Invalid Phone Number
{
  "success": false,
  "message": "Invalid phone number format. Must be 962XXXXXXXXX"
}

// Bulk Limit Exceeded
{
  "error": "Maximum 120 numbers allowed for bulk SMS"
}
```

## 📊 JOSMS Gateway Details

**Provider:** JOSMS.net (Jordan SMS Platform)

**Gateway Types:**
1. **OTP Gateway**: For one-time passwords and verification codes
2. **General Gateway**: For announcements, reminders, notifications
3. **Bulk Gateway**: For mass messaging (up to 120 recipients)

**Rate Limits:** Managed by JOSMS platform

## 🔧 Development

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run in development mode
dotnet run --environment Development

# Watch mode (auto-reload)
dotnet watch run
```

## 📝 License

Private - Internal Use Only

## 👤 Contact

For JOSMS account support or questions, contact your account manager.

---

**Built with**: ASP.NET Core 10.0 | JOSMS Gateway | HTTP Client
