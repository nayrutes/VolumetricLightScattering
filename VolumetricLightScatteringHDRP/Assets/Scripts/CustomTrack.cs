using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackBindingType(typeof(TimelineController))]
public class CustomTrack : MarkerTrack
{
   private List<IMarker> getMarkers;

   public override IEnumerable<PlayableBinding> outputs
   {
      get
      {
//         getMarkers = GetMarkers().ToList();
//         for(int i=0;i<getMarkers.Count;i++)
//         {
//            var m = getMarkers[i] as CustomTimelineMarker;
//            m.Setindex(i);
//         }

         return base.outputs;
         
      }
   }
}
