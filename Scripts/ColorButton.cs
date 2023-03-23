using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class ColorButton : MonoBehaviour
{
    public int color = 0;
    [SerializeField] private Image colorImage;

    private void Awake()
    {
        // Add click event
        EventTrigger trigger = GetComponentInParent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerDown;
        entry.callback.AddListener(e => {
            GameManager.instance.player.ChangeColor(color);
        });
        trigger.triggers.Add(entry);

        // Register as theme image
        GameManager.instance.themeImages.Add(new ImageColor{
            image = colorImage,
            colorID = color
        });
    }
}