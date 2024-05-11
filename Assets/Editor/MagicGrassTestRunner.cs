using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;

using MagicGrass;
public class MagicGrassTestRunner
{
    //Test cell bounds of grass renderer
    [Test]
    public void MagicGrassRendererCellBoundsPasses()
    {
        var cellSize = (int)MagicGrassGrassMapSize.Mid / 4;
        var renderer = new IndirectDrawController.MassGrassRenderer();
        renderer.SetCellInfo(cellSize, 0,0,MagicGrassGrassMapSize.Mid, Vector3.zero, 512);
        var bounds = renderer.GetCellCenter();
        Assert.AreEqual(new Vector3(64,0,64), bounds.center);
        Assert.AreEqual(512 / 4, bounds.size.x);
        
        renderer.SetCellInfo(cellSize, 0,2,MagicGrassGrassMapSize.Mid, Vector3.zero, 512);
        bounds = renderer.GetCellCenter();
        Assert.AreEqual(new Vector3(64,0,320), bounds.center);
        Assert.AreEqual(512 / 4, bounds.size.x);

        var originOffset = new Vector3(50, 0, 50);
        renderer.SetCellInfo(cellSize, 2,0,MagicGrassGrassMapSize.Mid,originOffset, 512);
        bounds = renderer.GetCellCenter();
        Assert.AreEqual(new Vector3(320,0,64) + originOffset, bounds.center);
        Assert.AreEqual(512 / 4, bounds.size.x);
    }
    
    [UnityTest]
    public IEnumerator MagicGrassTestRuntimeRT()
    {
        var indirectDrawController = GameObject.FindObjectOfType<IndirectDrawController>();
        indirectDrawController.ForceRebuildRenderers();
        yield return null;
        Assert.NotNull(indirectDrawController.SurfaceTexture);
        Assert.NotNull(indirectDrawController.NormalTexture);
        Assert.NotNull(indirectDrawController.HeightTexture);
        Assert.NotNull(indirectDrawController.HizTexture);
    }
    
    [UnityTest]
    public IEnumerator MagicGrassTestComputeShaderLoaded()
    {
        var indirectDrawController = GameObject.FindObjectOfType<IndirectDrawController>();
        indirectDrawController.ForceRebuildRenderers();
        yield return null;
        Assert.NotNull(indirectDrawController.DrawGrassCS);
        Assert.NotNull(indirectDrawController.FrustumCullingCS);
    }
    
    [UnityTest]
    public IEnumerator MagicGrassTestValidateModels()
    {
        var indirectDrawController = GameObject.FindObjectOfType<IndirectDrawController>();
        indirectDrawController.ForceRebuildRenderers();
        yield return null;
        foreach (var model in indirectDrawController.Models)
        {
            Assert.AreEqual(true, model.IsValidModel);
        }
    }
    
    
}
