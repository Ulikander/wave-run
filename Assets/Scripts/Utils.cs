using System.Threading;
using UnityEngine;

namespace WASD.Runtime
{
    public class Utils
    {
        public static Vector3 ScreenToWorld(Camera camera, Vector3 position)
        {
            //position.z = camera.nearClipPlane;
            return camera.ScreenToWorldPoint(position: position);
        }

        public static Ray ScreenToRay(Camera camera, Vector3 position)
        {
            //position.z = camera.nearClipPlane;
            return camera.ScreenPointToRay(pos: position);
        }

        public static bool IsTouchPositionHittingCollider(Camera camera, Vector3 position, Collider collider)
        {
            Ray ray = camera.ScreenPointToRay(pos: position);
            if(Physics.Raycast(ray: ray, out RaycastHit hit))
            {
                return hit.collider.Equals(other: collider);
            }
            return false;
        }

        public static void ChangeAllMeshRenderersMaterial(Renderer[] renderers, int[] indexes, Material material)
        {
            if (
               material == null ||
               renderers.Length == 0 ||
               indexes.Length == 0 ||
               renderers.Length != indexes.Length)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] newMaterials = renderers[i].sharedMaterials;
                newMaterials[indexes[i]] = material;
                renderers[i].sharedMaterials = newMaterials;
            }
        }

        public static bool IsUnityTaskRunning(ref UnityTask task)
        {
            return task != null && task.Running;
        }

        public static void StopUnityTask(ref UnityTask task)
        {
            if (IsUnityTaskRunning(task: ref task))
            {
                task.Stop();
            }
        }

        public static T GetGlobalInstance<T>() where T : MonoBehaviour
        {
            T instance = Object.FindObjectOfType(type: typeof(T)) as T;
            if (instance == null)
            {
                instance = new GameObject { name = typeof(T).Name }.AddComponent(componentType: typeof(T)) as T;
            }
            return instance;
        }


        #region UniTask & Threading

        public static bool IsCancelTokenSourceActive(ref CancellationTokenSource tokenSource)
        {
            return tokenSource is { IsCancellationRequested: false };
        }

        public static void CancelTokenSourceRequestCancelAndDispose(ref CancellationTokenSource tokenSource)
        {
            if (tokenSource == null) return;
            tokenSource.Cancel();
            tokenSource.Dispose();
            tokenSource = null;
        }

        #endregion
    }
}

