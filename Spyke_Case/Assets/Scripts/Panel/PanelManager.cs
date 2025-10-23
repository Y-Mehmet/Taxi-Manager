using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// PanelID enum is defined elsewhere
public class PanelManager : Singleton<PanelManager>
{
    // List that stores panel instances
    public List<PanelInstanceModel> _listInstance = new List<PanelInstanceModel>();
    

    // Reference to the object pool
    private ObjectPool _objectPool;

    private void Start()
    {
        // Get instance of the object pool
        _objectPool = ObjectPool.Instance;
        if (_objectPool == null)
        {
            Debug.LogError("ObjectPool instance is null. Make sure ObjectPool is initialized before PanelManager.");
        }
    }

    // Function to show a panel (updated to use enum)
    public void ShowPanel(PanelID panelID, PanelShowBehavior behavior = PanelShowBehavior.SHOW_PREVISE)
    {
       // Debug.LogWarning("panel�d " + panelID.ToString());

      bool isActive = false;
        if (_objectPool == null)
        {
            _objectPool = ObjectPool.Instance;
        }
        foreach (Transform childTransform in _objectPool.transform)
        {
            if (childTransform.name == panelID.ToString())
            {
                isActive = childTransform.gameObject.activeInHierarchy;
                break;
            }
        }

        if (isActive)
        {
            Debug.Log("panel allredy active "+ panelID.ToString());
        }else
        {
            // Convert enum to string to get panel
            string panelIDString = panelID.ToString();

            // Get panel instance from the object pool
            GameObject instancePanel = _objectPool.GetObjectFromPool(panelIDString);

            // If the panel object is found
            if (instancePanel != null)
            {
                // If the behavior is to hide the previous panel and there's at least one active panel
                if (behavior == PanelShowBehavior.HIDE_PREVISE && GetAmountPanelInList() > 0)
                {
                    var lastPanel = GetLastPanel();
                    if (lastPanel != null)
                    {
                        lastPanel.PanelInstance.SetActive(false);
                    }
                }

                // Add the new panel to the list
                _listInstance.Add(new PanelInstanceModel
                {
                    PanelID = panelID, // Enum stored as string
                    PanelInstance = instancePanel
                });
            }
            else
            {
                Debug.LogWarning($"Panel not found: {panelID}");
            }
        }
    }
    public void HidePanelWithPanelID(PanelID panelID, PanelShowBehavior panelShowBehavior=PanelShowBehavior.HIDE_PREVISE)
    {
        //Debug.LogWarning("panel�d hide last panel whit id  " + panelID.ToString());
        // Gizlenecek paneli listede bul. FirstOrDefault, bulamazsa null d�ner.
        PanelInstanceModel panelToHide = _listInstance.FirstOrDefault(p => p.PanelID == panelID);

        // Panel listede bulunamad�ysa uyar� ver ve i�lemi sonland�r.
        if (panelToHide == null)
        {
            // Debug.LogWarning($"Gizlenmeye �al���lan panel aktif listede bulunamad�: {panelID}");
            return;
        }

        // Gizlenecek panelin, listenin en sonundaki panel olup olmad���n� kontrol et.
        bool wasLastPanel = GetLastPanel() == panelToHide;

        // PaneliGameObject'ini ObjectPool'a geri g�nder.
        _objectPool.PoolObject(panelToHide.PanelInstance);
        // Paneli aktif panel listesinden kald�r.
        _listInstance.Remove(panelToHide);

        // E�er gizlenen panel son panel idiyse ve hala listede ba�ka paneller varsa,
        // yeni son paneli (bir �ncekini) aktif et.
        if (wasLastPanel && AnyPanelIsShowing()&& panelShowBehavior==PanelShowBehavior.SHOW_PREVISE)
        {
            var newLastPanel = GetLastPanel();
            if (newLastPanel != null && !newLastPanel.PanelInstance.activeInHierarchy)
            {
                newLastPanel.PanelInstance.SetActive(true);
            }
        }
    }

    // Function to hide the last panel
    public void HideLastPanel()
    {
        if (AnyPanelIsShowing())
        {
            var lastPanel = GetLastPanel();
           // Debug.LogWarning("panel�d hide last panel " + lastPanel.ToString());
            _listInstance.Remove(lastPanel);
            _objectPool.PoolObject(lastPanel.PanelInstance);

            // If there's still a panel left in the list, show it
            if (GetAmountPanelInList() > 0)
            {
                lastPanel = GetLastPanel();

                if (lastPanel != null && !lastPanel.PanelInstance.activeInHierarchy)
                {
                    lastPanel.PanelInstance.SetActive(true);
                }
            }
        }
        // else
        // Debug.LogWarning("No panel is currently open");
    }

    // Function to hide all panels
    public void HideAllPanel()
    {
      //  Debug.LogWarning("hide all panel");
        // Keep hiding panels until none are left
        while (AnyPanelIsShowing())
        {
            var lastPanel = GetLastPanel();
            _listInstance.Remove(lastPanel);
            _objectPool.PoolObject(lastPanel.PanelInstance);
        }
    }

    // Returns the last panel in the list
    PanelInstanceModel GetLastPanel()
    {
        if (_listInstance.Count == 0)
        {
            return null;
        }
        return _listInstance[_listInstance.Count - 1];
    }

    // Checks if any panel is currently showing
    public bool AnyPanelIsShowing()
    {
        return GetAmountPanelInList() > 0;
    }

    // Returns the number of panels currently showing
    public int GetAmountPanelInList()
    {
        return _listInstance.Count;
    }
}
