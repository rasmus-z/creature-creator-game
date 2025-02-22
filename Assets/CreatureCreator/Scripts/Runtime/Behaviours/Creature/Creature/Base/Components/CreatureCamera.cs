using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    [RequireComponent(typeof(CreatureSpeedup))]
    public class CreatureCamera : MonoBehaviour, ISetupable
    {
        #region Fields
        [SerializeField] private GameObject cameraPrefab;
        #endregion

        #region Properties
        public Transform Root { get; private set; }
        public CameraOrbit CameraOrbit { get; private set; }
        public Follower Follower { get; private set; }
        public Camera MainCamera { get; private set; }
        public Camera ToolCamera { get; private set; }

        public bool IsSetup { get; set; }
        #endregion

        #region Methods
        public void Setup()
        {
            GameObject camera = Instantiate(cameraPrefab);

            Root = camera.transform;
            CameraOrbit = camera.GetComponent<CameraOrbit>();
            Follower = camera.GetComponent<Follower>();
            MainCamera = Root.GetChild(0).GetChild(0).GetComponent<Camera>();
            ToolCamera = Root.GetChild(0).GetChild(0).GetChild(0).GetComponent<Camera>();

            Follower.SetFollow(transform, true);

            IsSetup = true;
        }
        #endregion
    }
}