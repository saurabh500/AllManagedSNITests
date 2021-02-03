using System;
using System.Diagnostics.Tracing;
using System.Linq;

namespace UnknownException459
{
    class SampleListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventsource)
        {
            if (eventsource.Name.Equals("Microsoft.Data.SqlClient.EventSource"))
            {
                EnableEvents(eventsource, EventLevel.LogAlways, EventKeywords.All);
            }
        }
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {

            // report all event information 
            Console.Write(" Event {0} ", eventData.EventName);

            // Don't display activity information, as that's not used in the demos 
            // Out.Write(" (activity {0}{1}) ", ShortGuid(eventData.ActivityId),  
            // eventData.RelatedActivityId != Guid.Empty ? "->" + ShortGuid(eventData.RelatedActivityId) : ""); 

            // Events can have formatting strings 'the Message property on the 'Event' attribute.  
            // If the event has a formatted message, print that, otherwise print out argument values.  
            if (eventData.Message != null)
                Console.WriteLine(eventData.Message, eventData.Payload.ToArray());
            else
            {
                string[] sargs = eventData.Payload != null ? eventData.Payload.Select(o => o.ToString()).ToArray() : null;
                Console.WriteLine("({0}).", sargs != null ? string.Join(", ", sargs) : "");
            }
        }
    }
}