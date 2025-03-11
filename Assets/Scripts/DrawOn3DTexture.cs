using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.SampleGazeData
{
    [AddComponentMenu("Scripts/DrawOn3DTexture")]
    public class DrawOn3DTexture : MonoBehaviour
    {
        public Texture2D HeatmapLookUpTable;

        [SerializeField]
        private float drawBrushSize = 2000.0f; // aka spread

        [SerializeField]
        private float drawIntensity = 15.0f; // aka amplitude

        [SerializeField]
        private float minThreshDeltaHeatMap = 0.001f; // Mostly for performance to reduce spreading heatmap for small values.

        [SerializeField]
        private bool useRaycastForUV = true; // Use mesh raycast hit info for more accurate UV mapping

        public bool UseLiveInputStream = true;

        private Texture2D myDrawTex;
        private Renderer myRenderer;

        public Material HeatmapOverlayMaterialTemplate;

        private EyeTrackingTarget eyeTarget = null;

        private EyeTrackingTarget EyeTarget
        {
            get
            {
                if (eyeTarget == null)
                {
                    eyeTarget = this.GetComponent<EyeTrackingTarget>();
                }
                return eyeTarget;
            }
        }

        private void Start()
        {
            if (EyeTarget != null)
            {
                EyeTarget.WhileLookingAtTarget.AddListener(OnLookAt);
            }

            // Initialize the draw texture
            InitializeDrawTexture();
        }

        private void InitializeDrawTexture()
        {
            if (myDrawTex == null && HeatmapOverlayMaterialTemplate != null)
            {
                int textureSize = 1024; // Can be exposed as a parameter
                myDrawTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);

                Color clearColor = new Color(0, 0, 0, 0);
                for (int x = 0; x < myDrawTex.width; x++)
                {
                    for (int y = 0; y < myDrawTex.height; y++)
                    {
                        myDrawTex.SetPixel(x, y, clearColor);
                    }
                }
                myDrawTex.Apply();

                SetupOverlayMaterial();
            }
        }

        private void SetupOverlayMaterial()
        {
            if (MyRenderer == null || myDrawTex == null)
                return;

            Material overlayMaterial = Instantiate(HeatmapOverlayMaterialTemplate);
            overlayMaterial.mainTexture = myDrawTex;

            Material[] currentMats = MyRenderer.sharedMaterials;
            Material[] newMats = new Material[currentMats.Length + 1];

            for (int i = 0; i < currentMats.Length; i++)
            {
                newMats[i] = currentMats[i];
            }
            newMats[currentMats.Length] = overlayMaterial;

            MyRenderer.sharedMaterials = newMats;
        }

        public void OnLookAt()
        {
            if (UseLiveInputStream && (EyeTarget != null) && (EyeTarget.IsLookedAt))
            {
                DrawAtThisHitPos(EyeTrackingTarget.LookedAtPoint);
            }
        }

        public void DrawAtThisHitPos(Vector3 hitPosition)
        {
            if (useRaycastForUV)
            {
                Ray ray;
                var gazeProvider = CoreServices.InputSystem?.GazeProvider;
                if (gazeProvider != null)
                {
                    // Construct the ray using the gaze origin and direction.
                    ray = new Ray(gazeProvider.GazeOrigin, gazeProvider.GazeDirection);
                }
                else
                {
                    ray = new Ray(Camera.main.transform.position, hitPosition - Camera.main.transform.position);
                }

                RaycastHit hit;
                if (UnityEngine.Physics.Raycast(ray, out hit) && hit.collider.gameObject == gameObject)
                {
                    Debug.Log("Raycast hit at world position: " + hit.point);

                    MeshCollider meshCollider = hit.collider as MeshCollider;
                    if (meshCollider != null && meshCollider.sharedMesh != null)
                    {
                        Vector2[] meshUVs = meshCollider.sharedMesh.uv;
                        if (meshUVs != null && meshUVs.Length > 0)
                        {
                            Debug.Log("First UV coordinate in mesh: " + meshUVs[0]);
                        }

                        Vector2 hitUV = hit.textureCoord;
                        Debug.Log("Hit UV from MeshCollider: " + hitUV);
                        StartCoroutine(DrawAt(hitUV));
                    }
                    else
                    {
                        Debug.Log("MeshCollider not found or missing sharedMesh");
                        Vector2? hitPosUV = GetCursorPosInTexture(hitPosition);
                        if (hitPosUV != null)
                        {
                            Debug.Log("Fallback UV: " + hitPosUV);
                            StartCoroutine(DrawAt(hitPosUV.Value));
                        }
                        else
                        {
                            Debug.Log("Could not compute UV coordinates using fallback.");
                        }
                    }
                }
            }
            else
            {
                Vector2? hitPosUV = GetCursorPosInTexture(hitPosition);
                if (hitPosUV != null)
                {
                    Debug.Log("Hit UV: " + hitPosUV);
                    StartCoroutine(DrawAt(hitPosUV.Value));
                }
            }
        }

        public void ClearDrawing()
        {
            if (myDrawTex != null)
            {
                Color clearColor = new Color(0, 0, 0, 0);
                for (int x = 0; x < myDrawTex.width; x++)
                {
                    for (int y = 0; y < myDrawTex.height; y++)
                    {
                        myDrawTex.SetPixel(x, y, clearColor);
                    }
                }
                myDrawTex.Apply();
                neverDrawnOn = true;
            }
        }

        bool neverDrawnOn = true;
        Vector2 prevPos = new Vector2(-1, -1);
        float dynamicRadius = 0;

        private IEnumerator DrawAt(Vector2 posUV)
        {
            if (MyDrawTexture != null)
            {
                if (neverDrawnOn)
                {
                    for (int ix = 0; ix < MyDrawTexture.width; ix++)
                    {
                        for (int iy = 0; iy < MyDrawTexture.height; iy++)
                        {
                            MyDrawTexture.SetPixel(ix, iy, new Color(0, 0, 0, 0));
                        }
                    }
                    neverDrawnOn = false;
                }

                yield return null;

                StartCoroutine(ComputeHeatmapAt(posUV, true, true));
                yield return null;

                StartCoroutine(ComputeHeatmapAt(posUV, true, false));
                yield return null;

                StartCoroutine(ComputeHeatmapAt(posUV, false, true));
                yield return null;

                StartCoroutine(ComputeHeatmapAt(posUV, false, false));
                yield return null;

                MyDrawTexture.Apply();
            }
        }

        private IEnumerator ComputeHeatmapAt(Vector2 currPosUV, bool positiveX, bool positiveY)
        {
            yield return null;

            Vector2 center = new Vector2(currPosUV.x * MyDrawTexture.width, currPosUV.y * MyDrawTexture.height);
            int sign_x = (positiveX) ? 1 : -1;
            int sign_y = (positiveY) ? 1 : -1;
            int start_x = (positiveX) ? 0 : 1;
            int start_y = (positiveY) ? 0 : 1;

            for (int dx = start_x; dx < MyDrawTexture.width; dx++)
            {
                float tx = currPosUV.x * MyDrawTexture.width + dx * sign_x;
                if ((tx < 0) || (tx >= MyDrawTexture.width))
                    break;

                for (int dy = start_y; dy < MyDrawTexture.height; dy++)
                {
                    float ty = currPosUV.y * MyDrawTexture.height + dy * sign_y;
                    if ((ty < 0) || (ty >= MyDrawTexture.height))
                        break;

                    Color? newColor = null;
                    if (ComputeHeatmapColorAt(new Vector2(tx, ty), center, out newColor))
                    {
                        if (newColor.HasValue)
                        {
                            MyDrawTexture.SetPixel((int)tx, (int)ty, newColor.Value);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private bool ComputeHeatmapColorAt(Vector2 currPnt, Vector2 origPivot, out Color? col)
        {
            col = null;

            float spread = drawBrushSize;
            float amplitude = drawIntensity;
            float distCenterToCurrPnt = Vector2.Distance(origPivot, currPnt) / spread;

            float B = 2f;
            float scaledInterest = 1 / (1 + Mathf.Pow(Mathf.Epsilon, -(B * distCenterToCurrPnt)));
            float delta = scaledInterest / amplitude;
            if (delta < minThreshDeltaHeatMap)
                return false;

            Color baseColor = MyDrawTexture.GetPixel((int)currPnt.x, (int)currPnt.y);
            float normalizedInterest = Mathf.Clamp(baseColor.a + delta, 0, 1);

            if (HeatmapLookUpTable != null)
            {
                col = HeatmapLookUpTable.GetPixel((int)(normalizedInterest * (HeatmapLookUpTable.width - 1)), 0);
                col = new Color(col.Value.r, col.Value.g, col.Value.b, normalizedInterest);
            }
            else
            {
                col = Color.blue;
                col = new Color(col.Value.r, col.Value.g, col.Value.b, normalizedInterest);
            }

            return true;
        }

        private Renderer MyRenderer
        {
            get
            {
                if (myRenderer == null)
                {
                    myRenderer = GetComponent<Renderer>();
                }
                return myRenderer;
            }
        }

        private Texture2D MyDrawTexture
        {
            get
            {
                if (myDrawTex == null)
                {
                    InitializeDrawTexture();
                }
                return myDrawTex;
            }
        }

        private Vector2? GetCursorPosInTexture(Vector3 hitPosition)
        {
            Vector2? hitPointUV = null;

            try
            {
                Vector3 center = gameObject.transform.position;
                Vector3 halfsize = gameObject.transform.localScale / 2;

                Vector3 transfHitPnt = hitPosition - center;
                transfHitPnt = Quaternion.AngleAxis(-(this.gameObject.transform.rotation.eulerAngles.y - 180), Vector3.up) * transfHitPnt;
                transfHitPnt = Quaternion.AngleAxis(this.gameObject.transform.rotation.eulerAngles.x, Vector3.right) * transfHitPnt;

                float uvx = (Mathf.Clamp(transfHitPnt.x, -halfsize.x, halfsize.x) + halfsize.x) / (2 * halfsize.x);
                float uvy = (Mathf.Clamp(transfHitPnt.y, -halfsize.y, halfsize.y) + halfsize.y) / (2 * halfsize.y);
                hitPointUV = new Vector2(uvx, uvy);
            }
            catch (UnityEngine.Assertions.AssertionException)
            {
                Debug.LogError(">> AssertionException");
            }

            return hitPointUV;
        }
    }
}
