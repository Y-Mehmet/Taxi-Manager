                if (availableWagon != null)
                {
                    Debug.Log($"<color=cyan>EŞLEŞME BULUNDU:</color> {availableWagon.wagonColor} renkli yük, {passenger.groupColor} renkli işçi tarafından alınıyor.");

                    // Yükü (vagonu) oyundan kaldır.
                    WagonManager.Instance.DeregisterWagon(availableWagon);
                    WagonManager.Instance.TriggerWagonRemovalEvent(availableWagon, availableWagon.transform);
                    availableWagon.gameObject.SetActive(false);
                    Debug.Log($"<color=yellow>YÜK ALINDI:</color> {availableWagon.name} yükü oyundan kaldırıldı.");

                    // İşçinin (PassengerGroup) kalan kapasitesini bir azalt.
                    passenger.GroupSize--;
                    Debug.Log($"<color=lightblue>İŞÇİ GÜNCELLENDİ:</color> {passenger.name} işçisinin kalan kapasitesi: {passenger.GroupSize}");

                    // Eğer işçinin kapasitesi dolduysa (yani 0'a ulaştıysa), işçiyi de oyundan kaldır.
                    if (passenger.GroupSize <= 0)
                    {
                        StopManager.Instance.FreeStop(stopIndex);
                        passenger.gameObject.SetActive(false);
                        Debug.Log($"<color=green>İŞÇİ GÖREVİ TAMAMLADI:</color> {passenger.name} işçisi kapasitesi dolduğu için kaldırıldı.");
                    }
                    
                    // Bir eşleşme bulunduktan sonra bu frame için işlemi bitir.
                    return; 
                }
