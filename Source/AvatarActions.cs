﻿using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using VRC.SDK3.Avatars.Components;

#if UNITY_EDITOR
namespace VRCAvatarActions
{
    [ExecuteAlways]
    public class AvatarActions : MonoBehaviour
    {
        //Descriptor Data
        public VRCAvatarDescriptor avatar;
        public MenuActions menuActions;
        public List<NonMenuActions> otherActions = new List<NonMenuActions>();

        //Build Options
        public List<string> ignoreLayers = new List<string>();
        public List<string> ignoreParameters = new List<string>();

        //Meta
        public bool foldoutBuildData = false;
        public bool foldoutBuildOptions = false;
        public bool foldoutIgnoreLayers = false;
        public bool foldoutIgnoreParameters = false;

        //Helper
        public UnityEngine.Object ReturnAnyScriptableObject()
        {
            if (menuActions != null)
                return menuActions;
            foreach(var action in otherActions)
            {
                if (action != null)
                    return action;
            }
            return null;
        }

        public void Awake()
        {
            if(GetComponent<VRCAvatarDescriptor>() != null)
            {
                EditorUtility.DisplayDialog("Error", "You are unable to add this script directly to an avatar. Please place this on a blank game object in the scene.", "Okay");
                GameObject.DestroyImmediate(this);
            }
        }
    }

    [CustomEditor(typeof(AvatarActions))]
    public class AvatarActionsEditor : Editor
    {
        AvatarActions script;
        VRCAvatarDescriptor avatarDescriptor; 

        public override void OnInspectorGUI()
        {
            script = target as AvatarActions;
            avatarDescriptor = script.avatar;

            EditorGUI.BeginChangeCheck();
            InspectorBody();
            if(EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(script);
            }
        }
        void InspectorBody()
        {
            //Target Avatar
            script.avatar = (VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar", script.avatar, typeof(VRCAvatarDescriptor), true);

            //Menu Actions
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                EditorGUILayout.BeginHorizontal();
                script.menuActions = (MenuActions)EditorGUILayout.ObjectField("Menu Actions", script.menuActions, typeof(MenuActions), false);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Non-Menu Actions
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            EditorGUILayout.LabelField("Other Actions");
            {
                if (GUILayout.Button("Add"))
                    script.otherActions.Add(null);
                for (int i = 0; i < script.otherActions.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        //Reference
                        script.otherActions[i] = (NonMenuActions)EditorGUILayout.ObjectField("Actions", script.otherActions[i], typeof(NonMenuActions), false);

                        //Delete
                        if (GUILayout.Button("X", GUILayout.Width(32)))
                        {
                            script.otherActions.RemoveAt(i);
                            i -= 1;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            EditorBase.Divider();

            //Build
            EditorGUI.BeginDisabledGroup(script.ReturnAnyScriptableObject() == null || avatarDescriptor == null);
            if (GUILayout.Button("Build Avatar Data", GUILayout.Height(32)))
            {
                BaseActions.BuildAvatarData(avatarDescriptor, script);
            }
            EditorGUI.EndDisabledGroup();

            //Build Options
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            {
                script.foldoutBuildOptions = EditorGUILayout.Foldout(script.foldoutBuildOptions, "Built Options");
                if (script.foldoutBuildOptions)
                {
                    //Ignore Lists
                    DrawStringList(ref script.foldoutIgnoreLayers, "Ignore Layers", script.ignoreLayers);
                    DrawStringList(ref script.foldoutIgnoreParameters, "Ignore Parameters", script.ignoreParameters);

                    void DrawStringList(ref bool foldout, string title, List<string> list)
                    {
                        EditorGUI.indentLevel += 1;
                        foldout = EditorGUILayout.Foldout(foldout, BaseActionsEditor.Title(title, list.Count > 0));
                        if (foldout)
                        {
                            //Add
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(EditorGUI.indentLevel * 10);
                            if (GUILayout.Button("Add"))
                            {
                                list.Add(null);
                            }
                            GUILayout.EndHorizontal();

                            //Layers
                            for (int i = 0; i < list.Count; i++)
                            {
                                EditorGUILayout.BeginHorizontal();
                                list[i] = EditorGUILayout.TextField(list[i]);
                                if (GUILayout.Button("X", GUILayout.Width(32)))
                                {
                                    list.RemoveAt(i);
                                    i--;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                        }
                        EditorGUI.indentLevel -= 1;
                    }
                }
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();

            //Build Data
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.indentLevel += 1;
            script.foldoutBuildData = EditorGUILayout.Foldout(script.foldoutBuildData, "Built Data");
            if (script.foldoutBuildData)
            {
                void AnimationController(VRCAvatarDescriptor.AnimLayerType animLayerType, string name)
                {
                    VRCAvatarDescriptor.CustomAnimLayer descLayer = new VRCAvatarDescriptor.CustomAnimLayer();
                    foreach (var layer in avatarDescriptor.baseAnimationLayers)
                    {
                        if (layer.type == animLayerType)
                        {
                            descLayer = layer;
                            break;
                        }
                    }

                    var controller = descLayer.animatorController as UnityEditor.Animations.AnimatorController;

                    EditorGUI.BeginChangeCheck();
                    controller = (UnityEditor.Animations.AnimatorController)EditorGUILayout.ObjectField(name, controller, typeof(UnityEditor.Animations.AnimatorController), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        descLayer.animatorController = controller;
                        descLayer.isDefault = false;
                    }
                }

                EditorGUILayout.HelpBox("Objects built and linked on the avatar descriptor. Anything referenced here will be modified and possibly destroyed by the compiling process.", MessageType.Info);

                AnimationController(VRCAvatarDescriptor.AnimLayerType.Action, "Action Controller");
                AnimationController(VRCAvatarDescriptor.AnimLayerType.FX, "FX Controller");
                avatarDescriptor.expressionsMenu = (ExpressionsMenu)EditorGUILayout.ObjectField("Expressions Menu", avatarDescriptor.expressionsMenu, typeof(ExpressionsMenu), false);
                avatarDescriptor.expressionParameters = (ExpressionParameters)EditorGUILayout.ObjectField("Expression Parameters", avatarDescriptor.expressionParameters, typeof(ExpressionParameters), false);
            }
            EditorGUI.indentLevel -= 1;
            EditorGUILayout.EndVertical();
        }
    }
}
#endif