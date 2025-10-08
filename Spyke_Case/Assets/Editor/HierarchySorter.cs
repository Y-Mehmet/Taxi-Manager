using UnityEngine;
using UnityEditor; // Editör scriptleri için bu kütüphane gerekli
using System.Collections.Generic;
using System.Linq; // Sıralama için LINQ kullanacağız

public class HierarchySorter
{
    // Bu satır, GameObject menüsüne ve sağ tık menüsüne yeni bir seçenek ekler
    [MenuItem("GameObject/Çocukları Sırala (İsme Göre - Artan)")]
    private static void SortChildrenByNameAscending()
    {
        // Seçili olan ana objeyi al
        GameObject parent = Selection.activeGameObject;

        // Eğer hiçbir obje seçili değilse veya seçili objenin çocuğu yoksa uyarı ver ve çık
        if (parent == null || parent.transform.childCount == 0)
        {
            Debug.LogWarning("Lütfen çocukları olan bir GameObject seçin.");
            return;
        }

        // Bütün çocukların transformlarını bir listeye al
        List<Transform> children = new List<Transform>();
        foreach (Transform child in parent.transform)
        {
            children.Add(child);
        }

        // Listeyi, objelerin isimlerini sayıya çevirerek küçükten büyüğe sırala
        List<Transform> sortedChildren = children.OrderBy(child =>
        {
            // İsimleri sayıya çevirmeye çalış, eğer sayı değilse 0 kabul et (hata vermemesi için)
            int.TryParse(child.name, out int number);
            return number;
        }).ToList();

        // Sıralanmış listeye göre hiyerarşideki yerlerini güncelle
        for (int i = 0; i < sortedChildren.Count; i++)
        {
            sortedChildren[i].SetSiblingIndex(i);
        }

        Debug.Log(parent.name + " isimli objenin çocukları başarıyla sıralandı!");
    }
}