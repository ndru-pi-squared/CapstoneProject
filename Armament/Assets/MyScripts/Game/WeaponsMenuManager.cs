using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Com.Kabaj.TestPhotonMultiplayerFPSGame
{
    public class WeaponsMenuManager : MonoBehaviour
    {
        public Transform weaponItemPrefab;

        PlayerManager localPlayerPM;
        
        // Player's inventory of weapons
        GameObject activeWeapon;
        GameObject[] inactiveWeapons;
        
        public int HighlightIndex { get; set; } = 0;

        ArrayList weaponItems = new ArrayList();

        bool readyToMoveHighlight = false; // keeps track of 

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (localPlayerPM == null)
                return;

        }

        void OnEnable()
        {
            localPlayerPM = PlayerManager.LocalPlayerInstance.GetComponent<PlayerManager>();
        }

        public void MoveHighlightIndexForward()
        {
            // Make sure the highlight isn't changed the first time this method is called after opening menu
            if (!readyToMoveHighlight)
            {
                readyToMoveHighlight = true;
                return;
            }

            // If there are no weapon items to highlight...
            if (weaponItems.Count == 0)
                return;

            // If we're on the last highlight index...
            if (HighlightIndex == weaponItems.Count - 1)
                HighlightIndex = 0; // wraps around to the beginning
            else
                HighlightIndex++; // goes to the next highlight index

            UpdateHighlightInMenu();
        }

        public void MoveHighlightIndexBackward()
        {
            // Make sure the highlight isn't changed the first time this method is called after opening menu
            if (!readyToMoveHighlight)
            {
                readyToMoveHighlight = true;
                return;
            }

            // If there are no weapon items to highlight...
            if (weaponItems.Count == 0)
                return;

            // If we're on the first highlight index...
            if (HighlightIndex == 0)
                HighlightIndex = weaponItems.Count - 1; // wraps around to the end
            else
                HighlightIndex--; // goes to the next highlight index

            UpdateHighlightInMenu();
        }

        private void UpdateHighlightInMenu()
        {
            for(int i = 0; i < weaponItems.Count; i++)
            {
                GameObject go = (GameObject)weaponItems[i];
                if (i == HighlightIndex)
                    go.GetComponent<Text>().fontStyle = FontStyle.Bold;
                else
                    go.GetComponent<Text>().fontStyle = FontStyle.Normal;
            }
        }

        /// <summary>
        /// Finds the Photon View ID of the Gun that is highlighted in the weapons menu, if there is a gun to be highlighted.
        /// </summary>
        /// <returns>If a Gun is highlighted: Gun ViewID. Otherwise: -1.</returns>
        public int GetHighlightedGunViewID()
        {
            int gunViewID = -1;
            if (weaponItems.Count > 0)
            {
                if (HighlightIndex == 0)
                {
                    Transform activeWeaponInventory = localPlayerPM.gameObject.transform.Find("FirstPersonCharacter/Active Weapon");
                    gunViewID = activeWeaponInventory.GetChild(0).GetComponent<PhotonView>().ViewID;
                }
                else if (HighlightIndex > 0)
                {
                    Transform activeWeaponInventory = localPlayerPM.gameObject.transform.Find("FirstPersonCharacter/Inactive Weapons");
                    Debug.LogFormat("WeaponsMenuManager: GetHighlightedGunViewID() HighlightIndex-1 = {0}", HighlightIndex - 1);
                    gunViewID = activeWeaponInventory.GetChild(HighlightIndex - 1).GetComponent<PhotonView>().ViewID;
                }
            }
            return gunViewID;
        }

        public void OpenMenu()
        {
            HighlightIndex = 0;
            gameObject.SetActive(true);
            readyToMoveHighlight = false;
        }

        public void CloseMenu()
        {
            gameObject.SetActive(false);
        }

        public void UpdateWeaponInventoryMenu()
        {

            if (localPlayerPM == null)
                localPlayerPM = PlayerManager.LocalPlayerInstance.GetComponent<PlayerManager>();

            // Find references to the player's weapon inventory
            Transform activeWeaponInventory = localPlayerPM.gameObject.transform.Find("FirstPersonCharacter/Active Weapon");
            Transform inactiveWeaponInventory = localPlayerPM.gameObject.transform.Find("FirstPersonCharacter/Inactive Weapons");

            // Get all the weapons in inventory
            activeWeapon = activeWeaponInventory.childCount > 0 ? activeWeaponInventory.GetChild(0).gameObject : null;
            inactiveWeapons = new GameObject[inactiveWeaponInventory.childCount];
            for (int i = 0; i < inactiveWeaponInventory.childCount; i++)
                inactiveWeapons[i] = inactiveWeaponInventory.GetChild(i).gameObject;

            // Remove all menu items
            foreach (GameObject go in weaponItems)
            {
                Destroy(go);
            }

            weaponItems = new ArrayList();
            HighlightIndex = 0;

            Debug.LogFormat("WeaponsMenuManager: UpdateWeaponInventoryMenu() transform.childCount = {0}", transform.childCount);

            if (activeWeapon != null)
            {
                Transform t = Instantiate(weaponItemPrefab, transform);
                Text textComponent = t.GetComponent<Text>();
                textComponent.text = Regex.Replace(activeWeapon.GetComponent<Gun>().gunPrefab.name, "\\(Clone\\)", "");
                textComponent.fontStyle = FontStyle.Bold;

                weaponItems.Add(t.gameObject);
            }

            for (int i = 0; i < inactiveWeapons.Length; i++)
            {
                Transform t = Instantiate(weaponItemPrefab, transform);
                Text textComponent = t.GetComponent<Text>();
                textComponent.text = Regex.Replace(inactiveWeapons[i].GetComponent<Gun>().gunPrefab.name, "\\(Clone\\)", "");

                weaponItems.Add(t.gameObject);
            }

        }
    }
}