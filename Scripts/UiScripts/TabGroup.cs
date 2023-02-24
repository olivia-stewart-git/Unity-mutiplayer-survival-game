using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TabGroup : MonoBehaviour
{
    public List<TabButtonUi> tabButtons;

    public Color unselectedColor;
    public Color selectedColor;
    public Color hoverColor;

    public TabButtonUi defaultSelected;
    [HideInInspector]
    public TabButtonUi selectedTab;

    public UnityEvent onTabChanged;

    private void Start()
    {
        if(defaultSelected != null)
        {
            OnTabSelected(defaultSelected);
        }
    }

    public void Subscribe(TabButtonUi button)
    {
        if(tabButtons == null)
        {
            tabButtons = new List<TabButtonUi>();
        }

        tabButtons.Add(button);
    }

    public void OnTabEnter(TabButtonUi button)
    {
        ResetTabs();
        if (selectedTab == null || button != selectedTab)
        {
            button.background.color = hoverColor;
        }
    }

    public void OnTabExit(TabButtonUi button)   
    {
        ResetTabs();
    }

    public void OnTabSelected(TabButtonUi button)
    {
        if(button != selectedTab && onTabChanged != null)
        {
            onTabChanged.Invoke();
        }

        if(selectedTab != null && selectedTab != button)
        {
            selectedTab.Deselect();
        }

        selectedTab = button;

        selectedTab.Select();

        ResetTabs();
        button.background.color = selectedColor;

        foreach (TabButtonUi tb in tabButtons)
        {
            if(tb == selectedTab)
            {
                tb.assignedObject.SetActive(true);
            }
            else
            {
                tb.assignedObject.SetActive(false);
            }
        }
    }

    public void ResetTabs()
    {
        foreach (TabButtonUi button in tabButtons)
        {
            if (selectedTab != null && button != selectedTab)
            {
                button.background.color = unselectedColor;
            }
        }
    }
}
