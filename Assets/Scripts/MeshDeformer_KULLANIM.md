# MeshDeformer - Kullanım Kılavuzu

## 🎯 Genel Bakış
MeshDeformer scripti, Unity'de çarpışmalarda yumuşak cisim (soft body) etkisi oluşturur. Bir nesne başka bir nesneye çarptığında, mesh deforme olur ve sonra yavaşça orijinal haline döner.

---

## ⚙️ ÇALIŞMASI İÇİN GEREKLİ AYARLAR

### 1️⃣ **DEFORME OLACAK NESNE (MeshDeformer scripti olan nesne)**

#### **Gerekli Bileşenler:**
- ✅ **MeshFilter** (otomatik eklenir)
- ✅ **MeshCollider** (otomatik eklenir)
- ✅ **MeshDeformer Script** (siz eklersiniz)

#### **MeshCollider Ayarları:**
- ❌ **Is Trigger: KAPALI** (OFF) - Çok önemli! Trigger açıksa OnCollisionEnter çalışmaz!
- ⚠️ **Convex: İsteğe bağlı** - Basit mesh'ler için açık olabilir, karmaşık mesh'ler için kapalı olmalı

#### **Rigidbody (İsteğe bağlı):**
- Eğer bu nesnede Rigidbody varsa:
  - ❌ **Is Kinematic: KAPALI** (OFF) - Fizik çarpışmaları için gerekli
  - ✅ **Use Gravity: Açık veya kapalı** (isteğe bağlı)

---

### 2️⃣ **ÇARPAN NESNE (Diğer nesne)**

#### **Gerekli Bileşenler:**
- ✅ **Collider** (BoxCollider, SphereCollider, CapsuleCollider, MeshCollider - herhangi biri)
- ✅ **Rigidbody** (MUTLAKA GEREKLİ!)

#### **Collider Ayarları:**
- ❌ **Is Trigger: KAPALI** (OFF) - Trigger açıksa çarpışma algılanmaz!

#### **Rigidbody Ayarları:**
- ❌ **Is Kinematic: KAPALI** (OFF) - Fizik çarpışması için gerekli
- ✅ **Use Gravity: Açık** (nesnenin düşmesi için) veya **Kapalı** (manuel hareket için)
- ✅ **Mass: 1** (varsayılan, değiştirilebilir)
- ✅ **Drag: 0** (varsayılan, değiştirilebilir)

---

## 📋 ADIM ADIM KURULUM

### Senaryo 1: Küp'e MeshDeformer Ekleme

1. **Hierarchy'de bir küp oluştur:**
   - GameObject → 3D Object → Cube

2. **MeshDeformer scriptini ekle:**
   - Küp seçiliyken → Inspector → Add Component → MeshDeformer

3. **MeshCollider ayarlarını kontrol et:**
   - Inspector'da MeshCollider'ı bul
   - **Is Trigger: KAPALI** olduğundan emin ol

4. **Çarpan nesneyi hazırla:**
   - Yeni bir küp veya küre oluştur
   - **Rigidbody** ekle (Add Component → Rigidbody)
   - **Use Gravity: Açık** yap
   - **Is Kinematic: KAPALI** olduğundan emin ol
   - Collider'ın **Is Trigger: KAPALI** olduğundan emin ol

5. **Test et:**
   - Play'e bas
   - Çarpan nesneyi deforme olacak nesnenin üzerine bırak
   - Çarpışma olmalı ve deformasyon görülmeli!

---

### Senaryo 2: Küre'ye MeshDeformer Ekleme

1. **Hierarchy'de bir küre oluştur:**
   - GameObject → 3D Object → Sphere

2. **MeshDeformer scriptini ekle**

3. **MeshCollider ayarlarını kontrol et:**
   - **Is Trigger: KAPALI**

4. **Çarpan nesneyi hazırla:**
   - Rigidbody ekle
   - **Is Kinematic: KAPALI**
   - **Use Gravity: Açık**

5. **Test et!**

---

## 🔧 INSPECTOR PARAMETRELERİ

### **Deformasyon Ayarları:**

#### **Deform Radius (0.5 varsayılan)**
- Deformasyonun etkili olacağı yarıçap
- **Küçük değer** = Sadece temas noktasına çok yakın vertex'ler etkilenir
- **Büyük değer** = Daha geniş alan etkilenir
- **Önerilen:** 0.3 - 1.0 arası

#### **Deform Force (0.1 varsayılan)**
- Deformasyon kuvveti - Ne kadar içeri göçeceği
- **Küçük değer** = Hafif deformasyon
- **Büyük değer** = Güçlü deformasyon
- **Önerilen:** 0.05 - 0.5 arası

#### **Restore Speed (0.1 varsayılan)**
- Orijinal pozisyona geri dönüş hızı
- **Küçük değer** = Yavaş geri dönüş
- **Büyük değer** = Hızlı geri dönüş
- **Önerilen:** 0.05 - 0.3 arası

### **Debug Ayarları:**

#### **Debug Mode (Açık/Kapalı)**
- Açıksa: Konsola çarpışma bilgileri yazdırılır
- Sorun gidermede çok faydalı!

---

## ❌ SIK KARŞILAŞILAN SORUNLAR

### **Sorun 1: Çarpışma algılanmıyor**

**Çözüm:**
1. ✅ Çarpan nesnede **Rigidbody** var mı? (MUTLAKA OLMALI!)
2. ✅ Her iki nesnede de **Collider** var mı?
3. ✅ Collider'larda **Is Trigger: KAPALI** mı?
4. ✅ Rigidbody'lerde **Is Kinematic: KAPALI** mı?
5. ✅ Nesneler gerçekten birbirine değiyor mu? (pozisyonları kontrol et)

### **Sorun 2: Deformasyon görünmüyor**

**Çözüm:**
1. ✅ **Deform Radius** çok küçük olabilir - artır (örn: 1.0)
2. ✅ **Deform Force** çok küçük olabilir - artır (örn: 0.5)
3. ✅ **Debug Mode** açık olsun, konsola bak
4. ✅ Çarpışma şiddeti çok düşük olabilir - Rigidbody'nin hızını artır

### **Sorun 3: Mesh bozuluyor veya garip görünüyor**

**Çözüm:**
1. ✅ **Deform Force** çok yüksek olabilir - azalt
2. ✅ **Deform Radius** çok büyük olabilir - azalt
3. ✅ Mesh çok karmaşık olabilir - daha basit mesh dene

### **Sorun 4: Geri dönüş çok yavaş/hızlı**

**Çözüm:**
1. ✅ **Restore Speed** değerini ayarla
   - Yavaşsa: Artır (örn: 0.3)
   - Hızlıysa: Azalt (örn: 0.05)

---

## 🎮 TEST SENARYOSU

### Basit Test Sahnesi:

1. **Zemin oluştur:**
   - GameObject → 3D Object → Plane
   - Scale: (10, 1, 10)

2. **Deforme olacak nesne:**
   - GameObject → 3D Object → Cube
   - Position: (0, 1, 0)
   - MeshDeformer script ekle
   - MeshCollider: Is Trigger = KAPALI

3. **Çarpan nesne:**
   - GameObject → 3D Object → Sphere
   - Position: (0, 5, 0)
   - Rigidbody ekle
   - Rigidbody: Use Gravity = Açık, Is Kinematic = KAPALI
   - SphereCollider: Is Trigger = KAPALI

4. **Play'e bas ve izle!**
   - Küre düşecek, küpe çarpacak ve küp deforme olacak!

---

## 💡 İPUÇLARI

1. **Debug Mode'u açık tut** - Sorun gidermede çok yardımcı olur
2. **Basit mesh'lerle başla** - Küp, küre gibi basit şekillerle test et
3. **Parametreleri yavaş yavaş artır** - Aşırı değerler mesh'i bozabilir
4. **Rigidbody hızını kontrol et** - Çok hızlı çarpışmalar aşırı deformasyona neden olabilir
5. **MeshCollider Convex ayarını dikkatli kullan** - Karmaşık mesh'lerde kapalı tut

---

## 📝 ÖZET CHECKLIST

Çarpışmanın çalışması için:

- [ ] Deforme olacak nesnede MeshDeformer script var
- [ ] Deforme olacak nesnede MeshCollider var
- [ ] MeshCollider: Is Trigger = **KAPALI**
- [ ] Çarpan nesnede Collider var
- [ ] Çarpan nesnede **Rigidbody var** (MUTLAKA!)
- [ ] Rigidbody: Is Kinematic = **KAPALI**
- [ ] Collider: Is Trigger = **KAPALI**
- [ ] Nesneler birbirine değiyor
- [ ] Debug Mode açık (test için)

---

**İyi çalışmalar! 🚀**
