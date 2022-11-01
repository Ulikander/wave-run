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

       
    }
}

