using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Popup : Menu
{
    public struct Outcome
    {
        public UnityEvent Event;
        public string displayName;
    }

    //Config
    [SerializeField] List<Outcome> outcomes = new();
    [SerializeField] int closeOutcomeIndex = 0;
    //[SerializeField] int escapeOutcomeIndex = -1;

    //Data
    //int selectedOutcomeIndex = -1;
    List<Button> outcomeButtons = new(); //4 by default

    public void TriggerOutcome(int index)
    {
        if (index < 0 || index >= outcomes.Count) return;
        outcomes[index].Event?.Invoke();
    }


    //public void TriggerEscape()
    //{
    //    if (selectedOutcomeIndex != escapeOutcomeIndex) selectedOutcomeIndex = escapeOutcomeIndex;
    //    else TriggerOutcome(escapeOutcomeIndex);
    //}



    public static Popup Begin()
    {
        Popup popup = new(); //Placeholder implementation. Instantiate at some point.

        popup.outcomeButtons.Clear();
        //popup.escapeOutcomeIndex = -1;
        //popup.selectedOutcomeIndex = -1;
        foreach (Button button in popup.outcomeButtons) button.gameObject.SetActive(false);

        return popup;
    }
    public Popup AddOutcome(string displayName, UnityEvent outcomeEvent)
    {
        outcomes.Add(new Outcome { displayName = displayName, Event = outcomeEvent });
        if (outcomes.Count > outcomeButtons.Count)
        {
            outcomeButtons.Add(Instantiate(outcomeButtons[0], outcomeButtons[0].transform.parent));
        }
        outcomeButtons[outcomes.Count - 1].gameObject.SetActive(true);
        outcomeButtons[outcomes.Count - 1].GetComponentInChildren<Text>().text = displayName;
        return this;
    }
    //public Popup SetCloseOutcome(int index)
    //{
    //    closeOutcomeIndex = index;
    //    return this;
    //}
    public Popup Show()
    {
        if (closeOutcomeIndex == -1) closeOutcomeIndex = outcomes.Count - 1;

        Menu.Open(this);
        return this;
    }


}
