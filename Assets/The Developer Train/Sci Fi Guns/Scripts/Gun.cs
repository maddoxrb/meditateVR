using System;
using System.Collections;
using System.Reflection;
using Oculus.Interaction;
using UnityEngine;

namespace TheDeveloperTrain.SciFiGuns
{

    public class Gun : MonoBehaviour
    {
        /// <summary>
        /// The particle systems for the gun, if any
        /// </summary>
        [Tooltip("The particle systems for the gun, if any")]
        public ParticleSystem[] gunParticleSystems;

        [SerializeField] private GameObject bulletPrefab;

        /// <summary>
        /// The Transform point of the muzzle, AKA where the bullet prefab is spawned. the bullet inherits its rotation as well
        /// </summary>
        [Tooltip("The Transform point of the muzzle, AKA where the bullet prefab is spawned. the bullet inherits its rotation as well")]
        [SerializeField] private Transform muzzleTransform;

        public GunStats stats;

        /// <summary>
        /// The number of bullets currently left, before the gun has to reload
        /// </summary>
        [HideInInspector] public int currentBulletCount;
        private int currentMagLeft;

        [HideInInspector] public bool isReloading = false;
        public bool IsInShotCooldown { get; private set; } = false;

        [Header("VR Trigger Input")]
        [SerializeField] private OVRInput.Button shootButton = OVRInput.Button.PrimaryIndexTrigger;
        [Tooltip("Optional anchors used to fall back to distance-based controller detection")]
        [SerializeField] private Transform leftHandAnchor;
        [SerializeField] private Transform rightHandAnchor;

        private Grabbable grabbable;
        private bool isHeld;
        private OVRInput.Controller holdingController = OVRInput.Controller.None;
        private string holdingInteractorName = "";

        /// <summary>
        /// Called when the bullet is actually created, AKA after the shoot delay.
        /// </summary>
        public Action onBulletShot;

        public Action onLastBulletShotInBurst;

        public Action onGunReloadStart;

        /// <summary>
        /// Called as soon as the gun starts it's shooting procedure, if the gun is ready to be fired
        /// </summary>
        public Action onGunShootingStart;

        void Awake()
        {
            grabbable = GetComponent<Grabbable>();
        }

        void OnEnable()
        {
            if (grabbable != null)
            {
                grabbable.WhenPointerEventRaised += HandlePointerEvent;
            }
        }

        void OnDisable()
        {
            if (grabbable != null)
            {
                grabbable.WhenPointerEventRaised -= HandlePointerEvent;
            }
        }

        void Start()
        {
            currentBulletCount = stats.magazineSize;
            currentMagLeft = stats.totalAmmo;
        }

        void Update()
        {
            if (!isHeld)
            {
                return;
            }

            bool pressed;

            if (holdingController != OVRInput.Controller.None)
            {
                pressed = OVRInput.GetDown(shootButton, holdingController);
            }
            else
            {
                pressed = OVRInput.GetDown(shootButton, OVRInput.Controller.LTouch) ||
                          OVRInput.GetDown(shootButton, OVRInput.Controller.RTouch);
            }

            if (holdingController == OVRInput.Controller.LTouch &&
                OVRInput.GetDown(shootButton, OVRInput.Controller.RTouch))
            {
                LogHold("Update.IgnoredOther", OVRInput.Controller.RTouch, "other hand pressed but not holding");
            }
            else if (holdingController == OVRInput.Controller.RTouch &&
                     OVRInput.GetDown(shootButton, OVRInput.Controller.LTouch))
            {
                LogHold("Update.IgnoredOther", OVRInput.Controller.LTouch, "other hand pressed but not holding");
            }

            if (pressed)
            {
                LogHold("Update.Fire", holdingController, $"button={shootButton}, interactorName={holdingInteractorName}");
                Shoot();
            }
        }

        public void Shoot()
        {

            if (currentBulletCount > 0 && !isReloading && !IsInShotCooldown)
            {
                IsInShotCooldown = true;
                onGunShootingStart?.Invoke();
                foreach (var particleSystem in gunParticleSystems)
                {
                    particleSystem.Play();
                }
                if (stats.fireMode == FireMode.Single)
                {
                    currentBulletCount--;

                    Invoke(nameof(SpawnBullet), stats.shootDelay);
                    StartCoroutine(nameof(ResetGunShotCooldown));


                    if (currentBulletCount == 0)
                    {
                        Reload();
                    }
                }
                else if (stats.fireMode == FireMode.Burst)
                {
                    StartCoroutine(nameof(FireBulletsInBurst));
                }
            }

        }

        private void SpawnBullet()
        {
            GameObject bullet = GameObject.Instantiate(bulletPrefab);
            bullet.transform.SetPositionAndRotation(muzzleTransform.position, muzzleTransform.rotation);
            bullet.GetComponent<Bullet>().speed = stats.bulletSpeed;

            onBulletShot?.Invoke();

        }

        public void Reload()
        {
            StartCoroutine(nameof(ReloadGun));
        }

        private IEnumerator ReloadGun()
        {
            if (!isReloading)
            {
                onGunReloadStart?.Invoke();
                isReloading = true;
                yield return new WaitForSeconds(stats.reloadDuration + (1 / stats.fireRate));
                if (currentMagLeft != 0)
                {
                    if (currentMagLeft - (stats.magazineSize - currentBulletCount) >= 0)
                    {
                        currentMagLeft -= (stats.magazineSize - currentBulletCount);
                        currentBulletCount = stats.magazineSize;
                    }
                    else
                    {
                        currentBulletCount += currentMagLeft;
                        currentMagLeft = 0;
                    }
                }
                isReloading = false;
            }
        }
        private IEnumerator ResetGunShotCooldown()
        {
            yield return new WaitForSeconds(1 / stats.fireRate - stats.shootDelay);
            IsInShotCooldown = false;
        }
        private IEnumerator FireBulletsInBurst()
        {
            yield return new WaitForSeconds(stats.shootDelay);

            for (int i = 0; i < stats.burstCount; i++)
            {
                SpawnBullet();
                currentBulletCount--;
                if (currentBulletCount == 0)
                {
                    Reload();
                    break;
                }
                onBulletShot?.Invoke();
                yield return new WaitForSeconds(stats.burstInterval);

            }
            onLastBulletShotInBurst?.Invoke();
            yield return new WaitForSeconds(1 / stats.fireRate - (stats.shootDelay + (stats.burstCount * stats.burstInterval)));
            IsInShotCooldown = false;
        }

        private void HandlePointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    isHeld = true;

                    var interactorController = OVRInput.Controller.None;
                    string source = "unknown";
                    string interactorName = "";

                    var evtType = evt.GetType();
                    var selectingProp = evtType.GetProperty("SelectingInteractor");
                    var interactorProp = evtType.GetProperty("Interactor");

                    object selectingObj = null;
                    if (selectingProp != null)
                    {
                        selectingObj = selectingProp.GetValue(evt, null);
                    }
                    else if (interactorProp != null)
                    {
                        selectingObj = interactorProp.GetValue(evt, null);
                    }

                    if (selectingObj != null && interactorController == OVRInput.Controller.None)
                    {
                        interactorController = TryFromHandedness(selectingObj, out source);
                    }

                    GameObject interactorGO = ResolveGameObject(selectingObj);
                    if (interactorGO != null)
                    {
                        interactorName = interactorGO.name;
                    }

                    if (interactorGO != null && interactorController == OVRInput.Controller.None)
                    {
                        interactorController = TryFromOVRGrabber(interactorGO, out source);
                    }

                    if (interactorGO != null && interactorController == OVRInput.Controller.None)
                    {
                        interactorController = TryFromName(interactorGO.name, out source);
                    }

                    if (interactorController == OVRInput.Controller.None)
                    {
                        interactorController = MapInteractorToController(evt.Identifier);
                        if (interactorController != OVRInput.Controller.None)
                        {
                            source = $"identifier:{evt.Identifier}";
                        }
                    }

                    if (interactorController == OVRInput.Controller.None)
                    {
                        var byButtons = DetectPressingController();
                        if (byButtons != OVRInput.Controller.None)
                        {
                            interactorController = byButtons;
                            source = "buttons";
                        }
                    }

                    if (interactorController == OVRInput.Controller.None)
                    {
                        var byDistance = DetectNearestAnchorTo(transform.position);
                        if (byDistance != OVRInput.Controller.None)
                        {
                            interactorController = byDistance;
                            source = "distance";
                        }
                    }

                    holdingController = interactorController;
                    holdingInteractorName = interactorName;
                    LogHold("HandlePointerEvent.Select", holdingController, $"source={source}, interactorName={interactorName}");

                    break;

                case PointerEventType.Unselect:
                    LogHold("HandlePointerEvent.Unselect", holdingController, "released");
                    isHeld = false;
                    holdingController = OVRInput.Controller.None;
                    holdingInteractorName = "";
                    break;
            }
        }

        private OVRInput.Controller MapInteractorToController(int identifier)
        {
            if (identifier == 0)
            {
                return OVRInput.Controller.LTouch;
            }
            if (identifier == 1)
            {
                return OVRInput.Controller.RTouch;
            }
            return OVRInput.Controller.None;
        }

        private static GameObject ResolveGameObject(object selectingObj)
        {
            if (selectingObj == null)
            {
                return null;
            }

            var goProp = selectingObj.GetType().GetProperty("gameObject");
            if (goProp != null)
            {
                var go = goProp.GetValue(selectingObj, null) as GameObject;
                if (go != null)
                {
                    return go;
                }
            }

            var transformProp = selectingObj.GetType().GetProperty("transform");
            if (transformProp != null)
            {
                var t = transformProp.GetValue(selectingObj, null) as Transform;
                if (t != null)
                {
                    return t.gameObject;
                }
            }

            if (selectingObj is Component component)
            {
                return component.gameObject;
            }

            return null;
        }

        private static OVRInput.Controller TryFromName(string name, out string source)
        {
            source = "name";
            if (string.IsNullOrEmpty(name))
            {
                return OVRInput.Controller.None;
            }

            var lower = name.ToLower();
            if (lower.Contains("left") || lower.Contains("l_touch") || lower.Contains("ltouch") || lower.Contains("lcontroller"))
            {
                return OVRInput.Controller.LTouch;
            }
            if (lower.Contains("right") || lower.Contains("r_touch") || lower.Contains("rtouch") || lower.Contains("rcontroller"))
            {
                return OVRInput.Controller.RTouch;
            }

            source = "name-unmatched";
            return OVRInput.Controller.None;
        }

        private static OVRInput.Controller TryFromHandedness(object selectingObj, out string source)
        {
            source = "handedness";
            if (selectingObj == null)
            {
                return OVRInput.Controller.None;
            }

            var prop = selectingObj.GetType().GetProperty("Handedness", BindingFlags.Public | BindingFlags.Instance);
            if (prop != null)
            {
                var val = prop.GetValue(selectingObj, null)?.ToString();
                if (!string.IsNullOrEmpty(val))
                {
                    if (val.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return OVRInput.Controller.LTouch;
                    }
                    if (val.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return OVRInput.Controller.RTouch;
                    }
                }
            }

            if (selectingObj is Component comp)
            {
                foreach (var mb in comp.GetComponents<MonoBehaviour>())
                {
                    var t = mb.GetType().GetProperty("Handedness", BindingFlags.Public | BindingFlags.Instance);
                    if (t != null)
                    {
                        var v = t.GetValue(mb, null)?.ToString();
                        if (!string.IsNullOrEmpty(v))
                        {
                            if (v.IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return OVRInput.Controller.LTouch;
                            }
                            if (v.IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return OVRInput.Controller.RTouch;
                            }
                        }
                    }
                }
            }

            source = "handedness-unavailable";
            return OVRInput.Controller.None;
        }

        private static OVRInput.Controller TryFromOVRGrabber(GameObject go, out string source)
        {
            source = "ovrgrabber";
            if (go == null)
            {
                return OVRInput.Controller.None;
            }

            var components = go.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c == null)
                {
                    continue;
                }
                var typeName = c.GetType().Name;
                if (typeName == "OVRGrabber")
                {
                    var byName = TryFromName(go.name, out _);
                    if (byName != OVRInput.Controller.None)
                    {
                        return byName;
                    }
                }
            }

            source = "ovrgrabber-notfound";
            return OVRInput.Controller.None;
        }

        private static string HandString(OVRInput.Controller c)
        {
            switch (c)
            {
                case OVRInput.Controller.LTouch:
                    return "Left";
                case OVRInput.Controller.RTouch:
                    return "Right";
                default:
                    return "None";
            }
        }

        private void LogHold(string where, OVRInput.Controller controller, string note)
        {
            Debug.Log($"[Gun] {where}: holding={HandString(controller)} controller={controller} note={note}");
        }

        private OVRInput.Controller DetectPressingController()
        {
            bool l =
                OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch) ||
                OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch) ||
                OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch) ||
                OVRInput.Get(OVRInput.Button.Three, OVRInput.Controller.LTouch);

            bool r =
                OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch) ||
                OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) ||
                OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch) ||
                OVRInput.Get(OVRInput.Button.Four, OVRInput.Controller.RTouch);

            if (l && !r)
            {
                return OVRInput.Controller.LTouch;
            }
            if (r && !l)
            {
                return OVRInput.Controller.RTouch;
            }
            if (l && r)
            {
                bool lIdx = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
                bool rIdx = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
                if (lIdx && !rIdx)
                {
                    return OVRInput.Controller.LTouch;
                }
                if (rIdx && !lIdx)
                {
                    return OVRInput.Controller.RTouch;
                }
            }
            return OVRInput.Controller.None;
        }

        private OVRInput.Controller DetectNearestAnchorTo(Vector3 worldPos)
        {
            if (leftHandAnchor == null && rightHandAnchor == null)
            {
                return OVRInput.Controller.None;
            }

            float dL = float.PositiveInfinity;
            float dR = float.PositiveInfinity;
            if (leftHandAnchor != null)
            {
                dL = Vector3.SqrMagnitude(worldPos - leftHandAnchor.position);
            }
            if (rightHandAnchor != null)
            {
                dR = Vector3.SqrMagnitude(worldPos - rightHandAnchor.position);
            }

            if (dL < dR)
            {
                return OVRInput.Controller.LTouch;
            }
            if (dR < dL)
            {
                return OVRInput.Controller.RTouch;
            }
            return OVRInput.Controller.None;
        }

    }

}
