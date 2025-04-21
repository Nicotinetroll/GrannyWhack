using System.Collections.Generic;
using OctoberStudio.Abilities;
using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Input;      // for InputType
using OctoberStudio.UI;         // for CharacterItemBehavior
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OctoberStudio.UI.Windows
{
    public class CharactersWindowBehavior : MonoBehaviour
    {
        [Header("Data Sources")]
        [SerializeField] private CharactersDatabase database;
        [SerializeField] private AbilitiesDatabase  abilitiesDatabase;

        [Header("UI Prefabs")]
        [SerializeField] private GameObject    itemPrefab;
        [SerializeField] private RectTransform itemsParent;

        [Header("Navigation")]
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private Button     backButton;

        private readonly List<CharacterItemBehavior> items = new();

        // Keep delegate handles so we unsubscribe the same instance
        private System.Action<InputAction.CallbackContext> backPerformedHandler;
        private UnityAction<InputType, InputType>          inputChangedHandler;

        public void Init(UnityAction onBackButtonClicked)
        {
            backButton.onClick.AddListener(onBackButtonClicked);

            for (int i = 0; i < database.CharactersCount; i++)
            {
                var go   = Instantiate(itemPrefab, itemsParent, false);
                var item = go.GetComponent<CharacterItemBehavior>();
                go.transform.ResetLocal();

                item.Init(i, database.GetCharacterData(i), abilitiesDatabase);
                item.onNavigationSelected += OnItemSelected;

                items.Add(item);
            }

            ResetNavigation();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            EasingManager.DoNextFrame(() => { /* select logic */ });

            // subscribe
            GameController.InputManager.InputAsset.UI.Back.performed += OnBackInputClicked;
            GameController.InputManager.onInputChanged            += OnInputChanged;
        }

        public void Close()
        {
            // unsubscribe
            GameController.InputManager.InputAsset.UI.Back.performed -= OnBackInputClicked;
            GameController.InputManager.onInputChanged            -= OnInputChanged;

            gameObject.SetActive(false);
        }

        public void ResetNavigation()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };

                if (i - 2 >= 0) nav.selectOnUp   = items[i - 2].Selectable;
                if (i - 1 >= 0) nav.selectOnLeft = items[i - 1].Selectable;
                nav.selectOnRight = (i + 1 < items.Count) ? items[i + 1].Selectable : backButton;
                nav.selectOnDown  = (i + 2 < items.Count) ? items[i + 2].Selectable : backButton;

                items[i].Selectable.navigation = nav;
            }

            if (items.Count > 0)
            {
                var wrap = new Navigation { mode = Navigation.Mode.Explicit };
                wrap.selectOnUp = items[^1].Selectable;
                backButton.navigation = wrap;
            }
        }

        private void OnItemSelected(CharacterItemBehavior selectedItem)
        {
            var viewport = scrollView.viewport;
            float halfH  = viewport.rect.height * 0.5f;
            var localPos = (Vector2)viewport.transform.InverseTransformPoint(selectedItem.Rect.position);
            float shift  = selectedItem.Rect.rect.height + 37f;

            if (localPos.y > halfH)
                scrollView.content.localPosition -= new Vector3(0, shift);
            else if (localPos.y < -halfH)
                scrollView.content.localPosition += new Vector3(0, shift);
        }

        private void OnBackInputClicked(InputAction.CallbackContext ctx)
        {
            backButton.onClick?.Invoke();
        }

        private void OnInputChanged(InputType prevInput, InputType currInput)
        {
            if (prevInput != InputType.UIJoystick) return;

            var viewport = scrollView.viewport;
            float halfH  = viewport.rect.height * 0.5f;

            foreach (var item in items)
            {
                var pos = (Vector2)viewport.transform.InverseTransformPoint(item.Rect.position);
                if (pos.y < halfH && pos.y > -halfH)
                {
                    item.Select();
                    return;
                }
            }

            // fallback
            if (items.Count > 0) items[0].Select();
            else                 backButton.Select();
        }

        public void Clear()
        {
            foreach (var item in items)
                item.Clear();
        }
    }
}
