using NUnit.Framework;
using NoRainbowAfter.Interaction;
using UnityEngine;

public class InteractionTests
{
    [Test] public void ObjectiveFlagsAreUnique()
    {
        var flags = new SessionObjectiveFlags();
        Assert.IsTrue(flags.Set("objective.book")); Assert.IsFalse(flags.Set("objective.book")); Assert.IsTrue(flags.Contains("objective.book"));
    }
    [Test] public void TriggersDoNotBlockButIgnoreRaycastLayerSolidsDo()
    {
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            target.transform.position = new Vector3(0, 0, 2); target.AddComponent<SceneInteractable>();
            obstruction.transform.position = new Vector3(0, 0, 1); obstruction.layer = 2;
            obstruction.GetComponent<Collider>().isTrigger = true; Physics.SyncTransforms();
            Assert.IsNotNull(PlayerInteractor.FindTarget(new Ray(Vector3.zero, Vector3.forward)));
            obstruction.GetComponent<Collider>().isTrigger = false; Physics.SyncTransforms();
            Assert.IsNull(PlayerInteractor.FindTarget(new Ray(Vector3.zero, Vector3.forward)));
        }
        finally { Object.DestroyImmediate(target); Object.DestroyImmediate(obstruction); }
    }
    [Test] public void OnlyOwnHierarchyIsIgnored()
    {
        var owner = new GameObject("owner");
        var ownCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var foreign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            ownCollider.transform.SetParent(owner.transform);
            ownCollider.transform.position = new Vector3(0, 0, .5f);
            ownCollider.transform.localScale = Vector3.one * .2f;
            foreign.transform.position = new Vector3(0, 0, 1);
            foreign.transform.localScale = Vector3.one * .2f;
            target.transform.position = new Vector3(0, 0, 2);
            target.AddComponent<SceneInteractable>();
            Physics.SyncTransforms();
            Assert.IsNull(PlayerInteractor.FindTarget(new Ray(Vector3.zero, Vector3.forward), owner.transform));
            foreign.SetActive(false); Physics.SyncTransforms();
            Assert.IsNotNull(PlayerInteractor.FindTarget(new Ray(Vector3.zero, Vector3.forward), owner.transform));
            Assert.IsNull(PlayerInteractor.FindTarget(new Ray(Vector3.zero, Vector3.forward)));
        }
        finally { Object.DestroyImmediate(owner); Object.DestroyImmediate(foreign); Object.DestroyImmediate(target); }
    }
    [Test] public void ClosestSolidBlocksTargetAndRangeIsLimited()
    {
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            target.transform.position = new Vector3(0, 0, 2.5f); target.AddComponent<SceneInteractable>();
            wall.transform.position = new Vector3(0, 0, 1); Physics.SyncTransforms();
            Assert.IsNull(PlayerInteractor.FindTarget(new Ray(Vector3.zero, Vector3.forward)));
            wall.SetActive(false); Physics.SyncTransforms();
            Assert.IsNotNull(PlayerInteractor.FindTarget(new Ray(Vector3.zero, Vector3.forward)));
            target.transform.position = new Vector3(0, 0, 2.81f); Physics.SyncTransforms();
            Assert.IsNull(PlayerInteractor.FindTarget(new Ray(Vector3.zero, Vector3.forward)));
        }
        finally { Object.DestroyImmediate(target); Object.DestroyImmediate(wall); }
    }
}
