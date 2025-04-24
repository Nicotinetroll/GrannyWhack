using OctoberStudio.Easing;
using OctoberStudio.Extensions;
using OctoberStudio.Input;
using OctoberStudio.Systems;            // RerollManager
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OctoberStudio.Upgrades.UI
{
    public class UpgradesWindowBehavior : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] UpgradesDatabase database;

        [Header("Prefabs")]
        [SerializeField] GameObject itemPrefab;         // default upgrade card
        [SerializeField] GameObject rerollItemPrefab;   // prefab with RerollShopTileBehavior

        [Header("Layout")]
        [SerializeField] RectTransform itemsParent;     // ScrollView/Viewport/Content
        [SerializeField] ScrollRect    scrollView;
        [SerializeField] Button        backButton;

        readonly List<UpgradeItemBehavior> items = new();
        Button rerollButton;                            // cached for navigation

        /* ─────────────────── initialisation ─────────────────── */
        public void Init(UnityAction onBackClick)
        {
            backButton.onClick.AddListener(onBackClick);

            /* 1. normal upgrades */
            for (int i = 0; i < database.UpgradesCount; ++i)
            {
                var data   = database.GetUpgrade(i);
                var go     = Instantiate(itemPrefab, itemsParent);
                go.transform.ResetLocal();

                var card   = go.GetComponent<UpgradeItemBehavior>();
                int lvl    = GameController.UpgradesManager.GetUpgradeLevel(data.UpgradeType);
                card.Init(data, lvl + 1);
                card.onNavigationSelected += OnItemSelected;
                items.Add(card);
            }

            /* 2. reroll tile */
            if (rerollItemPrefab != null)
            {
                var obj = Instantiate(rerollItemPrefab, itemsParent);
                obj.transform.ResetLocal();
                rerollButton = obj.GetComponentInChildren<Button>();
            }

            /* force layout update */
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsParent);
            scrollView.content.anchoredPosition = Vector2.zero;

            BuildNavigation();
        }

        /* ─────────────────── opening / closing ─────────────────── */
        public void Open()
        {
            gameObject.SetActive(true);

            ((RectTransform)transform).anchoredPosition = Vector2.zero; // safety

            EasingManager.DoNextFrame(() =>
            {
                if (items.Count > 0) items[0].Select();
                else if (rerollButton) EventSystem.current.SetSelectedGameObject(rerollButton.gameObject);
                else EventSystem.current.SetSelectedGameObject(backButton.gameObject);
            });

            GameController.InputManager.InputAsset.UI.Back.performed += OnBack;
            GameController.InputManager.onInputChanged               += OnInputChanged;
        }

        public void Close()
        {
            GameController.InputManager.InputAsset.UI.Back.performed -= OnBack;
            GameController.InputManager.onInputChanged               -= OnInputChanged;
            gameObject.SetActive(false);
        }

        /* ─────────────────── navigation ─────────────────── */
        void BuildNavigation()
        {
            var buttons = new List<Selectable>();
            foreach (var it in items) buttons.Add(it.Selectable);
            if (rerollButton) buttons.Add(rerollButton);

            for (int i = 0; i < buttons.Count; ++i)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };

                int col = i % 2;
                if (col == 0 && i + 1 < buttons.Count) nav.selectOnRight = buttons[i + 1];
                if (col == 1)                          nav.selectOnLeft  = buttons[i - 1];
                if (i - 2 >= 0)                        nav.selectOnUp    = buttons[i - 2];
                nav.selectOnDown = (i + 2 < buttons.Count) ? buttons[i + 2] : backButton;

                buttons[i].navigation = nav;
            }

            var backNav = new Navigation { mode = Navigation.Mode.Explicit };
            if (buttons.Count > 0) backNav.selectOnUp = buttons[^1];
            backButton.navigation = backNav;
        }

        /* ─────────────────── scrolling helper ─────────────────── */
        public void OnItemSelected(UpgradeItemBehavior card)
        {
            var pos    = (Vector2)scrollView.transform.InverseTransformPoint(card.Rect.position);
            float view = scrollView.GetComponent<RectTransform>().rect.height;
            float h    = card.Rect.rect.height + 37f;

            if (pos.y >  view / 2) scrollView.content.localPosition += Vector3.up   * h;
            if (pos.y < -view / 2) scrollView.content.localPosition += Vector3.down * h;
        }

        /* ─────────────────── input ─────────────────── */
        void OnBack(InputAction.CallbackContext _) => backButton.onClick?.Invoke();

        void OnInputChanged(InputType prev, InputType cur)
        {
            if (prev != InputType.UIJoystick) return;
            if (EventSystem.current.currentSelectedGameObject) return;

            if (items.Count > 0) items[0].Select();
            else if (rerollButton) EventSystem.current.SetSelectedGameObject(rerollButton.gameObject);
            else EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }

        /* ─────────────────── cleanup ─────────────────── */
        public void Clear()
        {
            foreach (var it in items) it.Clear();
        }
    }
}
