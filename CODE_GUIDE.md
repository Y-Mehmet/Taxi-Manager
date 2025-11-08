# Seyyahın Kod Rehberi (CODE_GUIDE.md)

Bu belge, projedeki temel mekaniklerin nasıl çalıştığını ve script'lerin birbiriyle nasıl etkileşimde bulunduğunu açıklayan canlı bir referans kaynağıdır. Amacı, projeye dahil olan herhangi bir geliştiricinin (veya yapay zekanın) sistemin mimarisini hızla anlamasını sağlamaktır.

---

## 1. Yetenek: Stop Ekleme (Add Stop Ability)

Bu mekanik, oyuncunun oyun alanına yeni "Stop"lar eklemesini sağlar. Bu yetenek, envanterden harcanan bir öğe **değildir**; maksimum stop sayısına ulaşılana kadar tekrar kullanılabilir.

### a. İlgili Script'ler

- **`StopManager.cs`**: Sahnedeki tüm Stop'ları yöneten merkezi script. Stop'ların başlangıç durumunu ayarlar, yeni Stop'ları aktive eder ve mevcut/maksimum stop sayısını tutar.
- **`AbilityManager.cs`**: Tüm oyuncu yeteneklerini yöneten merkezi script. `AddNewStop` yeteneğinin kullanılmasını tetikler ve maksimum limite ulaşıldığında yetenek sayısını `0`'a ayarlayarak butonun kapanmasını sağlar.
- **`AbilityButton.cs`**: UI'daki yetenek butonunu temsil eden script. `AbilityManager`'daki yetenek sayısını (`OnAbilityCountChanged` eventi ile) dinler. Sayı `0`'dan büyükse butonu tıklanabilir, `0` ise tıklanamaz yapar.

### b. İşleyiş Akışı

1.  **Başlangıç (Initialization):**
    - `StopManager.FindAndRegisterInitialStops()` metodu çalışır ve başlangıçta **1** adet Stop'u aktif hale getirir.
    - `AbilityButton.cs`, `AbilityManager`'dan `AddNewStop` yeteneğinin başlangıç sayısını (genellikle `1` veya daha fazla) alır ve butonu `interactable = true` yapar.

2.  **Yetenek Kullanımı (Ability Usage):**
    - Oyuncu, UI'daki "Add Stop" butonuna tıklar.
    - `AbilityButton.HandleButtonClick()`, `AbilityManager.Instance.UseAbility(AbilityType.AddNewStop)` fonksiyonunu çağırır.

3.  **Yetenek İşleme (Ability Execution):**
    - `AbilityManager.UseAbility()` metodu, yetenek tipinin `AddNewStop` olduğunu görür ve bu yeteneği envanterden **tüketmez** (`ConsumeAbility` çağrılmaz).
    - `ExecuteAddNewStop()` metodu tetiklenir.

4.  **Stop Aktivasyonu ve Maksimum Kontrolü:**
    - `ExecuteAddNewStop()` içinde, `StopManager.Instance.ActivateNextStop()` çağrılarak yeni bir stop aktive edilir.
    - Ardından, aktif stop sayısı (`AllStops.Count`) ile toplam mümkün olan stop sayısı (`AllPossibleStops.Count`) karşılaştırılır.
    - Eğer sayılar eşitse (maksimuma ulaşılmışsa), `AbilityManager` envanterdeki `AddNewStop` yeteneğinin sayısını `0`'a çeker ve `OnAbilityCountChanged` event'ini tetikler.

5.  **Butonun Kapanması (Button Disabled):**
    - `AbilityButton.cs`, `OnAbilityCountChanged` event'ini yakalar. Yeni yetenek sayısı `0` olduğu için, `UpdateButtonUI` metodu butonun `interactable` özelliğini `false` yaparak tıklanmasını kalıcı olarak engeller.

### c. Özet Şema

`AbilityButton` (Tıklar) -> `AbilityManager.UseAbility()` (Tüketmez!) -> `ExecuteAddNewStop()` -> `StopManager.ActivateNextStop()`

`StopManager` (Maksimuma Ulaşır) -> `AbilityManager` (Yetenek sayısını `0` yapar) -> `OnAbilityCountChanged` Eventi -> `AbilityButton` (Kendini Kapatır)
