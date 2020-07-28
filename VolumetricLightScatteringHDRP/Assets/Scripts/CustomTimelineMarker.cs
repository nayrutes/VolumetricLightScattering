using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class CustomTimelineMarker : Marker, INotification, INotificationOptionProvider
{
    [SerializeField] private string message = "";
    [SerializeField] private bool retroactive = false;
    [SerializeField] private bool emitOnce = false;
    [SerializeField] private bool triggerinEditMode = true;
    private TrackAsset ta;
    public int index;
    public PropertyName id => new PropertyName();
    public string Message => message;

    public NotificationFlags flags =>
        (retroactive ? NotificationFlags.Retroactive : default) |
        (emitOnce ? NotificationFlags.TriggerOnce : default) |
        (triggerinEditMode ? NotificationFlags.TriggerInEditMode : default);

    public override void OnInitialize(TrackAsset aPent)
    {
        ta = aPent;
        base.OnInitialize(aPent);
        Setindex();
    }

    public void Setindex()
    {
        if(ta!=null)
            index = ta.GetMarkerCount()-1;
    }
    public void Setindex(int i)
    {
        index = i;
    }
}
