﻿using System.Collections;
using System.IO;
using NUnit.Framework;
using Unity.FilmInternalUtilities;
using Unity.FilmInternalUtilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.MeshSync.Editor.Tests {


internal class SceneCachePlayerTests  {

    [Test]
    public void CreateSceneCache() {
        CreateAndDeleteSceneCachePlayerPrefab(MeshSyncTestEditorConstants.CUBE_TEST_DATA_PATH);
        CreateAndDeleteSceneCachePlayerPrefab(MeshSyncTestEditorConstants.SPHERE_TEST_DATA_PATH);        
    }

//----------------------------------------------------------------------------------------------------------------------       
    [Test]
    public void ChangeSceneCacheOnGameObject() {
        
        //Initial setup            
        SceneCachePlayer player = new GameObject("SceneCache").AddComponent<SceneCachePlayer>();
        Assert.IsFalse(player.IsSceneCacheOpened());
        
        //Change
        SceneCachePlayerEditorUtility.ChangeSceneCacheFile(player, Path.GetFullPath(MeshSyncTestEditorConstants.CUBE_TEST_DATA_PATH));
        Assert.IsTrue(player.IsSceneCacheOpened());
        Camera cam0   = player.GetComponentInChildren<Camera>();        
        Light  light0 = player.GetComponentInChildren<Light>();        
        
        SceneCachePlayerEditorUtility.ChangeSceneCacheFile(player, Path.GetFullPath(MeshSyncTestEditorConstants.SPHERE_TEST_DATA_PATH));
        Assert.IsTrue(player.IsSceneCacheOpened());
        Camera cam1   = player.GetComponentInChildren<Camera>();
        Light  light1 = player.GetComponentInChildren<Light>();        
        
        Assert.IsNotNull(cam0);
        Assert.IsNotNull(light0);
        Assert.AreEqual(cam0, cam1);
        Assert.AreEqual(light0, light1);

        
        Object.DestroyImmediate(player.gameObject); //Cleanup        
    }

//----------------------------------------------------------------------------------------------------------------------       
    [Test]
    public void ChangeSceneCacheOnDirectPrefab() {
        
        //Initial setup
        const string DEST_PREFAB_PATH = "Assets/TestSceneCache.prefab";
        const string ASSETS_FOLDER    = "Assets/TestSceneCacheAssets";
            
        bool prefabCreated = SceneCachePlayerEditorUtility.CreateSceneCachePlayerAndPrefab(
            Path.GetFullPath(MeshSyncTestEditorConstants.CUBE_TEST_DATA_PATH), DEST_PREFAB_PATH,ASSETS_FOLDER, 
            out SceneCachePlayer player, out GameObject prefab
        );
        Assert.IsTrue(prefabCreated);

        //Check the prefab
        Assert.IsFalse(prefab.IsPrefabInstance());              
        Assert.IsTrue(prefab.IsPrefab());              
        SceneCachePlayer prefabPlayer = prefab.GetComponent<SceneCachePlayer>();
        Assert.IsNotNull(prefabPlayer);
        Camera cam0   = prefabPlayer.GetComponentInChildren<Camera>();        
        Light  light0 = prefabPlayer.GetComponentInChildren<Light>();        
        
        //Change
        TestDataComponents comps = ChangeSceneCacheFileAndVerify(prefabPlayer,
            Path.GetFullPath(MeshSyncTestEditorConstants.SPHERE_TEST_DATA_PATH)
        );
        Assert.AreEqual(cam0, comps.cam);
        Assert.AreEqual(light0, comps.light);

        //Cleanup
        Object.DestroyImmediate(player.gameObject);
        DeleteSceneCachePlayerPrefab(prefab);        
    }
    
//----------------------------------------------------------------------------------------------------------------------       
    [Test]
    public void ChangeSceneCacheOnInstancedPrefab() {
        
        //Initial setup
        const string DEST_PREFAB_PATH = "Assets/TestSceneCache.prefab";
        const string ASSETS_FOLDER    = "Assets/TestSceneCacheAssets";
            
        bool prefabCreated = SceneCachePlayerEditorUtility.CreateSceneCachePlayerAndPrefab(
            Path.GetFullPath(MeshSyncTestEditorConstants.CUBE_TEST_DATA_PATH), DEST_PREFAB_PATH,ASSETS_FOLDER, 
            out SceneCachePlayer player, out GameObject prefab
        );
        Assert.IsTrue(prefabCreated);

        //Check the instanced prefab
        Assert.IsNotNull(player);              
        Assert.IsTrue(player.gameObject.IsPrefabInstance());              
        Camera       cam0   = player.GetComponentInChildren<Camera>();        
        Light        light0 = player.GetComponentInChildren<Light>();
        MeshRenderer mr0    = player.GetComponentInChildren<MeshRenderer>();
        
        Assert.IsNotNull(cam0);
        Assert.IsNotNull(light0);
        Assert.IsNotNull(mr0);

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.SetParent(light0.transform);
        
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(mr0.transform);
                
        //Change
        TestDataComponents comps = ChangeSceneCacheFileAndVerify(player,Path.GetFullPath(MeshSyncTestEditorConstants.SPHERE_TEST_DATA_PATH));
        Assert.IsTrue(player.IsSceneCacheOpened());
        Assert.AreEqual(cam0, comps.cam);
        Assert.AreEqual(light0, comps.light);
        Assert.AreNotEqual(mr0, comps.meshRenderer);
        
        Assert.IsTrue(capsule !=null);
        Assert.IsFalse(capsule.IsPrefabInstance());
        Assert.IsTrue(cylinder == null); //should have been deleted when changing sc
        

        //Cleanup
        Object.DestroyImmediate(player.gameObject);
        DeleteSceneCachePlayerPrefab(prefab);        
    }

//----------------------------------------------------------------------------------------------------------------------       
    [UnityTest]
    public IEnumerator ReloadSceneCache() {
        
        SceneCachePlayer player = new GameObject("SceneCache").AddComponent<SceneCachePlayer>();
        
        //Set and reload
        SceneCachePlayerEditorUtility.ChangeSceneCacheFile(player, Path.GetFullPath(MeshSyncTestEditorConstants.CUBE_TEST_DATA_PATH));
        Assert.IsTrue(player.IsSceneCacheOpened());
        yield return null;
        
        SceneCachePlayerEditorUtility.ReloadSceneCacheFile(player);
        Assert.IsTrue(player.transform.childCount > 0);
        Assert.IsTrue(player.IsSceneCacheOpened());        

        yield return null;
        
        
        Object.DestroyImmediate(player.gameObject); //Cleanup        
    }
    
//----------------------------------------------------------------------------------------------------------------------       

    private void CreateAndDeleteSceneCachePlayerPrefab(string sceneCachePath) {
        Assert.IsTrue(File.Exists(sceneCachePath));

        const string DEST_PREFAB_PATH  = "Assets/TestSceneCache.prefab";
        const string ASSETS_FOLDER     = "Assets/TestSceneCacheAssets";
            
        bool  prefabCreated  = SceneCachePlayerEditorUtility.CreateSceneCachePlayerAndPrefab(
            Path.GetFullPath(sceneCachePath), DEST_PREFAB_PATH,ASSETS_FOLDER, 
            out SceneCachePlayer player, out GameObject prefab
        );
        Assert.IsTrue(prefabCreated);


        string savedScFilePath = player.GetSceneCacheFilePath();
        Assert.IsTrue(AssetEditorUtility.IsPathNormalized(savedScFilePath),$"{savedScFilePath} is not normalized");

        //Check player
        Assert.IsNotNull(player);
        Assert.IsTrue(player.IsSceneCacheOpened());       
        Assert.IsTrue(player.gameObject.IsPrefabInstance());
        
        //Check the prefab
        Assert.IsNotNull(prefab);
        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        Assert.IsFalse(string.IsNullOrEmpty(prefabPath));
        Assert.AreEqual(DEST_PREFAB_PATH, prefabPath);

        
        DeleteSceneCachePlayerPrefab(prefab);
    }
    
//----------------------------------------------------------------------------------------------------------------------

    void DeleteSceneCachePlayerPrefab(GameObject prefab) {
        Assert.IsTrue(prefab.IsPrefab());       
        
        SceneCachePlayer prefabPlayer = prefab.GetComponent<SceneCachePlayer>();
        Assert.IsNotNull(prefabPlayer);

        string assetsFolder = prefabPlayer.GetAssetsFolder();
        
        //Check assets folder
        Assert.IsTrue(Directory.Exists(assetsFolder));
        string[] prefabAssetGUIDs = AssetDatabase.FindAssets("", new[] {assetsFolder});
        Assert.Greater(prefabAssetGUIDs.Length, 0);
        
        
        //Cleanup
        foreach (string guid in prefabAssetGUIDs) {
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
        }
        FileUtil.DeleteFileOrDirectory(assetsFolder);

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        Assert.IsFalse(string.IsNullOrEmpty(prefabPath));
        AssetDatabase.DeleteAsset(prefabPath);

        AssetDatabase.Refresh();                
    }
    
//----------------------------------------------------------------------------------------------------------------------
    
    [Test]
    public void CopySceneCacheToStreamingAssets() {
        
        //Initial setup
        const string DEST_PREFAB_PATH    = "Assets/TestSceneCache.prefab";
        const string ASSETS_FOLDER       = "Assets/TestSceneCacheAssets";
        string       destFolder          = Path.Combine(Application.streamingAssetsPath,"TestRunner");
        string       streamingAssetsPath = Path.Combine(destFolder,"Copied.sc");
            
        bool prefabCreated = SceneCachePlayerEditorUtility.CreateSceneCachePlayerAndPrefab(
            Path.GetFullPath(MeshSyncTestEditorConstants.CUBE_TEST_DATA_PATH), DEST_PREFAB_PATH,ASSETS_FOLDER, 
            out SceneCachePlayer player, out GameObject prefab
        );
        Assert.IsTrue(prefabCreated);

        //Copy
        Directory.CreateDirectory(destFolder);
        File.Copy(player.GetSceneCacheFilePath(), streamingAssetsPath);
        Assert.IsTrue(File.Exists(streamingAssetsPath));
        AssetDatabase.Refresh();
        
        //Change
        ChangeSceneCacheFileAndVerify(player, streamingAssetsPath);        
        Assert.IsTrue(player.IsSceneCacheOpened());

        //Cleanup        
        Object.DestroyImmediate(player.gameObject);
        DeleteSceneCachePlayerPrefab(prefab);
        
        AssetDatabase.DeleteAsset(AssetEditorUtility.NormalizePath(streamingAssetsPath));
        AssetDatabase.DeleteAsset(AssetEditorUtility.NormalizePath(destFolder));
        AssetDatabase.Refresh();                
        
    }
    
//----------------------------------------------------------------------------------------------------------------------

    static TestDataComponents ChangeSceneCacheFileAndVerify(SceneCachePlayer player, string scPath) {
        SceneCachePlayerEditorUtility.ChangeSceneCacheFile(player, scPath);
        string savedScFilePath = player.GetSceneCacheFilePath();
        Assert.AreEqual(AssetEditorUtility.NormalizePath(scPath), savedScFilePath);                
        Assert.IsTrue(AssetEditorUtility.IsPathNormalized(savedScFilePath),$"{savedScFilePath} is not normalized");
        Assert.IsTrue(player.transform.childCount > 0);

        TestDataComponents ret = new TestDataComponents(
            player.GetComponentInChildren<Camera>(),
            player.GetComponentInChildren<Light>(),
            player.GetComponentInChildren<MeshRenderer>()
        );
        
        Assert.IsNotNull(ret.cam);
        Assert.IsNotNull(ret.light);
        Assert.IsNotNull(ret.meshRenderer);
        return ret;
    }

    class TestDataComponents {
        internal readonly Camera       cam;
        internal readonly Light        light;
        internal readonly MeshRenderer meshRenderer;

        internal TestDataComponents(Camera _cam, Light _light, MeshRenderer _mr) {
            cam          = _cam;
            light        = _light;
            meshRenderer = _mr;
        }
    }
    
    
    
    
}

}