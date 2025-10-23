using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideAllPanelButton : MonoBehaviour
{
    private PanelManager _panelManager;

    private void Start()
    {
        _panelManager = PanelManager.Instance;
    }
    public void DoHidePanel()
    {
        _panelManager.HideAllPanel();

    }
}
