using UnityEngine;

namespace Palimpsest
{
    /// <summary>
    /// A fixed, gently tracking isometric-ish camera. It leans a fraction of the
    /// way toward the scribe so the arena never feels static, and it owns the
    /// screen shake that every impact in the game borrows.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera View { get; private set; }

        Transform _cameraTransform;
        Transform _focus;
        Vector3 _pivot;
        Vector3 _offset = new Vector3(0f, 30f, -21f);
        float _shake;

        public static CameraRig Build(Transform parent)
        {
            GameObject rigObject = new GameObject("Camera Rig");
            rigObject.transform.SetParent(parent, false);

            // Reuse the scene's main camera when one exists (the default Unity scene
            // always has one); otherwise make our own. Either way there is exactly one
            // rendering camera and one AudioListener.
            Camera camera = Camera.main;
            GameObject cameraObject;

            if (camera != null)
            {
                cameraObject = camera.gameObject;
                cameraObject.name = "Palimpsest Camera";
            }
            else
            {
                cameraObject = new GameObject("Palimpsest Camera");
                camera = cameraObject.AddComponent<Camera>();

                try { cameraObject.tag = "MainCamera"; }
                catch (UnityException) { /* Tag database is project-owned; ignore. */ }
            }

            cameraObject.transform.SetParent(rigObject.transform, false);
            cameraObject.transform.position = new Vector3(0f, 30f, -21f);
            cameraObject.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 4f, 0f) - cameraObject.transform.position, Vector3.up);

            // URP cameras want a UniversalAdditionalCameraData component. Adding it by
            // reflection keeps this file compiling in a Built-in Render Pipeline project.
            EnsureScriptablePipelineCameraData(cameraObject);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Night;
            camera.fieldOfView = 46f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 200f;
            camera.allowHDR = true;

            if (Object.FindObjectOfType<AudioListener>() == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }

            CameraRig rig = rigObject.AddComponent<CameraRig>();
            rig.View = camera;
            rig._cameraTransform = cameraObject.transform;
            rig._pivot = Vector3.zero;
            return rig;
        }

        static void EnsureScriptablePipelineCameraData(GameObject cameraObject)
        {
            if (cameraObject == null) return;
            if (!MaterialFactory.UsingScriptablePipeline) return;

            try
            {
                System.Type dataType = System.Type.GetType(
                    "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");

                if (dataType != null && cameraObject.GetComponent(dataType) == null)
                {
                    cameraObject.AddComponent(dataType);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[Palimpsest] Could not add URP camera data: " + exception.Message);
            }
        }

        /// <summary>Sets the transform the camera should lazily lean toward.</summary>
        public void Follow(Transform focus)
        {
            _focus = focus;
        }

        /// <summary>Adds a decaying shake impulse (0.05 subtle, 0.3 dramatic).</summary>
        public void Shake(float amount)
        {
            _shake = Mathf.Min(0.55f, _shake + Mathf.Max(0f, amount));
        }

        public void Tick(float deltaTime)
        {
            if (_cameraTransform == null) return;

            Vector3 targetPivot = Vector3.zero;
            if (_focus != null)
            {
                targetPivot = new Vector3(_focus.position.x * 0.22f, 0f, _focus.position.z * 0.22f);
            }

            _pivot = Vector3.Lerp(_pivot, targetPivot, 1f - Mathf.Exp(-4.5f * deltaTime));

            Vector3 shake = Vector3.zero;
            if (_shake > 0.0005f)
            {
                shake = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f) * 0.4f,
                    Random.Range(-1f, 1f)) * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, deltaTime * 1.6f);
            }

            _cameraTransform.position = _pivot + _offset + shake;

            Vector3 lookAt = _pivot + new Vector3(0f, 1.2f, 0f);
            Vector3 forward = lookAt - _cameraTransform.position;
            if (forward.sqrMagnitude > 0.001f)
            {
                _cameraTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
        }
    }
}
