#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
//  Editor tooling
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.EditorTools
{
    public static class MainMenuDeckbuildingPrefabBinder
    {
        #region _____________________________/ VALUES

        private const string PrefabPath = "Assets/Prefabs/UI/UI_Menu_Main.prefab";
        private const int TeamSize = 3;

        #endregion

        #region _____________________________| MENU

        [MenuItem("Tools/TacticalPort/UI/Bind Main Menu Deckbuilding")]
        public static void BindPrefab()
        {
            GameObject lRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Bind(lRoot);
                PrefabUtility.SaveAsPrefabAsset(lRoot, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"Bound main menu deckbuilding prefab: {PrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(lRoot);
            }
        }

        #endregion

        #region _____________________________| BIND

        private static void Bind(GameObject pRoot)
        {
            MainMenuTeamDeckbuildingController lController = EnsureComponent<MainMenuTeamDeckbuildingController>(pRoot);
            Transform lScreenRoot = FindByName(pRoot.transform, "Screen_MainMenu") ?? pRoot.transform;
            Transform lAnchorsRoot = EnsureChild(lScreenRoot, "CharacterModelAnchors");

            GameObject lDeckbuildingPanel = FindByName(pRoot.transform, "UI_Panel_Deckbuilding")?.gameObject;
            DeckbuildingPanelController lDeckbuildingController = lDeckbuildingPanel != null
                ? lDeckbuildingPanel.GetComponentInChildren<DeckbuildingPanelController>(true)
                : null;

            SerializedObject lObject = new SerializedObject(lController);
            AssignUnits(lObject.FindProperty("_AvailableUnits"));

            SerializedProperty lSlots = lObject.FindProperty("_TeamSlots");
            lSlots.arraySize = TeamSize;
            for (int lIndex = 0; lIndex < TeamSize; lIndex++)
                AssignSlot(lSlots.GetArrayElementAtIndex(lIndex), pRoot.transform, lAnchorsRoot, lIndex);

            lObject.FindProperty("_DeckbuildingPanel").objectReferenceValue = lDeckbuildingPanel;
            lObject.FindProperty("_DeckbuildingPanelController").objectReferenceValue = lDeckbuildingController;
            lObject.ApplyModifiedPropertiesWithoutUndo();

            if (lDeckbuildingPanel != null)
                lDeckbuildingPanel.SetActive(false);

            EditorUtility.SetDirty(pRoot);
        }

        private static void AssignSlot(SerializedProperty pSlot, Transform pSearchRoot, Transform pAnchorsRoot, int pSlotIndex)
        {
            Transform lButtonTransform = FindByName(pSearchRoot, $"Btn_Character{pSlotIndex + 1}");
            Button lButton = lButtonTransform != null ? lButtonTransform.GetComponent<Button>() : null;
            Transform lAnchor = EnsureModelAnchor(pAnchorsRoot, lButtonTransform, pSlotIndex);
            TMP_Text lNameLabel = lButtonTransform != null ? lButtonTransform.GetComponentInChildren<TMP_Text>(true) : null;

            pSlot.FindPropertyRelative("Button").objectReferenceValue = lButton;
            pSlot.FindPropertyRelative("ModelAnchor").objectReferenceValue = lAnchor;
            pSlot.FindPropertyRelative("NameLabel").objectReferenceValue = lNameLabel;
            pSlot.FindPropertyRelative("FallbackPortrait").objectReferenceValue = null;
        }

        private static Transform EnsureModelAnchor(Transform pAnchorsRoot, Transform pButtonTransform, int pSlotIndex)
        {
            Transform lAnchor = FindDirectChild(pAnchorsRoot, $"Anchor_Character{pSlotIndex + 1}");
            if (lAnchor == null)
            {
                GameObject lAnchorObject = new GameObject($"Anchor_Character{pSlotIndex + 1}", typeof(RectTransform));
                lAnchorObject.transform.SetParent(pAnchorsRoot, false);
                lAnchor = lAnchorObject.transform;
            }

            RectTransform lAnchorRect = lAnchor as RectTransform;
            RectTransform lButtonRect = pButtonTransform as RectTransform;
            if (lAnchorRect != null)
            {
                lAnchorRect.anchorMin = new Vector2(0.5f, 0.5f);
                lAnchorRect.anchorMax = new Vector2(0.5f, 0.5f);
                lAnchorRect.pivot = new Vector2(0.5f, 0.5f);
                lAnchorRect.sizeDelta = Vector2.zero;
                lAnchorRect.anchoredPosition = lButtonRect != null ? lButtonRect.anchoredPosition : Vector2.zero;
            }

            Vector3 lLocalPosition = lAnchor.localPosition;
            lLocalPosition.z = 60f;
            lAnchor.localPosition = lLocalPosition;
            lAnchor.localRotation = Quaternion.identity;
            lAnchor.localScale = Vector3.one;

            return lAnchor;
        }

        private static void AssignUnits(SerializedProperty pUnits)
        {
            string[] lGuids = AssetDatabase.FindAssets("t:UnitDefinition", new[] { "Assets" });
            List<UnitDefinition> lUnits = new List<UnitDefinition>();
            for (int lIndex = 0; lIndex < lGuids.Length; lIndex++)
            {
                string lPath = AssetDatabase.GUIDToAssetPath(lGuids[lIndex]);
                UnitDefinition lUnit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(lPath);
                if (lUnit != null && !lUnits.Contains(lUnit))
                    lUnits.Add(lUnit);
            }

            pUnits.arraySize = lUnits.Count;
            for (int lIndex = 0; lIndex < lUnits.Count; lIndex++)
                pUnits.GetArrayElementAtIndex(lIndex).objectReferenceValue = lUnits[lIndex];
        }

        #endregion

        #region _____________________________| HELPERS

        private static Transform EnsureChild(Transform pParent, string pName)
        {
            Transform lChild = FindDirectChild(pParent, pName);
            if (lChild != null)
                return lChild;

            GameObject lObject = new GameObject(pName, typeof(RectTransform));
            lObject.transform.SetParent(pParent, false);
            RectTransform lRect = lObject.GetComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;
            lRect.localScale = Vector3.one;
            return lObject.transform;
        }

        private static Transform FindDirectChild(Transform pParent, string pName)
        {
            for (int lIndex = 0; lIndex < pParent.childCount; lIndex++)
            {
                Transform lChild = pParent.GetChild(lIndex);
                if (lChild != null && lChild.name == pName)
                    return lChild;
            }

            return null;
        }

        private static Transform FindByName(Transform pRoot, string pName)
        {
            if (pRoot.name == pName)
                return pRoot;

            Transform[] lChildren = pRoot.GetComponentsInChildren<Transform>(true);
            for (int lIndex = 0; lIndex < lChildren.Length; lIndex++)
            {
                Transform lChild = lChildren[lIndex];
                if (lChild != null && lChild.name == pName)
                    return lChild;
            }

            return null;
        }

        private static T EnsureComponent<T>(GameObject pObject) where T : Component
        {
            if (pObject.TryGetComponent(out T lComponent))
                return lComponent;

            return pObject.AddComponent<T>();
        }

        #endregion
    }
}
