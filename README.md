# DataScrub — Otonom Veri Temizleme Platformu

MobileCRM'deki "firma eşleştirme, veri normalizasyonu, mükerrer kayıt kontrolü" deneyiminin
genelleştirilmiş, bağımsız bir ürün haline getirilmiş hali.

## Mimari

```
CSV/Excel dosyası
      ↓
ASP.NET Core Web API  (DataScrub.API)
  ├── Domain            → Dataset, DetectedIssue entity'leri
  ├── Application        → DatasetService (orkestrasyon), DTO'lar, interface'ler
  └── Infrastructure     → EF Core (SQL Server), Python servisine HTTP client
      ↓  (REST)
Python ML Servisi (FastAPI)
  ├── duplicate_detection.py   → fuzzy matching ile mükerrer kayıt tespiti
  ├── missing_data.py          → grup bazlı akıllı eksik veri doldurma
  └── format_fixing.py         → tarih/telefon/isim format standardizasyonu
      ↓
Sonuçlar DB'ye yazılır → React dashboard'da onay/red
```

## Kurulum

### 1. Python ML Servisi

```bash
cd ml-service
python3 -m venv venv
source venv/bin/activate       # Windows: venv\Scripts\activate
pip install -r requirements.txt
python3 main.py                 # http://localhost:8000 üzerinde açılır
```

Test etmek için: `http://localhost:8000/docs` adresinde Swagger UI otomatik açılır.

### 2. ASP.NET Core Backend

Gereksinim: [.NET 8 SDK](https://dotnet.microsoft.com/download) ve SQL Server (LocalDB yeterli).

```bash
cd backend
dotnet restore
dotnet ef migrations add InitialCreate --project DataScrub.Infrastructure --startup-project DataScrub.API
dotnet ef database update --project DataScrub.Infrastructure --startup-project DataScrub.API
dotnet run --project DataScrub.API      # http://localhost:5xxx üzerinde açılır
```

> `dotnet ef` komutları için önce şu paketi kurman gerekebilir:
> `dotnet tool install --global dotnet-ef`

Swagger arayüzü: `https://localhost:<port>/swagger`

### 3. Test Akışı (Swagger üzerinden)

1. `POST /api/datasets/upload` — bir CSV dosyası yükle (repo'da `sample-data/test_data.csv` var)
2. `POST /api/datasets/{id}/analyze` — Python servisini tetikler, sorunları tespit eder
3. `GET /api/datasets/{id}` — tespit edilen sorunları ve güven skorlarını gör
4. `POST /api/datasets/issues/{issueId}/resolve` — öneriyi onayla/reddet

## Sonraki Adımlar (henüz yapılmadı)

- [ ] React frontend (dosya yükleme, önce/sonra karşılaştırma dashboard'u)
- [ ] Hangfire ile asenkron analiz (büyük dosyalarda HTTP timeout riski var)
- [ ] Export endpoint'i (onaylanan düzeltmelerle temiz dosya üretme)
- [ ] Unit testler (xUnit — DatasetService ve ML modülleri için)
- [ ] Docker Compose (backend + ml-service + SQL Server tek komutla ayağa kalksın)
- [ ] Kullanıcının hangi kolonların "kimlik" kolonu olduğunu seçebilmesi (şu an otomatik tahmin ediliyor)

## Neden Bu Mimari?

- **Mikroservis ayrımı:** .NET ve Python birbirinden bağımsız deploy edilebilir, ML tarafı
  ölçeklendirilmek istenirse (örn. GPU'lu sunucuya taşınmak istenirse) API'ye dokunmadan olur.
- **Interface tabanlı tasarım:** `IMlAnalysisService` sayesinde Application katmanı Python'un
  varlığından haberdar değil — ileride farklı bir ML çözümüne geçmek Infrastructure katmanıyla sınırlı kalır.
- **Güven skoru:** Her öneri 0.0-1.0 arası bir skorla geliyor, kullanıcı körü körüne otomasyona
  güvenmiyor, "insan onaylı AI" yaklaşımı benimsendi.
