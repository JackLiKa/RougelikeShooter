using System.Collections.Generic;
using UnityEngine;

public static class GameSceneSelectionApplier
{
    private static readonly Vector3 DefaultWeaponLocalPosition = new Vector3(0.14f, -0.19f, 0f);
    private static readonly Quaternion DefaultWeaponLocalRotation = Quaternion.identity;
    private static readonly Vector3 DefaultWeaponLocalScale = Vector3.one;
    private static Camera cachedMainCamera;

    public static void Apply(PlayerType playerType, WeaponType weaponType)
    {
        Transform playersRoot = GameObject.Find("Players")?.transform;
        if (playersRoot == null)
        {
            return;
        }

        string selectedPlayerName = GameSelectionConfig.GetPlayerObjectName(playerType);
        GameObject selectedPlayerObject = null;

        foreach (Transform playerTransform in playersRoot)
        {
            bool isSelectedPlayer = playerTransform.name == selectedPlayerName;
            playerTransform.gameObject.SetActive(isSelectedPlayer);

            if (isSelectedPlayer)
            {
                selectedPlayerObject = playerTransform.gameObject;
            }
        }

        if (selectedPlayerObject == null)
        {
            return;
        }

        PlayerRuntimeStats runtimeStats = selectedPlayerObject.GetComponent<PlayerRuntimeStats>();
        if (runtimeStats == null)
        {
            runtimeStats = selectedPlayerObject.AddComponent<PlayerRuntimeStats>();
        }
        runtimeStats.ApplyProfile(PlayerProfileRepository.GetProfile(playerType));

        AttachSelectedWeapon(selectedPlayerObject.transform, weaponType);
        UpdateCameraTarget(selectedPlayerObject.transform);

        GameObject weaponsRoot = GameObject.Find("Weapons");
        if (weaponsRoot != null)
        {
            weaponsRoot.SetActive(false);
        }
    }

    private static void AttachSelectedWeapon(Transform playerTransform, WeaponType weaponType)
    {
        string selectedWeaponName = GameSelectionConfig.GetWeaponObjectName(weaponType);
        List<Transform> weaponTransforms = new List<Transform>();
        Transform selectedWeaponTransform = null;
        Vector3 localPosition = DefaultWeaponLocalPosition;
        Quaternion localRotation = DefaultWeaponLocalRotation;
        Vector3 localScale = DefaultWeaponLocalScale;

        foreach (Transform childTransform in playerTransform)
        {
            if (childTransform.GetComponent<Ak47>() == null)
            {
                continue;
            }

            weaponTransforms.Add(childTransform);

            if (weaponTransforms.Count == 1)
            {
                localPosition = childTransform.localPosition;
                localRotation = childTransform.localRotation;
                localScale = childTransform.localScale;
            }

            if (childTransform.name == selectedWeaponName)
            {
                selectedWeaponTransform = childTransform;
            }
        }

        if (selectedWeaponTransform == null)
        {
            Transform templateRoot = GameObject.Find("Weapons")?.transform;
            Transform templateTransform = templateRoot != null ? templateRoot.Find(selectedWeaponName) : null;
            if (templateTransform != null)
            {
                GameObject clonedWeaponObject = Object.Instantiate(templateTransform.gameObject, playerTransform);
                clonedWeaponObject.name = selectedWeaponName;
                clonedWeaponObject.transform.localPosition = localPosition;
                clonedWeaponObject.transform.localRotation = localRotation;
                clonedWeaponObject.transform.localScale = localScale;
                selectedWeaponTransform = clonedWeaponObject.transform;
                weaponTransforms.Add(selectedWeaponTransform);
            }
        }

        foreach (Transform weaponTransform in weaponTransforms)
        {
            weaponTransform.gameObject.SetActive(weaponTransform == selectedWeaponTransform);
            AssignCamera(weaponTransform);
        }
    }

    private static void AssignCamera(Transform weaponTransform)
    {
        if (weaponTransform == null)
        {
            return;
        }

        Ak47 weapon = weaponTransform.GetComponent<Ak47>();
        if (weapon == null)
        {
            return;
        }

        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }

        if (cachedMainCamera == null)
        {
            GameObject namedCamera = GameObject.Find("Main Camera");
            if (namedCamera != null)
            {
                cachedMainCamera = namedCamera.GetComponent<Camera>();
            }
        }

        if (cachedMainCamera == null)
        {
            cachedMainCamera = Object.FindAnyObjectByType<Camera>();
        }

        weapon.mainCamera = cachedMainCamera;
    }

    private static void UpdateCameraTarget(Transform playerTransform)
    {
        CameraFollow cameraFollow = Object.FindAnyObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.target = playerTransform;
        }
    }
}
