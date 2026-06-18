# Kurulum Rehberi

## Gereksinimler

- .NET 8 SDK
- Docker Desktop (önerilen)
- SQL Server 2022 (Docker ile otomatik)
- RabbitMQ 3.13 (Docker ile otomatik)

## Docker Compose Kurulumu

```bash
git clone <repo-url>
cd DynamicConfig
docker compose up --build
```

İlk açılışta:

1. SQL Server migration otomatik uygulanır
2. Örnek seed verileri eklenir
3. RabbitMQ topology (exchange, queue, DLQ) oluşturulur

## Manuel Kurulum

### 1. Veritabanı

```bash
dotnet ef database update \
  --project src/DynamicConfig.Infrastructure \
  --startup-project src/DynamicConfig.Api
```

### 2. API

```bash
dotnet run --project src/DynamicConfig.Api
```

### 3. Sample Service

```bash
dotnet run --project src/SampleService.A
```

## Konfigürasyon

`src/DynamicConfig.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=DynamicConfigDb;User Id=sa;Password=Your_strong_password123;TrustServerCertificate=True;"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672
  }
}
```

## Admin UI

Tarayıcıda `http://localhost:8080` adresine gidin.

- Kayıtları listeleyin
- İsim alanından client-side filtre uygulayın
- Yeni kayıt ekleyin veya mevcut kaydı güncelleyin

## Dead Letter Queue Testi

RabbitMQ Management UI üzerinden `dynamic-config.dlq` kuyruğunu izleyebilirsiniz.

Bilinçli hata üretmek için `ApplicationName = FORCE-FAIL` olan bir mesaj consumer tarafında DLQ'ya yönlendirilir.
