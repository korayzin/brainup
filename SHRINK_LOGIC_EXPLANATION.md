# 🧠 Shrink Mantığı Açıklaması

## Shader Mantığı

Shader'da (`BrainShrivel.shader` satır 78):
```hlsl
float scaleFactor = lerp(1.0, _MinObjectScale, _ShrinkAmount);
```

Bu demek ki:
- **`_ShrinkAmount = 0.0`** → `scaleFactor = 1.0` (tam büyük beyin)
- **`_ShrinkAmount = 1.0`** → `scaleFactor = _MinObjectScale` (küçük beyin)

**Yani: Shrink değeri YÜKSEK = Beyin KÜÇÜK**

## Oyun Mantığı

- **Başlangıç shrink:** 0.400 (küçük beyin - kaybetme durumu)
- **Kazanma threshold:** 0.390 (büyük beyin - kazanma durumu)
- **Hedef:** Shrink değerini 0.390'ın altına çekmek (beyin büyütmek)

## Sorun ve Çözüm

### Sorun:
`_MinObjectScale` değeri çok yüksek (0.6-0.7) olduğu için:
- Shrink = 0.400 → Scale = lerp(1.0, 0.7, 0.4) = **0.88** (hala büyük görünüyor!)
- Shrink = 0.390 → Scale = lerp(1.0, 0.7, 0.39) = **0.88** (hala büyük görünüyor!)

### Çözüm:
`_MinObjectScale` değerini daha düşük yapmalıyız (0.45):
- Shrink = 0.400 → Scale = lerp(1.0, 0.45, 0.4) = **0.78** (küçük görünüyor ✅)
- Shrink = 0.390 → Scale = lerp(1.0, 0.45, 0.39) = **0.78** (küçük görünüyor ✅)
- Shrink = 0.300 → Scale = lerp(1.0, 0.45, 0.3) = **0.84** (daha büyük görünüyor ✅)
- Shrink = 0.200 → Scale = lerp(1.0, 0.45, 0.2) = **0.91** (çok büyük görünüyor ✅)
- Shrink = 0.000 → Scale = lerp(1.0, 0.45, 0.0) = **1.00** (tam büyük görünüyor ✅)

## Yapılan Değişiklikler

1. **Başlangıç shrink değeri:** 0.379 → **0.400**
2. **MinObjectScale limit:** 0.7 → **0.45** (daha belirgin küçük beyin için)
3. **Debug log'ları:** Shrink mantığını açıklayan log'lar eklendi

## Test Senaryoları

| Shrink Değeri | Scale Hesaplama | Görünüm | Durum |
|---------------|----------------|---------|-------|
| 0.400 | lerp(1.0, 0.45, 0.4) = 0.78 | Küçük | Başlangıç (Kaybetme) |
| 0.390 | lerp(1.0, 0.45, 0.39) = 0.78 | Küçük | Threshold (Kazanma) |
| 0.300 | lerp(1.0, 0.45, 0.3) = 0.84 | Orta | İyi |
| 0.200 | lerp(1.0, 0.45, 0.2) = 0.91 | Büyük | Çok İyi |
| 0.000 | lerp(1.0, 0.45, 0.0) = 1.00 | Tam Büyük | Mükemmel |

## Önemli Notlar

1. **Shrink değeri YÜKSEK = Beyin KÜÇÜK** (shader mantığı)
2. **Shrink değeri DÜŞÜK = Beyin BÜYÜK** (shader mantığı)
3. **Başlangıç shrink = 0.400** (küçük beyin - kaybetme durumu)
4. **Kazanma shrink ≤ 0.390** (büyük beyin - kazanma durumu)
5. **MinObjectScale = 0.45** (daha belirgin görsel fark için)

