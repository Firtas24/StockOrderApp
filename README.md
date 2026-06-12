# Stock & Order Management System (.NET 8 MVC)

Bu proje, modern web mimarisi ve kurumsal tasarım kalıpları (Design Patterns) dikkate alınarak geliştirilmiş, güvenliği ve performansı optimize edilmiş bir **Stok ve Sipariş Yönetim Sistemi** uygulamasıdır. 

Proje, özellikle ilişkisel veritabanı yönetimi, rol bazlı yetkilendirme ve defansif kodlama (Defensive Programming) prensiplerini sergilemek amacıyla inşa edilmiş bir vitrin çalışmasıdır.

---

## 🚀 Öne Çıkan Teknik Özellikler

### 🛡️ Siber Güvenlik ve Validasyon (OWASP Top 10)
* **Rol Bazlı Yetkilendirme (RBAC):** ASP.NET Core Identity altyapısı kullanılarak `Admin` ve `User` rolleri kurgulanmıştır. Yönetim paneli (`Areas/Admin`) sadece Admin rolüne sahip kullanıcılara tamamen kapalıdır.
* **CSRF/XSRF Koruması:** Veritabanında veri manipülasyonu (ekleme, silme, güncelleme) yapan tüm HTTP Post aksiyonları `[ValidateAntiForgeryToken]` niteliğiyle sahte isteklere karşı korunmaktadır.
* **SQL Injection Koruması:** Entity Framework Core ve LINQ asenkron sorguları sayesinde tüm veri tabanı istekleri doğal olarak parametrik çalışmakta ve SQL enjeksiyon risklerini sıfırlamaktadır.
* **Güvenli Dosya Yükleme (File Upload):** Ürün görselleri yüklenirken uzantı doğrulaması (Beyaz Liste) yapılmakta ve `Guid` kullanılarak dosya isimleri benzersiz hale getirilmektedir.

### ⚡ Performans ve Sistem Kaynakları Yönetimi
* **RAM Optimizasyonu:** Sadece listeleme yapılan (veri tabanında değişiklik gerektirmeyen) sorgularda `.AsNoTracking()` kullanılarak EF Core'un hafıza takibi devre dışı bırakılmış ve performans artırılmıştır.
* **Veri Yaşam Döngüsü (Data Lifecycle):** Bir ürün silindiğinde veya resmi güncellendiğinde, eski görsel sunucu diskinden (`wwwroot/uploads`) fiziksel olarak silinerek sunucuda çöp dosya birikmesi engellenmiştir.
* **Asenkron Mimari:** Veritabanı ve dosya sistemine yapılan tüm çağrılar `async/await` yapısıyla asenkron olarak yönetilerek uygulamanın ölçeklenebilirliği (scalability) artırılmıştır.

### 🎛️ İş Mantığı (Business Logic) & UI/UX
* **Hata Yönetimi (Exception Handling):** İlişkisel veritabanlarında sık yaşanan "bağlı kayıt silme hatası" `DbUpdateException` ile yakalanmış; sistemin çömesi engellenerek kullanıcıya şık arayüz uyarıları (`Tempdata`) dönülmüştür.
* **Gelişmiş Arama & Filtreleme:** Sipariş yönetiminde null veri kontrolleriyle arama güvenliği sağlanmış, tarih aralığı filtrelerinde zaman kaymalarını önleyen akıllı algoritmalar kullanılmıştır.
* **Modern Arayüz:** Bootstrap 5 bileşenleri kullanılarak, temiz, ferah ve içerik odaklı modern bir kullanıcı deneyimi hedeflenmiştir.

---

## 🛠️ Kullanılan Teknolojiler

* **Backend:** .NET 8 (C#) / ASP.NET Core MVC
* **Veritabanı ve ORM:** MSSQL / Entity Framework Core (Code First)
* **Kimlik Doğrulama:** ASP.NET Core Identity
* **Frontend:** Razor Views, Bootstrap 5, HTML5, CSS3, JavaScript

---

## 💻 Kurulum ve Çalıştırma

Projeyi lokal bilgisayarınızda test etmek için:

1. Projeyi klonlayın: `git clone <repo-url>`
2. `appsettings.json` dosyasındaki `ConnectionStrings` alanını kendi yerel veritabanınıza göre düzenleyin.
3. Paketleri yüklemek ve veritabanı migration'larını uygulamak için Package Manager Console'da çalıştırın:
   ```bash
   Update-Database
