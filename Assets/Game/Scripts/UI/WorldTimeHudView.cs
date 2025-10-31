using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using SevenCrowns.Systems;

namespace SevenCrowns.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldTimeHudView : MonoBehaviour
    {
        private const string DefaultTable = "UI.Common";
        private const string DefaultDayEntry = "WorldTime.DayLabel";
        private const string DefaultWeekEntry = "WorldTime.WeekLabel";
        private const string DefaultMonthEntry = "WorldTime.MonthLabel";

        [Header("Time Service")]
        [SerializeField] private MonoBehaviour _timeServiceBehaviour;

        [Header("Values")]
        [SerializeField] private TextMeshProUGUI _dayValue;
        [SerializeField] private TextMeshProUGUI _weekValue;
        [SerializeField] private TextMeshProUGUI _monthValue;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI _dayLabel;
        [SerializeField] private LocalizedString _dayLabelEntry;
        [SerializeField] private TextMeshProUGUI _weekLabel;
        [SerializeField] private LocalizedString _weekLabelEntry;
        [SerializeField] private TextMeshProUGUI _monthLabel;
        [SerializeField] private LocalizedString _monthLabelEntry;

        private IWorldTimeService _timeService;
        private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        private LocalizedString.ChangeHandler _dayLabelHandler;
        private LocalizedString.ChangeHandler _weekLabelHandler;
        private LocalizedString.ChangeHandler _monthLabelHandler;

        private void Awake()
        {
            ResolveTimeService();
            HookLabel(ref _dayLabelEntry, _dayLabel, ref _dayLabelHandler, DefaultDayEntry);
            HookLabel(ref _weekLabelEntry, _weekLabel, ref _weekLabelHandler, DefaultWeekEntry);
            HookLabel(ref _monthLabelEntry, _monthLabel, ref _monthLabelHandler, DefaultMonthEntry);
        }

        private void OnEnable()
        {
            if (_timeService != null)
            {
                // ok
            }
            else
            {
                ResolveTimeService();
            }
            if (_timeService != null)
            {
                _timeService.DateChanged += OnDateChanged;
                OnDateChanged(_timeService.CurrentDate);
            }

            RefreshLabels();
        }

        private void OnDisable()
        {
            if (_timeService != null)
            {
                _timeService.DateChanged -= OnDateChanged;
            }
        }

        private void OnDestroy()
        {
            UnhookLabel(_dayLabelEntry, ref _dayLabelHandler);
            UnhookLabel(_weekLabelEntry, ref _weekLabelHandler);
            UnhookLabel(_monthLabelEntry, ref _monthLabelHandler);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDefaults(ref _dayLabelEntry, DefaultDayEntry);
            EnsureDefaults(ref _weekLabelEntry, DefaultWeekEntry);
            EnsureDefaults(ref _monthLabelEntry, DefaultMonthEntry);
        }
#endif

        private void ResolveTimeService()
        {
            if (_timeServiceBehaviour != null)
            {
                if (_timeServiceBehaviour is IWorldTimeService service)
                {
                    _timeService = service;
                }
                else
                {
                    Debug.LogError("Assigned time service must implement IWorldTimeService.", this);
                }
            }

            if (_timeService != null) return;

            var behaviours = FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IWorldTimeService candidate)
                {
                    _timeService = candidate;
                    break;
                }
            }
        }

        private void OnDateChanged(WorldDate value)
        {
            if (_dayValue != null)
            {
                _dayValue.text = value.Day.ToString(_culture);
            }

            if (_weekValue != null)
            {
                _weekValue.text = value.Week.ToString(_culture);
            }

            if (_monthValue != null)
            {
                _monthValue.text = value.Month.ToString(_culture);
            }
        }

        private void HookLabel(ref LocalizedString entry, TextMeshProUGUI target, ref LocalizedString.ChangeHandler handler, string defaultEntry)
        {
            if (target == null) return;

            if (string.IsNullOrEmpty(entry.TableReference.TableCollectionName))
            {
                entry.TableReference = DefaultTable;
            }

            if (string.IsNullOrEmpty(entry.TableEntryReference))
            {
                entry.TableEntryReference = defaultEntry;
            }

            handler = value =>
            {
                if (target != null)
                {
                    target.text = value;
                }
            };
            entry.StringChanged += handler;
        }

        private void UnhookLabel(LocalizedString entry, ref LocalizedString.ChangeHandler handler)
        {
            if (handler == null) return;
            entry.StringChanged -= handler;
            handler = null;
        }

        private void RefreshLabels()
        {
            _dayLabelEntry.RefreshString();
            _weekLabelEntry.RefreshString();
            _monthLabelEntry.RefreshString();
        }

#if UNITY_EDITOR
        private static void EnsureDefaults(ref LocalizedString entry, string defaultEntry)
        {
            if (string.IsNullOrEmpty(entry.TableReference.TableCollectionName))
            {
                entry.TableReference = DefaultTable;
            }

            if (string.IsNullOrEmpty(entry.TableEntryReference))
            {
                entry.TableEntryReference = defaultEntry;
            }
        }
#endif
    }
}
