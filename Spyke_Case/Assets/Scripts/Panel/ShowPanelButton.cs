using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowPanelButton : MonoBehaviour
{
    public PanelID panelID;
    public PanelShowBehavior behavior;
    private PanelManager _panelManager;
    
    private void Start()
    {
        _panelManager = PanelManager.Instance;
        
    }
    public void DoShowPanel()
    {

        _panelManager.ShowPanel(panelID, behavior);
        Debug.Log("Panel Showed: " + panelID);

    }
    public void DoShowPanelWhitId(PanelID panelID)
    {
     
        _panelManager.ShowPanel(panelID, behavior);
       
    }
    
}
