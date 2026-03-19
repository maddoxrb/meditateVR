using System;
using System.Collections.Generic;
using System.Reflection;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MeditationVR.Experiment
{
    [DisallowMultipleComponent]
    public sealed class MetaXRDropdownAdapter : MonoBehaviour
    {
        [Serializable]
        public sealed class SelectionChangedUnityEvent : UnityEvent<int>
        {
        }

        [Serializable]
        private sealed class OptionBinding
        {
            [Tooltip("Root object for this option. Assign the button, toggle, or Meta interactable object that should select the option.")]
            public GameObject optionObject;

            [Tooltip("Optional label object for this option. If not assigned, the adapter searches the option hierarchy for a text component.")]
            public GameObject labelObject;

            [Tooltip("Optional override text/value for this option.")]
            public string explicitText;
        }

        private sealed class PressRegistration
        {
            public Button button;
            public Toggle toggle;
            public InteractableUnityEventWrapper interactableWrapper;
            public PointableUnityEventWrapper pointableWrapper;
            public UnityAction buttonAction;
            public UnityAction<bool> toggleAction;
            public UnityAction interactableAction;
            public UnityAction<PointerEvent> pointableAction;
        }

        [Header("Menu Structure")]
        [Tooltip("Assign the trigger object that opens/closes the option list.")]
        [SerializeField]
        private GameObject triggerObject;

        [Tooltip("Assign the label object that displays the current selection. Text or TMP text is supported.")]
        [SerializeField]
        private GameObject selectedLabelObject;

        [Tooltip("Assign the list container object that shows/hides the option buttons.")]
        [SerializeField]
        private GameObject listContainerObject;

        [Tooltip("Optional CanvasGroup used to show/hide the list without relying only on active state.")]
        [SerializeField]
        private CanvasGroup listCanvasGroup;

        [Header("Options")]
        [Tooltip("Assign the option objects for this dropdown-like control. If left empty, direct children of the list container are used.")]
        [SerializeField]
        private List<OptionBinding> optionBindings = new List<OptionBinding>();

        [Header("Behavior")]
        [SerializeField]
        private bool startCollapsed = true;

        [SerializeField]
        private bool closeListOnSelection = true;

        [SerializeField]
        private int selectedIndex;

        [Header("Events")]
        [SerializeField]
        private SelectionChangedUnityEvent onValueChanged = new SelectionChangedUnityEvent();

        private readonly List<PressRegistration> optionRegistrations = new List<PressRegistration>();
        private readonly List<string> optionTexts = new List<string>();
        private PressRegistration triggerRegistration;
        private Toggle triggerToggle;
        private bool initialized;
        private bool listenersRegistered;

        public int Value => selectedIndex;
        public string SelectedText => GetOptionText(selectedIndex);
        public SelectionChangedUnityEvent OnValueChanged => onValueChanged;

        public event Action<int> ValueChanged;

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
            RegisterListeners();
        }

        private void OnDisable()
        {
            UnregisterListeners();
        }

        public void SetValueWithoutNotify(int index)
        {
            SetSelectedIndex(index, false);
        }

        public void SetOptions(IReadOnlyList<string> options)
        {
            EnsureOptionBindings();

            if (options == null)
            {
                RebuildOptionTexts();
                ApplySelectionToView();
                return;
            }

            int sharedCount = Mathf.Min(options.Count, optionBindings.Count);
            for (int i = 0; i < sharedCount; i++)
            {
                optionBindings[i].explicitText = options[i] ?? string.Empty;
                ApplyOptionLabelText(optionBindings[i], optionBindings[i].explicitText);
            }

            if (options.Count > optionBindings.Count)
            {
                Debug.LogWarning(
                    $"[Experiment][MetaXRDropdownAdapter] Received {options.Count} options, but only {optionBindings.Count} option bindings are assigned.",
                    this);
            }

            RebuildOptionTexts();
            SetSelectedIndex(selectedIndex, false);
        }

        public void ShowList()
        {
            SetListVisible(true);
        }

        public void HideList()
        {
            SetListVisible(false);
        }

        public void ToggleList()
        {
            SetListVisible(!IsListVisible());
        }

        private void InitializeIfNeeded()
        {
            if (initialized)
            {
                return;
            }

            EnsureOptionBindings();
            triggerToggle = triggerObject != null ? triggerObject.GetComponent<Toggle>() : null;
            RebuildOptionTexts();
            selectedIndex = ClampSelectedIndex(selectedIndex);
            ApplySelectionToView();
            SetListVisible(triggerToggle != null ? triggerToggle.isOn : !startCollapsed, true);
            initialized = true;
        }

        private void EnsureOptionBindings()
        {
            if (optionBindings.Count > 0 || listContainerObject == null)
            {
                return;
            }

            Transform listTransform = listContainerObject.transform;
            for (int i = 0; i < listTransform.childCount; i++)
            {
                optionBindings.Add(new OptionBinding
                {
                    optionObject = listTransform.GetChild(i).gameObject
                });
            }
        }

        private void RegisterListeners()
        {
            if (listenersRegistered)
            {
                return;
            }

            triggerRegistration = CreatePressRegistration(triggerObject);
            RegisterTriggerListener(triggerRegistration);

            optionRegistrations.Clear();
            for (int i = 0; i < optionBindings.Count; i++)
            {
                PressRegistration registration = CreatePressRegistration(optionBindings[i].optionObject);
                optionRegistrations.Add(registration);
                RegisterOptionListener(registration, i);
            }

            listenersRegistered = true;
        }

        private void UnregisterListeners()
        {
            if (!listenersRegistered)
            {
                return;
            }

            UnregisterTriggerListener(triggerRegistration);
            triggerRegistration = null;

            for (int i = 0; i < optionRegistrations.Count; i++)
            {
                UnregisterOptionListener(optionRegistrations[i]);
            }

            optionRegistrations.Clear();
            listenersRegistered = false;
        }

        private static PressRegistration CreatePressRegistration(GameObject sourceObject)
        {
            if (sourceObject == null)
            {
                return null;
            }

            return new PressRegistration
            {
                button = sourceObject.GetComponent<Button>(),
                toggle = sourceObject.GetComponent<Toggle>(),
                interactableWrapper = sourceObject.GetComponent<InteractableUnityEventWrapper>(),
                pointableWrapper = sourceObject.GetComponent<PointableUnityEventWrapper>()
            };
        }

        private void RegisterTriggerListener(PressRegistration registration)
        {
            if (registration == null)
            {
                return;
            }

            // Prefer standard UI callbacks first to avoid duplicate invocations on objects that also expose Meta wrappers.
            if (registration.button != null)
            {
                registration.buttonAction = HandleTriggerPressed;
                registration.button.onClick.AddListener(registration.buttonAction);
                return;
            }

            if (registration.toggle != null)
            {
                registration.toggleAction = HandleTriggerToggleChanged;
                registration.toggle.onValueChanged.AddListener(registration.toggleAction);
                return;
            }

            if (registration.interactableWrapper != null)
            {
                registration.interactableAction = HandleTriggerPressed;
                registration.interactableWrapper.WhenSelect.AddListener(registration.interactableAction);
                return;
            }

            if (registration.pointableWrapper != null)
            {
                registration.pointableAction = HandleTriggerPointableSelected;
                registration.pointableWrapper.WhenSelect.AddListener(registration.pointableAction);
            }
        }

        private static void UnregisterTriggerListener(PressRegistration registration)
        {
            if (registration == null)
            {
                return;
            }

            if (registration.button != null && registration.buttonAction != null)
            {
                registration.button.onClick.RemoveListener(registration.buttonAction);
                return;
            }

            if (registration.toggle != null && registration.toggleAction != null)
            {
                registration.toggle.onValueChanged.RemoveListener(registration.toggleAction);
                return;
            }

            if (registration.interactableWrapper != null && registration.interactableAction != null)
            {
                registration.interactableWrapper.WhenSelect.RemoveListener(registration.interactableAction);
                return;
            }

            if (registration.pointableWrapper != null && registration.pointableAction != null)
            {
                registration.pointableWrapper.WhenSelect.RemoveListener(registration.pointableAction);
            }
        }

        private void RegisterOptionListener(PressRegistration registration, int optionIndex)
        {
            if (registration == null)
            {
                return;
            }

            if (registration.button != null)
            {
                registration.buttonAction = () => HandleOptionSelected(optionIndex);
                registration.button.onClick.AddListener(registration.buttonAction);
                return;
            }

            if (registration.toggle != null)
            {
                registration.toggleAction = isOn =>
                {
                    if (isOn)
                    {
                        HandleOptionSelected(optionIndex);
                    }
                };
                registration.toggle.onValueChanged.AddListener(registration.toggleAction);
                return;
            }

            if (registration.interactableWrapper != null)
            {
                registration.interactableAction = () => HandleOptionSelected(optionIndex);
                registration.interactableWrapper.WhenSelect.AddListener(registration.interactableAction);
                return;
            }

            if (registration.pointableWrapper != null)
            {
                registration.pointableAction = _ => HandleOptionSelected(optionIndex);
                registration.pointableWrapper.WhenSelect.AddListener(registration.pointableAction);
            }
        }

        private static void UnregisterOptionListener(PressRegistration registration)
        {
            if (registration == null)
            {
                return;
            }

            if (registration.button != null && registration.buttonAction != null)
            {
                registration.button.onClick.RemoveListener(registration.buttonAction);
                return;
            }

            if (registration.toggle != null && registration.toggleAction != null)
            {
                registration.toggle.onValueChanged.RemoveListener(registration.toggleAction);
                return;
            }

            if (registration.interactableWrapper != null && registration.interactableAction != null)
            {
                registration.interactableWrapper.WhenSelect.RemoveListener(registration.interactableAction);
                return;
            }

            if (registration.pointableWrapper != null && registration.pointableAction != null)
            {
                registration.pointableWrapper.WhenSelect.RemoveListener(registration.pointableAction);
            }
        }

        private void HandleTriggerPressed()
        {
            ToggleList();
        }

        private void HandleTriggerPointableSelected(PointerEvent _)
        {
            HandleTriggerPressed();
        }

        private void HandleTriggerToggleChanged(bool isOn)
        {
            SetListVisible(isOn);
        }

        private void HandleOptionSelected(int optionIndex)
        {
            SetSelectedIndex(optionIndex, true);

            if (closeListOnSelection)
            {
                HideList();
            }
        }

        private void SetSelectedIndex(int index, bool sendCallback)
        {
            int clampedIndex = ClampSelectedIndex(index);
            if (clampedIndex == selectedIndex)
            {
                ApplySelectionToView();
                return;
            }

            selectedIndex = clampedIndex;
            ApplySelectionToView();

            if (sendCallback && selectedIndex >= 0)
            {
                onValueChanged?.Invoke(selectedIndex);
                ValueChanged?.Invoke(selectedIndex);
            }
        }

        private void ApplySelectionToView()
        {
            string selectedText = GetOptionText(selectedIndex);
            GameObject labelSource = selectedLabelObject != null ? selectedLabelObject : triggerObject;
            if (labelSource != null)
            {
                TrySetResolvedText(labelSource, selectedText);
            }

            for (int i = 0; i < optionBindings.Count; i++)
            {
                Toggle optionToggle = optionBindings[i].optionObject != null
                    ? optionBindings[i].optionObject.GetComponent<Toggle>()
                    : null;

                if (optionToggle != null)
                {
                    optionToggle.SetIsOnWithoutNotify(i == selectedIndex);
                }
            }
        }

        private void RebuildOptionTexts()
        {
            optionTexts.Clear();
            for (int i = 0; i < optionBindings.Count; i++)
            {
                OptionBinding optionBinding = optionBindings[i];
                string optionText = ResolveOptionText(optionBinding);
                optionTexts.Add(optionText);

                if (!string.IsNullOrEmpty(optionBinding.explicitText))
                {
                    ApplyOptionLabelText(optionBinding, optionBinding.explicitText);
                }
            }
        }

        private void ApplyOptionLabelText(OptionBinding optionBinding, string text)
        {
            GameObject labelSource = optionBinding.labelObject != null
                ? optionBinding.labelObject
                : optionBinding.optionObject;
            TrySetResolvedText(labelSource, text);
        }

        private string ResolveOptionText(OptionBinding optionBinding)
        {
            if (!string.IsNullOrEmpty(optionBinding.explicitText))
            {
                return optionBinding.explicitText;
            }

            GameObject labelSource = optionBinding.labelObject != null
                ? optionBinding.labelObject
                : optionBinding.optionObject;
            if (TryGetResolvedText(labelSource, out string text))
            {
                return text;
            }

            return optionBinding.optionObject != null ? optionBinding.optionObject.name : string.Empty;
        }

        private string GetOptionText(int index)
        {
            if (index < 0 || index >= optionTexts.Count)
            {
                return string.Empty;
            }

            return optionTexts[index] ?? string.Empty;
        }

        private int ClampSelectedIndex(int index)
        {
            if (optionBindings.Count == 0)
            {
                return -1;
            }

            return Mathf.Clamp(index, 0, optionBindings.Count - 1);
        }

        private void SetListVisible(bool visible)
        {
            SetListVisible(visible, false);
        }

        private void SetListVisible(bool visible, bool force)
        {
            if (!force && IsListVisible() == visible)
            {
                return;
            }

            if (listContainerObject != null)
            {
                listContainerObject.SetActive(visible);
            }

            if (listCanvasGroup != null)
            {
                listCanvasGroup.alpha = visible ? 1f : 0f;
                listCanvasGroup.interactable = visible;
                listCanvasGroup.blocksRaycasts = visible;
            }

            if (triggerToggle != null)
            {
                triggerToggle.SetIsOnWithoutNotify(visible);
            }
        }

        private bool IsListVisible()
        {
            if (listContainerObject != null)
            {
                return listContainerObject.activeSelf;
            }

            if (listCanvasGroup != null)
            {
                return listCanvasGroup.alpha > 0.001f
                       && listCanvasGroup.interactable
                       && listCanvasGroup.blocksRaycasts;
            }

            return false;
        }

        private static bool TryGetResolvedText(GameObject sourceObject, out string value)
        {
            if (!TryGetTextComponent(sourceObject, out Component textComponent))
            {
                value = default;
                return false;
            }

            return TryGetStringPropertyValue(textComponent, "text", out value);
        }

        private static bool TrySetResolvedText(GameObject sourceObject, string value)
        {
            if (!TryGetTextComponent(sourceObject, out Component textComponent))
            {
                return false;
            }

            return TrySetStringPropertyValue(textComponent, "text", value);
        }

        private static bool TryGetTextComponent(GameObject sourceObject, out Component component)
        {
            component = null;
            if (sourceObject == null)
            {
                return false;
            }

            string[] typeNames =
            {
                "TMPro.TextMeshProUGUI",
                "TMPro.TMP_Text",
                "UnityEngine.UI.Text"
            };

            for (int i = 0; i < typeNames.Length; i++)
            {
                Type type = Type.GetType(typeNames[i] + ", Unity.TextMeshPro")
                            ?? Type.GetType(typeNames[i] + ", UnityEngine.UI")
                            ?? Type.GetType(typeNames[i]);
                if (type == null)
                {
                    continue;
                }

                component = sourceObject.GetComponent(type) ?? sourceObject.GetComponentInChildren(type, true);
                if (component != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetStringPropertyValue(Component source, string propertyName, out string value)
        {
            if (source == null)
            {
                value = default;
                return false;
            }

            PropertyInfo property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanRead)
            {
                value = default;
                return false;
            }

            object rawValue = property.GetValue(source);
            if (rawValue is string stringValue)
            {
                value = stringValue;
                return true;
            }

            value = default;
            return false;
        }

        private static bool TrySetStringPropertyValue(Component source, string propertyName, string value)
        {
            if (source == null)
            {
                return false;
            }

            PropertyInfo property = source.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite || property.PropertyType != typeof(string))
            {
                return false;
            }

            property.SetValue(source, value);
            return true;
        }
    }
}
