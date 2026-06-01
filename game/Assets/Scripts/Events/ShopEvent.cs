using UnityEngine;
using System;
using AbyssWalker.Entity;

namespace AbyssWalker.Events
{
    /// <summary>
    /// Handles shop event logic: displays shop interface, manages buy/sell.
    /// </summary>
    public class ShopEvent : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private PopupUI shopPopup;

        public event Action<ShopTransaction> OnPurchase;
        public event Action OnShopOpened;
        public event Action OnShopClosed;

        private EventData currentEvent;

        /// <summary>
        /// Opens the shop interface for the given event.
        /// </summary>
        public void OpenShop(EventData eventData)
        {
            if (eventData == null || eventData.shopItems == null) return;

            currentEvent = eventData;
            eventData.hasBeenTriggered = true;

            OnShopOpened?.Invoke();

            ShowShopUI();
        }

        /// <summary>
        /// Attempts to buy an item at the given index.
        /// </summary>
        public bool BuyItem(int itemIndex)
        {
            if (currentEvent == null || currentEvent.shopItems == null) return false;
            if (itemIndex < 0 || itemIndex >= currentEvent.shopItems.Length) return false;

            ShopItem item = currentEvent.shopItems[itemIndex];
            if (item.isSold) return false;

            Player player = FindObjectOfType<Player>();
            if (player == null) return false;

            if (player.GetGold() < item.price)
            {
                ShowMessage("Not enough gold!");
                return false;
            }

            // Deduct gold
            player.SpendGold(item.price);

            // Grant item
            GrantItem(item, player);

            item.isSold = true;

            ShopTransaction transaction = new ShopTransaction
            {
                item = item,
                wasBought = true,
                pricePaid = item.price
            };
            OnPurchase?.Invoke(transaction);

            ShowShopUI(); // Refresh UI
            return true;
        }

        /// <summary>
        /// Attempts to sell an item. Returns true if successful.
        /// </summary>
        public bool SellItem(string itemId, int sellPrice)
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return false;

            if (player.RemoveItem(itemId))
            {
                player.AddGold(sellPrice);

                ShopTransaction transaction = new ShopTransaction
                {
                    itemId = itemId,
                    wasBought = false,
                    pricePaid = sellPrice
                };
                OnPurchase?.Invoke(transaction);
                return true;
            }

            return false;
        }

        public void CloseShop()
        {
            currentEvent = null;
            OnShopClosed?.Invoke();

            if (shopPopup != null)
            {
                shopPopup.Hide();
            }
        }

        private void GrantItem(ShopItem item, Player player)
        {
            switch (item.itemType)
            {
                case ShopItemType.Potion:
                    player.AddPotion();
                    break;
                case ShopItemType.Weapon:
                    player.EquipWeapon(item.itemId, item.itemName, item.value);
                    break;
                case ShopItemType.Armor:
                    player.EquipArmor(item.itemId, item.itemName, item.value);
                    break;
                case ShopItemType.Skill:
                    player.LearnSkill(item.itemId);
                    break;
                case ShopItemType.Accessory:
                    player.EquipAccessory(item.itemId, item.itemName, item.value);
                    break;
            }
        }

        private void ShowShopUI()
        {
            if (shopPopup == null)
            {
                shopPopup = FindObjectOfType<PopupUI>();
            }
            if (shopPopup == null || currentEvent == null) return;

            string title = currentEvent.eventName ?? "Shop";
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < currentEvent.shopItems.Length; i++)
            {
                ShopItem item = currentEvent.shopItems[i];
                string status = item.isSold ? " [SOLD]" : "";
                sb.AppendLine($"[{i + 1}] {item.itemName} - {item.price}g{status}");

                if (!string.IsNullOrEmpty(item.description))
                {
                    sb.AppendLine($"    {item.description}");
                }
            }

            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                sb.AppendLine($"\nYour Gold: {player.GetGold()}");
            }

            shopPopup.Show(title, sb.ToString(), "Close", null);
        }

        private void ShowMessage(string message)
        {
            if (shopPopup != null)
            {
                shopPopup.Show("Shop", message, "OK", null);
            }
        }
    }

    /// <summary>
    /// Data class representing a shop transaction.
    /// </summary>
    [System.Serializable]
    public class ShopTransaction
    {
        public ShopItem item;
        public string itemId;
        public bool wasBought;
        public int pricePaid;
    }
}
