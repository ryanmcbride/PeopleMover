using UnityEngine;
using UnityEngine.EventSystems;
using System.Globalization;
using System;
using Paroxe.SuperCalendar.Internal;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Paroxe.SuperCalendar
{
    public class Calendar : UIBehaviour
    {
        [Serializable]
        public class DateTimeChangedEvent : UnityEvent { }

        [FormerlySerializedAs("onDateChanged")]
        [SerializeField]
        private DateTimeChangedEvent m_OnDateTimeChanged = new DateTimeChangedEvent();

        public DateTimeChangedEvent onDateTimeChanged
        {
            get { return m_OnDateTimeChanged; }
            set { m_OnDateTimeChanged = value; }
        }

        public static Calendar Current
        {
            get { return s_Current; }
        }

        public delegate void DateTimeChangedHandler(DateTime dateTime);
        public delegate void SerializableDateTimeChangedHandler(SerializableDateTime serializableDataTime);

        public event DateTimeChangedHandler DateTimeChanged;
        public event SerializableDateTimeChangedHandler SerializableDateTimeChanged;

        public string LocalizedHourTerm = "Hour";
        public string LocalizedMinuteTerm = "Minute";

        public enum SelectionModeType
        {
            DateOnly,
            TimeOnly,
            DateAndTime
        };

        public bool AnimatePanels
        {
            get { return m_AnimatePanels; }
            set { m_AnimatePanels = value; }
        }

        public bool AnimateOpen
        {
            get { return m_AnimateOpen; }
            set { m_AnimateOpen = value; }
        }

        public int YearsRangeBegin
        {
            get
            {
                ComputeYearsRange();
                return m_YearsRangeBegin; 
            } 
        }

        public Font Font
        {
            get { return m_Font; }
            set
            {
                if (value != m_Font)
                {
                    m_Font = value;

                    Text[] texts = GetComponentsInChildren<Text>();
                    foreach (Text text in texts)
                    {
                        text.font = m_Font;

#if UNITY_EDITOR
                        if (!EditorApplication.isPlaying)
                            EditorUtility.SetDirty(text);
#endif
                    }
                }
            }
        }

        public SelectionModeType SelectionMode
        {
            get { return m_SelectionMode; }
            set
            {
                if (value != m_SelectionMode)
                {
                    m_SelectionMode = value;

                    if (m_SelectionMode == SelectionModeType.TimeOnly)
                    {

                        m_ClanedarTimePicker.transform.SetAsLastSibling();

                        m_ClanedarTimePicker.GetComponent<CanvasGroup>().alpha = 1.0f;
                        m_ClanedarTimePicker.GetComponent<CanvasGroup>().interactable = true;
                        m_ClanedarTimePicker.GetComponent<CanvasGroup>().blocksRaycasts = true;

                        m_CalendarYearPicker.GetComponent<CanvasGroup>().alpha = 0.0f;
                    }
                    else
                    {
                        m_ClanedarTimePicker.transform.SetAsFirstSibling();

                        m_ClanedarTimePicker.GetComponent<CanvasGroup>().alpha = 0.0f;
                        m_ClanedarTimePicker.GetComponent<CanvasGroup>().interactable = false;
                        m_ClanedarTimePicker.GetComponent<CanvasGroup>().blocksRaycasts = false;

                        m_CalendarYearPicker.GetComponent<CanvasGroup>().alpha = 1.0f;
                    }

                    
                    NotifyDateTimeChanged();
                }
            }
        }



        public SerializableDateTime SerializableDateTime
        {
            get { return m_SerializableDateTime; }
            set
            {
                if (value.DateTime != m_SerializableDateTime.DateTime)
                {
                    m_SerializableDateTime = value;

                    NotifyDateTimeChanged();
                }
            }
        }

        public DateTime DateTime
        {
            get { return m_SerializableDateTime.DateTime; }
            set
            {
                if (value != m_SerializableDateTime.DateTime)
                {
                    m_SerializableDateTime.DateTime = value;

                    NotifyDateTimeChanged();
                }
            }
        }

        public string CultureInfoName
        {
            get { return m_CultureInfoName; }
            set
            {
                if (value != m_CultureInfoName)
                {
                    m_UseCurrentHostCulture = false;

                    m_CultureInfoName = value;
                    m_CultureInfo = new CultureInfo(m_CultureInfoName);

                    OnCultureInfoChanged();
                }
            }
        }

        public CultureInfo CultureInfo
        {
            get { return m_CultureInfo; }
            set
            {
                if (!Equals(value, m_CultureInfo))
                {
                    m_UseCurrentHostCulture = false;

                    m_CultureInfoName = m_CultureInfo.Name;
                    m_CultureInfo = value;

                    OnCultureInfoChanged();
                }
            }
        }

        public bool UseCurrentHostCulture
        {
            get
            {
                return m_UseCurrentHostCulture;
            }
            set
            {
                if (value != m_UseCurrentHostCulture)
                {
                    m_UseCurrentHostCulture = value;

                    if (m_UseCurrentHostCulture)
                    {
                        m_CultureInfoName = CultureInfo.CurrentCulture.Name;
                        m_CultureInfo = CultureInfo.CurrentCulture;

                        OnCultureInfoChanged();
                    }
                }
            }
        }

        public bool UseNowDateTime
        {
            get { return m_UseNowDateTime; }
            set
            {
                if (value != m_UseNowDateTime)
                {
                    m_UseNowDateTime = value;
                }
            }
        }

        public bool KeepSelected
        {
            get { return m_KeepSelected; }
            set { m_KeepSelected = value; }
        }

        public bool Opened
        {
            get { return m_Opened; }
            set
            {
                if (value != m_Opened)
                {
                    m_Opened = value;
                    m_ContentTransform.localScale = new Vector3(1.0f, m_Opened ? 1.0f : 0.0f, 1.0f);
                    m_AnimationPosition = 1.0f;
                    m_OpenerImage.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, m_Opened ? 90.0f : -90.0f);
                }
            }
        }

        #region PRIVATE
        internal enum SelectionStateType
        {
            None = 0,
            YearSelected = 1,
            Year = 2,
            MonthSelected = 3,
            Month = 4,
            DaySelected = 5,
            Day = 6,
            Time = 7
        };

        internal SelectionStateType SelectionState
        {
            get { return m_SelectionState; }
            set
            {
                if (value != m_SelectionState)
                {
                    m_SelectionState = value;

                    NotifyDateTimeChanged();
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            SetupCultureInfo();

            if (m_UseNowDateTime)
                m_SerializableDateTime.DateTime = DateTime.Now;

            if (SelectionMode == SelectionModeType.TimeOnly)
            {
                m_Panels[m_CurrentPanel].HideImmediate();
                m_CurrentPanel = 0;
                m_CurrentCalendarBottomContent = m_Panels[m_CurrentPanel];
                m_Panels[m_CurrentPanel].ShowImmediate();
            }

            NotifyDateTimeChanged();
            OnCultureInfoChanged();

#if UNITY_EDITOR
            m_BannerIsOpened = !!m_BannerIsOpened;
#endif
        }

        private void SetupCultureInfo()
        {
            if (m_UseCurrentHostCulture)
            {
                m_CultureInfo = CultureInfo.CurrentCulture;
                m_CultureInfoName = m_CultureInfo.Name;
            }
            else
            {
                m_CultureInfo = new CultureInfo(m_CultureInfoName);
                m_CultureInfoName = m_CultureInfo.Name;
            }
        }

        public void OnHourEdited()
        {
            if (string.IsNullOrEmpty(m_HourInput.text))
            {
                m_HourInput.text = DateTime.Hour.ToString();
                return;
            }

            TimeSpan ts = new TimeSpan(0, int.Parse(m_HourInput.text), DateTime.Minute, DateTime.Second, DateTime.Millisecond);
            DateTime = DateTime.Date + ts;
        }

        public void OnMinuteEdited()
        {
            if (string.IsNullOrEmpty(m_MinuteInput.text))
            {
                m_HourInput.text = DateTime.Minute.ToString();
                return;
            }

            TimeSpan ts = new TimeSpan(0, DateTime.Hour, int.Parse(m_MinuteInput.text), DateTime.Second, DateTime.Millisecond);
            DateTime = DateTime.Date + ts;
        }

        public void OnHourPlusButtonClicked()
        {
            DateTime = DateTime.AddHours(1);
        }

        public void OnHourMinusButtonClicked()
        {
            DateTime = DateTime.AddHours(-1);
        }

        public void OnMinutePlusButtonClicked()
        {
            DateTime = DateTime.AddMinutes(1.0f);
        }

        public void OnMinuteMinusButtonClicked()
        {
            DateTime = DateTime.AddMinutes(-1.0f);
        }

        public void OnAMButtonClicked()
        {
            if (m_CultureInfo.DateTimeFormat.AMDesignator != "")
            {
                if (DateTime.Hour < 12)
                    DateTime = DateTime.AddHours(12);
                else
                    DateTime = DateTime.AddHours(-12);
            }
        }

        private void NotifyDateTimeChanged()
        {
            if (m_CultureInfo == null)
                SetupCultureInfo();

            if (m_CultureInfo.DateTimeFormat.AMDesignator != "")
            {
                if (DateTime.Hour < 12)
                {
                    m_HourInput.text = DateTime.Hour.ToString();
                    m_AMButtonText.text = m_CultureInfo.DateTimeFormat.AMDesignator;
                }
                else
                {
                    m_HourInput.text = (DateTime.Hour - 12).ToString();
                    m_AMButtonText.text = m_CultureInfo.DateTimeFormat.PMDesignator;
                }
            }
            else
                m_HourInput.text = DateTime.Hour.ToString();

            m_MinuteInput.text = DateTime.Minute.ToString("00");

            if (m_SelectionMode != SelectionModeType.TimeOnly)
            {
                switch (m_SelectionState)
                {
                    case SelectionStateType.Day:
                        m_DateTitleText.text = DateTime.ToString(m_CultureInfo.DateTimeFormat.LongDatePattern, m_CultureInfo);
                        break;
                    case SelectionStateType.DaySelected:
                    case SelectionStateType.Month:
                        if (m_KeepSelected)
                            m_DateTitleText.text = DateTime.ToString(m_CultureInfo.DateTimeFormat.LongDatePattern, m_CultureInfo);
                        else
                            m_DateTitleText.text = DateTime.ToString(m_CultureInfo.DateTimeFormat.YearMonthPattern, m_CultureInfo);
                        break;
                    case SelectionStateType.MonthSelected:
                    case SelectionStateType.Year:
                        m_DateTitleText.text = DateTime.Year.ToString();
                        break;
                    case SelectionStateType.YearSelected:
                    case SelectionStateType.None:
                        m_DateTitleText.text = YearsRangeBegin + " - " + (YearsRangeBegin + 15);
                        break;
                }
            }
            else
            {
                m_DateTitleText.text = DateTime.ToString(m_CultureInfo.DateTimeFormat.LongTimePattern);
            }

            switch (m_SelectionMode)
            {
                case SelectionModeType.TimeOnly:
                    m_ButtonText.text = DateTime.ToString(m_CultureInfo.DateTimeFormat.LongTimePattern, m_CultureInfo);
                    m_DateTimeImage.sprite = m_TimeSprite;
                    break;
                case SelectionModeType.DateOnly:
                    m_ButtonText.text = DateTime.ToString(m_CultureInfo.DateTimeFormat.LongDatePattern, m_CultureInfo);
                    m_DateTimeImage.sprite = m_DateSprite;
                    break;
                case SelectionModeType.DateAndTime:
                    m_ButtonText.text = DateTime.ToString(m_CultureInfo.DateTimeFormat.FullDateTimePattern, m_CultureInfo);
                    m_DateTimeImage.sprite = m_DateSprite;
                    break;
            }


            m_NextButton.gameObject.SetActive(m_SelectionMode != SelectionModeType.TimeOnly);
            m_PreviousButton.gameObject.SetActive(m_SelectionMode != SelectionModeType.TimeOnly);

            m_CalendarDayPicker.SetupCellsLayout(DateTime);
            m_CalendarMonthPicker.SetupCellsLayout(DateTime);
            m_CalendarYearPicker.SetupCellsLayout(DateTime);

            if (DateTime != m_PreviousNotificationDateTime)
            {
                s_Current = this;

                if (DateTimeChanged != null)
                    DateTimeChanged(DateTime);

                if (SerializableDateTimeChanged != null)
                    SerializableDateTimeChanged(SerializableDateTime);

                onDateTimeChanged.Invoke();

                m_PreviousNotificationDateTime = DateTime;
            }
        }

        private void ComputeYearsRange()
        {
            m_YearsRangeBegin = (DateTime.Year - DateTime.Year % 16) + m_YearsRangeOffset;
        }

        private void OnCultureInfoChanged()
        {
            for (int i = 0; i < 7; ++i)
            {
                m_WeekDaysLabel[i].text = m_CultureInfo.DateTimeFormat.AbbreviatedDayNames[i].ToUpper();
            }

            m_HourLabel.text = LocalizedHourTerm;
            m_MinuteLabel.text = LocalizedMinuteTerm;

            if (m_CultureInfo.DateTimeFormat.AMDesignator != "")
            {
                m_AMButton.gameObject.SetActive(true);
            }
            else
            {
                m_AMButton.gameObject.SetActive(false);
            }

            NotifyDateTimeChanged();

            m_CalendarDayPicker.SetupCellsCulture(m_CultureInfo);
            m_CalendarMonthPicker.SetupCellsCulture(m_CultureInfo);
            m_CalendarYearPicker.SetupCellsCulture(m_CultureInfo);
        }

        public void OnYearSelected()
        {
            GoDown();

            SelectionState = SelectionStateType.Year;

            m_YearsRangeOffset = 0;
        }

        public void OnMonthSelected()
        {
            GoDown();

            SelectionState = SelectionStateType.Month;
        }

        public void OnDaySelected()
        {
            if (SelectionMode != SelectionModeType.DateOnly)
                GoDown();

            SelectionState = SelectionStateType.Day;
        }

        public void OnDateTitleButtonClicked()
        {
            if (SelectionMode == SelectionModeType.TimeOnly)
                return;
            
            if (m_CurrentCalendarBottomContent == m_ClanedarTimePicker)
            {
                SelectionState = SelectionStateType.DaySelected;
                GoUp();
            }
            else if (m_CurrentCalendarBottomContent == m_CalendarDayPicker.GetComponent<CalendarBottomContent>())
            {
                SelectionState = SelectionStateType.MonthSelected;
                GoUp();
            }
            else if (m_CurrentCalendarBottomContent == m_CalendarMonthPicker.GetComponent<CalendarBottomContent>())
            {
                SelectionState = SelectionStateType.YearSelected;
                GoUp();
            }
        }

        public void OnNextButtonCliked()
        {
            OnNextOrPreviousButtonClicked(1);
        }

        public void OnPreviousButtonCliked()
        {
            OnNextOrPreviousButtonClicked(-1);
        }

        public void Toggle()
        {
            m_Opened = !m_Opened;
            m_AnimationPosition = 0.0f;
            m_OpenerImage.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, m_Opened ? 90.0f : -90.0f);
        }

        float CubicEaseOut(float currentTime, float startingValue, float finalValue, float duration)
        {
            return finalValue * ((currentTime = currentTime / duration - 1) * currentTime * currentTime + 1) + startingValue;
        }

        float CubicEaseIn(float currentTime, float startingValue, float finalValue, float duration)
        {
            return finalValue * (currentTime /= duration) * currentTime * currentTime + startingValue;
        }

        void Update()
        {
            if (m_AnimationPosition != 1.0f)
            {
                if (m_AnimateOpen)
                    m_AnimationPosition = Mathf.Clamp01(m_AnimationPosition + Time.deltaTime / m_AnimationDuration);
                else
                    m_AnimationPosition = 1.0f;

                if (m_Opened)
                {
                    
                    float ease = CubicEaseOut(m_AnimationPosition, 0.0f, 1.0f, 1.0f);

                    m_ContentTransform.localScale = new Vector3(1.0f, ease, 1.0f);

                    m_ContentCanvasGroup.alpha = m_AnimationPosition;
                }
                else
                {
                    float ease = CubicEaseIn(1.0f - m_AnimationPosition, 0.0f, 1.0f, 1.0f);

                    m_ContentTransform.localScale = new Vector3(1.0f, ease, 1.0f);

                    m_ContentCanvasGroup.alpha = 1.0f - Mathf.Clamp01(m_AnimationPosition * 1.05f);
                }
            }
        }

        private void OnNextOrPreviousButtonClicked(int rel)
        {
            

            switch (m_SelectionState)
            {
                case SelectionStateType.Day:
                case SelectionStateType.DaySelected:
                case SelectionStateType.Month:
                    DateTime = new DateTime(
                        DateTime.AddMonths(rel).Year,
                        DateTime.AddMonths(rel).Month,
                        DateTime.AddMonths(rel).Day,
                        DateTime.AddMonths(rel).Hour,
                        DateTime.AddMonths(rel).Minute,
                        DateTime.AddMonths(rel).Second,
                        DateTime.AddMonths(rel).Millisecond,
                        DateTime.AddMonths(rel).Kind);
                    break;
                case SelectionStateType.MonthSelected:
                case SelectionStateType.Year:
                    DateTime = new DateTime(
                        DateTime.AddYears(rel).Year,
                        DateTime.AddYears(rel).Month,
                        DateTime.AddYears(rel).Day,
                        DateTime.AddYears(rel).Hour,
                        DateTime.AddYears(rel).Minute,
                        DateTime.AddYears(rel).Second,
                        DateTime.AddYears(rel).Millisecond,
                        DateTime.AddYears(rel).Kind);
                    break;
                case SelectionStateType.YearSelected:
                case SelectionStateType.None:
                    m_YearsRangeOffset += rel*4;
                    NotifyDateTimeChanged();
                    break;
            }
        }

        private void GoUp()
        {
            m_Panels[m_CurrentPanel].Hide(true, 0.0f);
            m_Panels[(m_CurrentPanel + 1)%m_Panels.Length].Show(false, m_Delay);

            m_CurrentPanel++;
            m_CurrentPanel %= m_Panels.Length;

            m_CurrentCalendarBottomContent = m_Panels[m_CurrentPanel];
        }

        private void GoDown()
        {
            m_Panels[m_CurrentPanel - 1 < 0 ? m_Panels.Length - 1 : m_CurrentPanel - 1].Show(true, m_Delay);
            m_Panels[m_CurrentPanel].Hide(false, 0.0f);

            m_CurrentPanel--;
            if (m_CurrentPanel < 0)
                m_CurrentPanel = m_Panels.Length - 1;
            else
                m_CurrentPanel %= m_Panels.Length;

            m_CurrentCalendarBottomContent = m_Panels[m_CurrentPanel];
        }

        void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
                GoUp();
            else if (Input.GetKeyDown(KeyCode.DownArrow))
                GoDown();
        }

        [SerializeField]
        private CalendarBottomContent m_ClanedarTimePicker;
        [SerializeField]
        private CalendarDayPicker m_CalendarDayPicker;
        [SerializeField]
        private CalendarMonthPicker m_CalendarMonthPicker;
        [SerializeField]
        private CalendarYearPicker m_CalendarYearPicker;
        [SerializeField]
        internal CalendarBottomContent[] m_Panels;
        [SerializeField]
        private float m_Delay = 0.50f;
        [SerializeField]
        internal CalendarBottomContent m_CurrentCalendarBottomContent;
        [SerializeField]
        private CalendarBottomTransform m_Content;
        [SerializeField]
        private RectTransform m_ContentTransform;
        [SerializeField]
        private CanvasGroup m_ContentCanvasGroup;
        [SerializeField]
        private Text[] m_WeekDaysLabel;
        [SerializeField]
        private Text m_DateTitleText;
        [SerializeField]
        private RectTransform m_AMButton;
        [SerializeField]
        private Text m_AMButtonText;
        [SerializeField]
        private InputField m_HourInput;
        [SerializeField]
        private InputField m_MinuteInput;
        [SerializeField]
        private Text m_HourLabel;
        [SerializeField]
        private Text m_MinuteLabel;
        [SerializeField]
        private Text m_ButtonText;
        [SerializeField]
        private Sprite m_DateSprite;
        [SerializeField]
        private Sprite m_TimeSprite;
        [SerializeField]
        private Image m_DateTimeImage;
        [SerializeField]
        private Image m_OpenerImage;
        [SerializeField]
        private Button m_PreviousButton;
        [SerializeField]
        private Button m_NextButton;

        [SerializeField]
        private Font m_Font;
        [SerializeField]
        private SelectionStateType m_SelectionState = SelectionStateType.None;
        [SerializeField]
        private SerializableDateTime m_SerializableDateTime = new SerializableDateTime(new DateTime(2015, 8, 5));
        [SerializeField]
        private bool m_UseCurrentHostCulture;
        private int m_CurrentPanel = 3;
        [SerializeField]
        private string m_CultureInfoName;
        private CultureInfo m_CultureInfo;
        [SerializeField]
        private SelectionModeType m_SelectionMode;
        [SerializeField]
        private bool m_UseNowDateTime;
        [SerializeField]
        private bool m_KeepSelected;
        private int m_YearsRangeBegin;
        private int m_YearsRangeOffset;

        [SerializeField]
        private float m_AnimationPosition = 0.0f;
        private float m_AnimationDuration = 0.2f;
        [SerializeField]
        private bool m_Opened;
        [SerializeField]
        private bool m_AnimatePanels = true;

        [SerializeField]
        private bool m_AnimateOpen = true;

#if UNITY_EDITOR
        [SerializeField]
        internal bool m_BannerIsOpened = true;
#endif

        private static Calendar s_Current;
        private DateTime m_PreviousNotificationDateTime;

        #endregion

    }
}