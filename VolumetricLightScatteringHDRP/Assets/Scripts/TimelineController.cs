using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoboRyanTron.QuickButtons;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[ExecuteInEditMode]
public class TimelineController : MonoBehaviour, INotificationReceiver
{
   [SerializeField] private PlayableDirector playableDirector;
   [SerializeField] private int playIndex;
   //[FillList]
   [SerializeField] private List<AnimationSection> sections;
   [SerializeField] private TextChanger textChanger;
   
   private TimelineAsset timeline;
   private List<IMarker> markers;
   private int startIndex;
   private double? end;
   public QuickButton playButton = new QuickButton("Play");
   public QuickButton playNextButton = new QuickButton("PlayNext");


   private void PlayNext()
   {
      playIndex = (playIndex+1)%sections.Count;
      Play();
   }

   [ContextMenu("Play")]
   private void Play()
   {
      if(playIndex<0 || playIndex > sections.Count)
         return;
      textChanger.SetMyText(sections[playIndex].name);
      double start = 0;
      FindSection(playIndex, ref start);
      PlayPart(start);
   }
   private void FindSection(int index, ref double start)
   {
      startIndex = index;
      timeline = playableDirector.playableAsset as TimelineAsset;
      TrackAsset ta = timeline.GetOutputTracks().First(t => t.GetType() == typeof(CustomTrack));
      markers = ta.GetMarkers().ToList();
      int tmpIndex=0;
      foreach (var marker in markers)
      {
         if (marker is CustomTimelineMarker)
         {
            if (index == tmpIndex)
            {
               start = marker.time;
               end = markers.Count>index+1 ? markers[index + 1].time : timeline.duration;
               return;
            }

            tmpIndex++;
         }
      }
   }

   private void PlayPart(double start)
   {
      if (playableDirector.state == PlayState.Playing)
      {
         playableDirector.Pause();
         playableDirector.time = start;
         playableDirector.Resume();
      }
      else
      {
         playableDirector.time = start;
         playableDirector.Play();
      }
   }

   public void OnNotify(Playable origin, INotification notification, object context)
   {
      print("Notified, "+origin.ToString()+", "+notification.ToString()+", "+context?.ToString());
      if (!end.HasValue)
      {
         return;
      }

      if (notification is CustomTimelineMarker)
      {
         var ctm = notification as CustomTimelineMarker;
         
         if (startIndex == ctm.index)
            return;
         
         playableDirector.Pause();
      }
   }
}
