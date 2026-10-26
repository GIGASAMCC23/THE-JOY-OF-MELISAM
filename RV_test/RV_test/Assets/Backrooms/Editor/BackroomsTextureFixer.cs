#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class BackroomsTextureFixer : EditorWindow
{
    private GameObject target;

    // Relación EXACTA tomada del TXT
    private static readonly Dictionary<string, string> materialToTexture =
        new Dictionary<string, string>
        {
            { "ArmChair_1", "Image_0" },
            { "ArmChair_1.001", "Image_0.001" },

            { "Ceiling_1", "Image_1" },
            { "Ceiling_1.001", "Image_1.001" },

            { "Ceiling_2", "Image_2" },
            { "Ceiling_2.001", "Image_2.001" },

            { "Ceiling_3", "Image_3" },
            { "Ceiling_3.001", "Image_3.001" },

            { "Ceiling_4", "Image_4" },
            { "Ceiling_4.001", "Image_4.001" },

            { "Ceiling_Lamp", "Image_5" },
            { "Ceiling_Lamp.001", "Image_5.001" },

            { "Exit_Door", "Image_6" },
            { "Exit_Door.001", "Image_6.001" },

            { "Exit_Sign", "Image_7" },
            { "Exit_Sign.001", "Image_7.001" },

            { "Exit_Traslucent", "Image_8" },
            { "Exit_Traslucent.001", "Image_8.001" },

            { "Furniture_1", "Image_9" },
            { "Furniture_1.001", "Image_9.001" },

            { "Lamp", "Image_10" },
            { "Lamp.001", "Image_10.001" },

            { "Moquette_1", "Image_11" },
            { "Moquette_1.001", "Image_11.001" },

            { "Moquette_2", "Image_12" },
            { "Moquette_2.001", "Image_12.001" },

            { "Moquette_3", "Image_13" },
            { "Moquette_3.001", "Image_13.001" },

            { "Moquette_4", "Image_14" },
            { "Moquette_4.001", "Image_14.001" },

            { "Sensor", "Image_15" },
            { "Sensor.001", "Image_15.001" },

            { "Socket_1", "Image_16" },
            { "Socket_1.001", "Image_16.001" },

            { "Translucent", "Image_17" },
            { "Translucent.001", "Image_17.001" },

            { "Traslucent", "Image_18" },
            { "Traslucent.001", "Image_18.001" },

            { "Vent_1", "Image_19" },
            { "Vent_1.001", "Image_19.001" },

            { "Wall_1", "Image_20" },
            { "Wall_1.001", "Image_20.001" },

            { "Wall_2", "Image_21" },
            { "Wall_2.001", "Image_21.001" },

            { "Wall_3", "Image_22" },
            { "Wall_3.001", "Image_22.001" },

            { "Wall_4", "Image_23" },
            { "Wall_4.001", "Image_23.001" },

            { "Window_1", "Image_24" },
            { "Window_1.001", "Image_24.001" },

            { "Withe", "Image_25" },
            { "Withe.001", "Image_25.001" },

            { "Wood", "Image_26" },
            { "Wood.001", "Image_26.001" }
        };


    [MenuItem("Backrooms/Arreglar Texturas Backrooms")]
    public static void ShowWindow()
    {
        GetWindow<BackroomsTextureFixer>("Backrooms");
    }


    private void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label(
            "REPARADOR DE TEXTURAS - BACKROOMS",
            EditorStyles.boldLabel
        );

        GUILayout.Space(10);

        target = (GameObject)EditorGUILayout.ObjectField(
            "Modelo:",
            target,
            typeof(GameObject),
            true
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Seleccionar BackRoomLevel0"))
        {
            target = GameObject.Find("BackRoomLevel0");

            if (target == null)
            {
                EditorUtility.DisplayDialog(
                    "Backrooms",
                    "No encontré un objeto llamado BackRoomLevel0 en la escena.",
                    "OK"
                );
            }
        }

        GUILayout.Space(10);

        GUI.enabled = target != null;

        if (GUILayout.Button(
            "APLICAR TEXTURAS CORRECTAMENTE",
            GUILayout.Height(40)))
        {
            ApplyTextures();
        }

        GUI.enabled = true;
    }


    private void ApplyTextures()
    {
        Renderer[] renderers =
            target.GetComponentsInChildren<Renderer>(true);

        int materialsProcessed = 0;
        int texturesAssigned = 0;
        int texturesMissing = 0;
        int materialsWithoutTexture = 0;

        List<string> missing = new List<string>();

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];

                if (material == null)
                    continue;

                materialsProcessed++;

                string materialName = material.name;

                // Quitamos posibles sufijos de Unity
                materialName = CleanMaterialName(materialName);

                // Materiales que oficialmente NO tienen textura
                if (materialName == "Emission_Red" ||
                    materialName == "Material")
                {
                    materialsWithoutTexture++;
                    continue;
                }

                if (!materialToTexture.TryGetValue(
                    materialName,
                    out string textureName))
                {
                    missing.Add(
                        material.name +
                        "  -> no existe en el TXT"
                    );

                    continue;
                }

                Texture2D texture =
                    FindTexture(textureName);

                if (texture == null)
                {
                    texturesMissing++;

                    missing.Add(
                        material.name +
                        " -> " +
                        textureName
                    );

                    continue;
                }

                // Crear una copia del material para no destruir
                // accidentalmente el material original.
                Material newMaterial =
                    new Material(material);

                newMaterial.name =
                    material.name + "_URP";

                // URP Lit
                Shader urpShader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit"
                    );

                if (urpShader != null)
                {
                    newMaterial.shader = urpShader;
                }

                // Base Map
                if (newMaterial.HasProperty("_BaseMap"))
                {
                    newMaterial.SetTexture(
                        "_BaseMap",
                        texture
                    );
                }

                // Color blanco para no oscurecer la textura
                if (newMaterial.HasProperty("_BaseColor"))
                {
                    newMaterial.SetColor(
                        "_BaseColor",
                        Color.white
                    );
                }

                // Metallic
                if (newMaterial.HasProperty("_Metallic"))
                {
                    newMaterial.SetFloat(
                        "_Metallic",
                        0f
                    );
                }

                // Suavidad moderada
                if (newMaterial.HasProperty("_Smoothness"))
                {
                    newMaterial.SetFloat(
                        "_Smoothness",
                        0.35f
                    );
                }

                // Guardar material
                string folder =
                    "Assets/Backrooms/Materials/Generated";

                EnsureFolder(folder);

                string safeName =
                    SanitizeFileName(
                        newMaterial.name
                    );

                string materialPath =
                    folder +
                    "/" +
                    safeName +
                    ".mat";

                // Evitar conflictos de nombres
                materialPath =
                    AssetDatabase.GenerateUniqueAssetPath(
                        materialPath
                    );

                AssetDatabase.CreateAsset(
                    newMaterial,
                    materialPath
                );

                // Asignar
                materials[i] = newMaterial;

                texturesAssigned++;
            }

            renderer.sharedMaterials = materials;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message =
            "REORGANIZACIÓN TERMINADA\n\n" +
            "Renderers encontrados: " +
            renderers.Length + "\n" +
            "Materiales procesados: " +
            materialsProcessed + "\n" +
            "Texturas asignadas: " +
            texturesAssigned + "\n" +
            "Texturas faltantes: " +
            texturesMissing + "\n" +
            "Sin textura según original: " +
            materialsWithoutTexture;

        if (missing.Count > 0)
        {
            message +=
                "\n\nELEMENTOS QUE NO PUDIERON ASIGNARSE:\n";

            foreach (string item in missing)
            {
                message += "\n" + item;
            }
        }

        EditorUtility.DisplayDialog(
            "Backrooms",
            message,
            "OK"
        );
    }


    private static Texture2D FindTexture(
        string originalName)
    {
        // 1. Intento exacto
        string[] exact =
            AssetDatabase.FindAssets(
                "\"" + originalName + "\" t:Texture2D"
            );

        foreach (string guid in exact)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            string fileName =
                Path.GetFileNameWithoutExtension(path);

            if (fileName == originalName)
            {
                return AssetDatabase.LoadAssetAtPath<Texture2D>(
                    path
                );
            }
        }

        // 2. Normalizamos .001
        string normalized =
            originalName.Replace(".001", "");

        string[] all =
            AssetDatabase.FindAssets(
                "t:Texture2D"
            );

        Texture2D candidate = null;

        foreach (string guid in all)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            string fileName =
                Path.GetFileNameWithoutExtension(path);

            // Ejemplo:
            // Image_20
            // Image_20_4
            // Image_20_23
            if (fileName == normalized ||
                fileName.StartsWith(normalized + "_"))
            {
                candidate =
                    AssetDatabase.LoadAssetAtPath<Texture2D>(
                        path
                    );

                if (candidate != null)
                    return candidate;
            }
        }

        return null;
    }


    private static string CleanMaterialName(
        string name)
    {
        // Unity puede agregar "(Instance)"
        name = name.Replace(" (Instance)", "");

        return name;
    }


    private static void EnsureFolder(
        string folder)
    {
        string[] parts =
            folder.Split('/');

        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next =
                current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(
                    current,
                    parts[i]
                );
            }

            current = next;
        }
    }


    private static string SanitizeFileName(
        string name)
    {
        foreach (char c in
                 Path.GetInvalidFileNameChars())
        {
            name = name.Replace(
                c.ToString(),
                "_"
            );
        }

        return name;
    }
}

#endif