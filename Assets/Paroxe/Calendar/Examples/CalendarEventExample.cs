using UnityEngine;
using System.Collections;

public class CalendarEventExample : MonoBehaviour
{
    public void OnCalendarDateTimeChanged()
    {
        Debug.Log(Paroxe.SuperCalendar.Calendar.Current.DateTime);

        // OR
        // using namespace Paroxe.SuperCalendar;
        //..
        // Debug.Log(.Calendar.Current.DateTime);

    }
}
