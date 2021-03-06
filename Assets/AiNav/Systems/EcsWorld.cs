﻿using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AiNav
{
    public class EditorModeSystemConfig
    {
        public Type Type;
        public bool Update;
    }

    public class EcsWorld : MonoBehaviour
    {
        public static bool EditorModeEnabled { get; private set; }
        private static List<EditorModeSystemConfig> SystemConfigs = new List<EditorModeSystemConfig>()
        {
            new EditorModeSystemConfig {Type = typeof(AiNavSystem), Update = true}
        };

        private static bool IsPlaying;

        public static World Active
        {
            get
            {
#if UNITY_EDITOR
                if (!IsPlaying)
                {
                    return GetEditorModeWorld();
                }
#endif
                return World.Active;
            }
        }

#if UNITY_EDITOR
        public static void StopEditorSystems()
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            if (World.Active == null) return;
            if (!EditorModeEnabled) return;

            foreach (var config in SystemConfigs)
            {
                var system = World.Active.GetExistingSystem(config.Type);
                if (config.Update)
                {
                    UnityEditor.EditorApplication.update -= system.Update;
                }
                World.Active.DestroySystem(system);
            }

            var entities = World.Active.EntityManager.GetAllEntities(Allocator.Temp);
            World.Active.EntityManager.DestroyEntity(entities);
            entities.Dispose();

            EditorModeEnabled = false;
            Debug.Log("Editor systems stopped");
        }

        private static World GetEditorModeWorld()
        {
            if (World.Active == null)
            {
                DefaultWorldInitialization.DefaultLazyEditModeInitialize();
            }

            if (!EditorModeEnabled)
            {
                EditorModeEnabled = true;

                foreach (var config in SystemConfigs)
                {
                    var system = World.Active.GetOrCreateSystem(config.Type);
                    
                    if (config.Update)
                    {
                        system.Enabled = true;
                        UnityEditor.EditorApplication.update +=  system.Update;
                    }
                }
                
                Debug.Log("Editor systems started");
            }
            
            return World.Active;
        }

        private static void OnPlaymodeChanged(UnityEditor.PlayModeStateChange change)
        {
            switch (change)
            {
                case UnityEditor.PlayModeStateChange.EnteredEditMode:
                    IsPlaying = false;
                    break;
            }
        }

#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            if (EditorModeEnabled)
            {
                StopEditorSystems();
            }
            EditorModeEnabled = false;
            IsPlaying = true;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= OnPlaymodeChanged;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlaymodeChanged;
#endif

        }

    }
}
