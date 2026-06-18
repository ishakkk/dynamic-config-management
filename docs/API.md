# API Dokümantasyonu

Base URL: `http://localhost:8080/api`

## Endpoints

### GET /configurations

Tüm konfigürasyon kayıtlarını listeler.

**Response 200**

```json
[
  {
    "id": 1,
    "name": "SiteName",
    "type": "String",
    "value": "soty.io",
    "isActive": true,
    "applicationName": "SERVICE-A",
    "updatedAt": "2026-06-10T12:00:00Z"
  }
]
```

### GET /configurations/application/{applicationName}

Belirli uygulamaya ait kayıtları döner.

### POST /configurations

Yeni kayıt oluşturur. RabbitMQ'ya `Created` eventi yayınlar.

**Request**

```json
{
  "name": "SiteName",
  "type": "String",
  "value": "soty.io",
  "isActive": true,
  "applicationName": "SERVICE-A"
}
```

### PUT /configurations/{id}

Kayıt günceller. RabbitMQ'ya `Updated` eventi yayınlar.

### DELETE /configurations/{id}

Kayıt siler. RabbitMQ'ya `Deleted` eventi yayınlar.

## Configuration Value Types

| Type | Örnek Value |
|------|-------------|
| String | `soty.io` |
| Int | `50` |
| Double | `12.5` |
| Bool | `1` veya `0` |

## Sample Service Endpoints

Base URL: `http://localhost:8081`

### GET /

SERVICE-A aktif konfigürasyon özetini döner.

### GET /config/{key}

Belirli bir key değerini döner.
